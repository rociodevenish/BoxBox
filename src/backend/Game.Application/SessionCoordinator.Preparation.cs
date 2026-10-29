using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;

public sealed partial class SessionCoordinator
{
    private EffectiveWeekend ApplyWeekendConditions(Lobby lobby,EffectiveWeekend effective)
    {
        if(lobby.CampaignConditions is {} frozen)return effective with{Configuration=effective.Configuration with{Weather=frozen.Weather,WetTyres=frozen.WetTyres,WeatherReactions=frozen.WeatherReactions}};
        var configuration=presets.Get(lobby.Input.Preset!,lobby.Input.Laps!.Value).Configuration;
        if(configuration.Weather is null&&configuration.WetTyres is null)return effective;
        return effective with{Configuration=effective.Configuration with{
            Weather=configuration.Weather is {} weather ? WeatherWeekendProfiles.GetProfile(weather) is {} profile
                ? WeatherWeekendProfiles.Apply(effective,profile) with{TimeOriginSeconds=weather.TimeOriginSeconds,InformationQuality=weather.InformationQuality}
                : WeekendPresetCatalog.WeatherInputs(effective,weather) : null,
            WetTyres=configuration.WetTyres,WeatherReactions=configuration.WeatherReactions}};
    }
    private const string PreparationModule = "weekend-preparation-v1";
    private sealed record AcceptedSetup(string Fingerprint, SetupAck Ack);
    private sealed record PreparedTeam(WeekendPreset Preset, WeekendSetup Setup, EffectiveWeekend Effective,
        long Revision, string Lifecycle, Dictionary<string, AcceptedSetup> Commands, RecordedPractice? Practice = null,
        string Source = "human",IReadOnlyList<RecordedPreparationChoice>? AutomaticChoices=null,
        CurrentWeather? Observed=null,TeamWeatherForecast? Forecast=null);
    private sealed record RecordedPractice(WeekendPreset Preset, int Laps, WeekendSetup Setup, int Seed,
        string ModelVersion, PracticeAssessment Result);
    private sealed record AcceptedFreeze(string Fingerprint, FreezePreparationAck Ack);
    private sealed record PreparedLobby(string Version, Dictionary<string, PreparedTeam> Teams,
        FrozenPreparationContext? Frozen = null, Dictionary<string, AcceptedFreeze>? FreezeCommands = null);

    public PreparationPublicSnapshot BeginPreparation(string id, AuthIdentity identity, BeginPreparationInput input)
        =>BeginPreparationCore(id,identity,input,null);

    private PreparationPublicSnapshot BeginPreparationCore(string id, AuthIdentity identity, BeginPreparationInput input,IReadOnlyList<WeekendPreset>? frozen)
    {
        mutations.Wait();
        try { lock (gate)
        {
            var lobby = RequireLobby(id); RequireHost(lobby, identity);
            if(lobby.ModuleState.ContainsKey("campaign-development-pending-v1"))throw new SessionFault(SessionFailure.Conflict,"Close campaign development before preparation and practice.");
            if(lobby.ModuleState.ContainsKey("campaign-construction-pending-v1"))throw new SessionFault(SessionFailure.Conflict,"Complete Base Car construction before preparation.");
            if(lobby.ModuleState.ContainsKey("campaign-personnel-pending-v1"))throw new SessionFault(SessionFailure.Conflict,"Complete campaign personnel acquisition before preparation.");
            if(lobby.CampaignPresets is {} campaignPresets)
            {
                if(input.CircuitId!=campaignPresets[0].Circuit.Id)throw new SessionFault(SessionFailure.Conflict,"Campaign event circuit is frozen.");
                frozen=campaignPresets;
            }
            if (lobby.ModuleState.ContainsKey(PreparationModule))
            {
                var existing = ReadPreparation(lobby);
                if (existing.Teams.First().Value.Preset.Circuit.Id != input.CircuitId)
                    throw new SessionFault(SessionFailure.Conflict,"Preparation already preserves a different circuit.");
                return ProjectPreparation(lobby, existing);
            }
            RequireLobbyOpen(lobby);
            if (lobby.Assignments.Count == 0 || lobby.Assignments.Values.Any(a => !a.Approved))
                throw new SessionFault(SessionFailure.Conflict,"Claim at least one team and approve all assignments first.");
            var teams = new Dictionary<string, PreparedTeam>();
            try
            {
                foreach (var (teamId, index) in ConstructorCatalogue.Select((t,i)=>(t.TeamId,i)))
                {
                    var pair = PreparationDrivers[index];
                    var opened = frozen is null?WeekendPresetCatalog.Open(input.CircuitId, pair.First, pair.Second):frozen[index];
                    var preset = RacePackages.Migrate(opened) with { Cars = opened.Cars.Select((car,i) => car with
                        { TeamId=teamId, EntryId=$"{teamId}-car-{i+1}" }).ToImmutableArray() };
                    var setup = new WeekendSetup();
                    var effective=ApplyWeekendConditions(lobby,WeekendPreparation.Prepare(preset,lobby.Input.Laps!.Value,setup));
                    var tuning=effective.Configuration.Weather;
                    var observed=tuning is null?null:CircuitWeather.Observe(tuning,lobby.Input.Seed!.Value,tuning.TimeOriginSeconds);
                    teams.Add(teamId,new(preset,setup,effective,1,"provisional",[],Observed:observed,
                        Forecast:observed is null?null:CircuitWeather.Forecast(observed,teamId,tuning!.InformationQuality,tuning.ForecastRefreshSeconds)));
                }
            }
            catch (ArgumentException error) { throw new SessionFault(SessionFailure.InvalidInput,error.Message); }
            lobby.RequiredTeams=lobby.Assignments.Keys.Order(StringComparer.Ordinal).ToArray();
            lobby.ScriptedTeams=teams.Keys.Except(lobby.RequiredTeams).Order(StringComparer.Ordinal).ToArray();
            foreach(var teamId in lobby.ScriptedTeams)
                teams[teamId]=PrepareAutomaticTeam(lobby,teams[teamId]);
            var state=new PreparedLobby(PreparationModule,teams);
            lobby.Lifecycle="preparing"; lobby.Revision++; WritePreparation(lobby,state); Persist(id);
            return ProjectPreparation(lobby,state);
        }} finally { mutations.Release(); }
    }

    public PreparationPublicSnapshot PreparationPublic(string id)
    { lock(gate) { var lobby=RequireLobby(id); return ProjectPreparation(lobby,ReadPreparation(lobby)); } }

    public PreparationTeamSnapshot HouseholdPreparation(string id,AuthIdentity identity,string deviceId,string teamId)
    { lock(gate) { var access=AuthorisePreparationController(id,identity,deviceId,teamId);
        return ProjectPreparationTeam(access.Lobby,ReadPreparation(access.Lobby).Teams[teamId],teamId,access.Assignment); } }

    public PreparationTeamSnapshot OpenPreparation(string id,string teamId)
    { lock(gate) { var lobby=RequireLobby(id); var assignment=lobby.Assignments.GetValueOrDefault(teamId);
        if(lobby.Input.Mode=="Competitive" || assignment is null || !assignment.Approved || assignment.Control!="Open")
            throw new SessionFault(SessionFailure.Forbidden,"This team's preparation is private.");
        return ProjectPreparationTeam(lobby,ReadPreparation(lobby).Teams[teamId],teamId,assignment); } }

    public SetupAck SaveSetup(string id,AuthIdentity identity,string deviceId,string teamId,SetupCommand command)
    {
        mutations.Wait();
        try { lock(gate)
        {
            var access=AuthorisePreparationController(id,identity,deviceId,teamId);
            var lobby=access.Lobby; var state=ReadPreparation(lobby); var team=state.Teams[teamId];
            if(command is null || command.Setup is null)
                throw new SessionFault(SessionFailure.InvalidInput,"Supply command identity, current revisions and all three setup choices.");
            var key=ValidatePreparationCommand(command.CommandId,command.Revision,command.AssignmentRevision,access.Assignment.Revision);
            try { command.Setup.Validate(); }
            catch(ArgumentException error) { throw new SessionFault(SessionFailure.InvalidInput,error.Message); }
            var fingerprint=JsonSerializer.Serialize(new {Operation="setup",Session=id,Team=teamId,command.Revision,
                command.AssignmentRevision,command.Setup,Version=PreparationModule});
            if(PreparationReceipt(team,key,fingerprint) is {} accepted)return accepted;
            if(lobby.Lifecycle!="preparing" || team.Lifecycle is not ("provisional" or "assessed"))
                throw new SessionFault(SessionFailure.Conflict,"Setup is not editable in this lifecycle.");
            if(command.Revision!=team.Revision)throw new SessionFault(SessionFailure.Conflict,"Setup changed. Recover the saved revision before editing.");
            var effective=ApplyWeekendConditions(lobby,WeekendPreparation.Prepare(team.Preset,lobby.Input.Laps!.Value,command.Setup));
            var next=team with {Setup=command.Setup,Effective=effective,Revision=team.Revision+1};
            var ack=new SetupAck(key,id,teamId,next.Revision,next.Lifecycle);
            next.Commands[key]=new(fingerprint,ack); state.Teams[teamId]=next;
            lobby.Revision++; WritePreparation(lobby,state); Persist(id); return ack;
        }} finally { mutations.Release(); }
    }

    public SetupAck ConfirmPractice(string id,AuthIdentity identity,string deviceId,string teamId,PracticeCommand command)
    {
        mutations.Wait();
        try { lock(gate)
        {
            var access=AuthorisePreparationController(id,identity,deviceId,teamId);
            var lobby=access.Lobby;var state=ReadPreparation(lobby);var team=state.Teams[teamId];
            if(command is null)
                throw new SessionFault(SessionFailure.InvalidInput,"Supply command identity and current revisions.");
            var key=ValidatePreparationCommand(command.CommandId,command.Revision,command.AssignmentRevision,access.Assignment.Revision);
            var fingerprint=JsonSerializer.Serialize(new {Operation="practice",Session=id,Team=teamId,command.Revision,
                command.AssignmentRevision,Version=PreparationModule});
            if(PreparationReceipt(team,key,fingerprint) is {} accepted)return accepted;
            if(lobby.Lifecycle!="preparing" || team.Lifecycle!="provisional" || team.Practice is not null)
                throw new SessionFault(SessionFailure.Conflict,"This team already assessed practice or cannot assess in this lifecycle.");
            if(command.Revision!=team.Revision)
                throw new SessionFault(SessionFailure.Conflict,"Setup changed. Recover the saved revision before confirming practice.");
            var seed=lobby.Input.Seed!.Value;
            var result=WeekendPreparation.Assess(team.Preset,lobby.Input.Laps!.Value,team.Setup,seed);
            var recorded=new RecordedPractice(team.Preset,lobby.Input.Laps.Value,team.Setup,seed,
                WeekendPreparation.PracticeModelVersion,result);
            var next=team with {Practice=recorded,Revision=team.Revision+1,Lifecycle="assessed"};
            var ack=new SetupAck(key,id,teamId,next.Revision,next.Lifecycle);
            next.Commands[key]=new(fingerprint,ack);state.Teams[teamId]=next;
            lobby.Revision++;WritePreparation(lobby,state);Persist(id);return ack;
        }} finally { mutations.Release(); }
    }

    public SetupAck ConfirmPreparation(string id, AuthIdentity identity, string deviceId, string teamId, PracticeCommand command)=>
        ChangePreparationReadiness(id,identity,deviceId,teamId,command,false);

    public SetupAck ReopenPreparation(string id, AuthIdentity identity, string deviceId, string teamId, PracticeCommand command)=>
        ChangePreparationReadiness(id,identity,deviceId,teamId,command,true);

    private SetupAck ChangePreparationReadiness(string id, AuthIdentity identity, string deviceId, string teamId,
        PracticeCommand command, bool reopen)
    {
        mutations.Wait();
        try { lock(gate)
        {
            var access=AuthorisePreparationController(id,identity,deviceId,teamId);
            var lobby=access.Lobby;var state=ReadPreparation(lobby);var team=state.Teams[teamId];
            if(command is null)
                throw new SessionFault(SessionFailure.InvalidInput,"Supply command identity and current revisions.");
            var key=ValidatePreparationCommand(command.CommandId,command.Revision,command.AssignmentRevision,access.Assignment.Revision);
            var fingerprint=JsonSerializer.Serialize(new {Operation=reopen?"reopen":"confirm",Session=id,Team=teamId,
                command.Revision,command.AssignmentRevision,Version=PreparationModule});
            if(PreparationReceipt(team,key,fingerprint) is {} accepted)return accepted;
            if(lobby.Lifecycle!="preparing" || team.Lifecycle!=(reopen?"final-ready":"assessed") || team.Practice is null)
                throw new SessionFault(SessionFailure.Conflict,"Preparation cannot change readiness in this lifecycle.");
            if(command.Revision!=team.Revision)
                throw new SessionFault(SessionFailure.Conflict,"Setup changed. Recover the saved revision before confirmation.");
            team.Setup.Validate();
            var next=team with {Revision=team.Revision+1,Lifecycle=reopen?"assessed":"final-ready"};
            var ack=new SetupAck(key,id,teamId,next.Revision,next.Lifecycle);
            next.Commands[key]=new(fingerprint,ack);state.Teams[teamId]=next;
            lobby.Revision++;WritePreparation(lobby,state);Persist(id);return ack;
        }} finally { mutations.Release(); }
    }

    // S08 invokes this operation when qualifying begins. No race/session is created here.
    public FreezePreparationAck FreezePreparationForQualifying(string id, AuthIdentity identity, FreezePreparationCommand command)
    {
        mutations.Wait();
        try { lock(gate)
        {
            var lobby=RequireLobby(id);RequireHost(lobby,identity);var state=ReadPreparation(lobby);
            if(command is null || !Guid.TryParse(command.CommandId,out var commandId) || commandId==Guid.Empty || command.Revision is null)
                throw new SessionFault(SessionFailure.InvalidInput,"Supply command identity and current lobby revision.");
            var key=commandId.ToString("D");
            var fingerprint=JsonSerializer.Serialize(new{Operation="freeze",Session=id,command.Revision,Version=PreparationModule});
            if(state.FreezeCommands?.TryGetValue(key,out var accepted)==true)
            {
                if(accepted.Fingerprint!=fingerprint)throw new SessionFault(SessionFailure.Conflict,"Command identity was already used with different inputs.");
                return accepted.Ack;
            }
            if(state.Teams.Values.Any(t=>t.Commands.ContainsKey(key)))
                throw new SessionFault(SessionFailure.Conflict,"Command identity was already used for a team operation.");
            if(lobby.Lifecycle!="preparing" || state.Frozen is not null || command.Revision!=lobby.Revision)
                throw new SessionFault(SessionFailure.Conflict,"Preparation changed or is already frozen.");
            if(!state.Teams.Keys.Order(StringComparer.Ordinal).SequenceEqual(ConstructorCatalogue.Select(t=>t.TeamId).Order(StringComparer.Ordinal))
                || state.Teams.Values.Any(t=>t.Lifecycle!="final-ready" || t.Practice is null))
                throw new SessionFault(SessionFailure.Conflict,"Every team must be final-ready before qualifying can freeze preparation.");
            foreach(var (teamId,team) in state.Teams)
            {
                try
                {
                    team.Setup.Validate();
                    _=WeekendPreparation.Prepare(team.Preset,lobby.Input.Laps!.Value,team.Setup);
                    if(team.Effective.ModelVersion!=WeekendPreparation.ModelVersion || team.Effective.Setup!=team.Setup
                        || team.Preset.Cars.Any(c=>c.TeamId!=teamId) || string.IsNullOrWhiteSpace(team.Source))
                        throw new ArgumentException("Saved effective context does not match the prepared team.");
                }
                catch(ArgumentException error){throw new SessionFault(SessionFailure.Conflict,error.Message);}
            }
            var context=new FrozenPreparationContext(Guid.NewGuid().ToString("D"),PreparationModule,
                state.Teams.OrderBy(t=>t.Key,StringComparer.Ordinal).Select(t=>new FrozenPreparedTeam(t.Key,t.Value.Revision,
                    t.Value.Source,t.Value.Preset,t.Value.Setup,t.Value.Effective,t.Value.AutomaticChoices)).ToImmutableArray());
            var ack=new FreezePreparationAck(key,id,lobby.Revision+1,context.ContextId);
            var receipts=state.FreezeCommands??[];receipts[key]=new(fingerprint,ack);
            foreach(var (teamId,team) in state.Teams.ToArray())
                state.Teams[teamId]=team with{Lifecycle="locked",Revision=team.Revision+1};
            state=state with{Frozen=context,FreezeCommands=receipts};
            lobby.Lifecycle="preparation-locked";lobby.Revision++;WritePreparation(lobby,state);Persist(id);return ack;
        }} finally { mutations.Release(); }
    }

    public FrozenPreparationContext FrozenPreparation(string id,AuthIdentity identity)
    { lock(gate){var lobby=RequireLobby(id);RequireHost(lobby,identity);
        return ReadPreparation(lobby).Frozen??throw new SessionFault(SessionFailure.Conflict,"Preparation has not been frozen.");} }

    public IReadOnlyList<PreparationTeamSnapshot> PreparationControllerUpdates(string id,AuthIdentity identity)
    { lock(gate){var lobby=RequireLobby(id);RequireHost(lobby,identity);var state=ReadPreparation(lobby);
        return state.Teams.Where(t=>lobby.Assignments.ContainsKey(t.Key)).Select(t=>
            ProjectPreparationTeam(lobby,t.Value,t.Key,lobby.Assignments[t.Key])).ToArray();} }

    // Call after current controller authorisation. Payload fingerprints and lifecycle checks remain operation-specific.
    private static string ValidatePreparationCommand(string commandId,long? revision,long? assignmentRevision,long currentAssignmentRevision)
    {
        if(!Guid.TryParse(commandId,out var identity) || identity==Guid.Empty || revision is null || assignmentRevision is null)
            throw new SessionFault(SessionFailure.InvalidInput,"Supply command identity and current revisions.");
        if(assignmentRevision!=currentAssignmentRevision)
            throw new SessionFault(SessionFailure.Conflict,"Controller assignment changed. Refresh current authority.");
        return identity.ToString("D");
    }

    private static SetupAck? PreparationReceipt(PreparedTeam team,string key,string fingerprint)
    {
        if(!team.Commands.TryGetValue(key,out var accepted))return null;
        if(accepted.Fingerprint!=fingerprint)
            throw new SessionFault(SessionFailure.Conflict,"Command identity was already used with different inputs.");
        return accepted.Ack;
    }

    private static PreparedLobby ReadPreparation(Lobby lobby)
    {
        if(!lobby.ModuleState.TryGetValue(PreparationModule,out var json))
            throw new SessionFault(SessionFailure.Conflict,"Begin weekend preparation first.");
        var state=JsonSerializer.Deserialize<PreparedLobby>(json,BrowserReplay.JsonOptions);
        if(state is null || state.Version!=PreparationModule || state.Teams.Count!=11
            || (state.Frozen is not null && (state.Frozen.ModelVersion!=PreparationModule || state.Frozen.Teams.Count!=11
                || state.Frozen.Teams.Any(t=>t.Effective.ModelVersion!=WeekendPreparation.ModelVersion || t.Effective.SetupModelVersion!=WeekendSetup.Version)))
            || state.Teams.Values.Any(t=>t.Effective.SetupModelVersion!=WeekendSetup.Version
                || (t.Practice is not null && (t.Practice.ModelVersion!=WeekendPreparation.PracticeModelVersion
                    || t.Practice.Result.Feedback.ModelVersion!=WeekendPreparation.PracticeModelVersion))))
            throw new SessionFault(SessionFailure.Conflict,"Saved preparation model is incompatible; preserved inputs require the compatible deployment.");
        return state;
    }
    private static void WritePreparation(Lobby lobby,PreparedLobby state)=>
        lobby.ModuleState[PreparationModule]=JsonSerializer.Serialize(state,BrowserReplay.JsonOptions);
    private static PreparationPublicSnapshot ProjectPreparation(Lobby lobby,PreparedLobby state)=>
        new(lobby.Id,lobby.Revision,lobby.Lifecycle,state.Teams.First().Value.Preset.Circuit,state.Teams.Keys.ToArray(),
            state.Teams.Select(t=>new PreparationReadiness(t.Key,t.Value.Lifecycle is "final-ready" or "locked",t.Value.Lifecycle=="locked",t.Value.Source)).ToArray(),state.Teams.First().Value.Observed);
    private static PreparationPolicyObservation ObservePreparation(PreparedTeam team)=>
        new(PreparationDecisionPolicy.ObservationVersion,string.Join("|",team.Preset.Version,team.Preset.Circuit.Version,
            team.Preset.Cars[0].Car.Version,team.Preset.Cars[0].Driver.Version,team.Preset.Cars[1].Driver.Version,team.Preset.Engineer.Version),
            team.Preset.Circuit.Name,team.Preset.Circuit.Demands,
            team.Preset.Cars[0].Car.Ratings,team.Preset.Cars.Select(c=>new PreparationDriverObservation(c.Driver.Id,
                c.Driver.Name,c.Driver.Preferences,c.Driver.Ratings["adaptability"])).ToArray(),
            team.Practice is null?null:new(team.Practice.Result.Feedback.ModelVersion,team.Practice.Setup,
                team.Practice.Result.Feedback.CircuitFeedback,team.Practice.Result.Feedback.Drivers));

    private PreparedTeam PrepareAutomaticTeam(Lobby lobby,PreparedTeam team)
    {
        var provisionalObservation=ObservePreparation(team);
        var provisional=PreparationDecisionPolicy.Evaluate(provisionalObservation);
        var result=WeekendPreparation.Assess(team.Preset,lobby.Input.Laps!.Value,provisional.Setup,lobby.Input.Seed!.Value);
        var assessed=team with{Setup=provisional.Setup,Practice=new(team.Preset,lobby.Input.Laps.Value,provisional.Setup,
            lobby.Input.Seed.Value,WeekendPreparation.PracticeModelVersion,result)};
        var finalObservation=ObservePreparation(assessed);var final=PreparationDecisionPolicy.Evaluate(finalObservation);
        return assessed with{Setup=final.Setup,Effective=ApplyWeekendConditions(lobby,WeekendPreparation.Prepare(team.Preset,lobby.Input.Laps.Value,final.Setup)),
            Lifecycle="final-ready",Revision=team.Revision+3,Source="unclaimed-ai",
            AutomaticChoices=[new("provisional",provisionalObservation,provisional),new("final",finalObservation,final)]};
    }

    public IReadOnlyList<PreparationAutomaticRecord> PreparationAutomaticRecords(string id)
    { lock(gate){return ReadPreparation(RequireLobby(id)).Teams.Where(t=>t.Value.AutomaticChoices is not null)
        .Select(t=>new PreparationAutomaticRecord(t.Key,t.Value.Source,t.Value.AutomaticChoices!)).ToArray();} }

    private static PreparationGuidance ProjectPreparationGuidance(Lobby lobby,PreparedTeam team,Assignment assignment)
    {
        var settings=AssistanceSettings(lobby,assignment.PlayerId);
        return new(settings.HelpLevel,settings.AutoAcceptRecommendation,settings.HelpLevel=="Standard"?null:
            PreparationDecisionPolicy.Evaluate(ObservePreparation(team)));
    }

    private static PreparationTeamSnapshot ProjectPreparationTeam(Lobby lobby,PreparedTeam team,string teamId,Assignment assignment)=>
        new(lobby.Id,teamId,team.Revision,assignment.Revision,team.Lifecycle,team.Setup,RacePackages.Migrate(team.Preset),
            team.Effective.Summary,team.Effective.SetupModelVersion,
            team.Practice is null ? null : new PracticeSnapshot(team.Practice.Result.Feedback.ModelVersion,
                team.Practice.Result.Feedback.Setup,team.Practice.Result.Feedback.CircuitFeedback,
                team.Practice.Result.Feedback.Compromises,team.Practice.Result.Feedback.Drivers,
                RacePackages.Migrate(team.Practice.Preset).SelectedPackage,
                RacePackages.Selected(RacePackages.Migrate(team.Practice.Preset)).Name),ProjectPreparationGuidance(lobby,team,assignment),team.Observed,team.Forecast);

    // Explicit preset identity; constructor metadata and illustrative card stars never provide coefficients.
    private static readonly (string First,string Second)[] PreparationDrivers = [
        ("driver-lando-norris","driver-oscar-piastri"), ("driver-charles-leclerc","driver-lewis-hamilton"),
        ("driver-max-verstappen","driver-isack-hadjar"), ("driver-george-russell","driver-andrea-kimi-antonelli"),
        ("driver-fernando-alonso","driver-lance-stroll"), ("driver-pierre-gasly","driver-franco-colapinto"),
        ("driver-esteban-ocon","driver-oliver-bearman"), ("driver-liam-lawson","driver-arvid-lindblad"),
        ("driver-alexander-albon","driver-carlos-sainz"), ("driver-nico-hulkenberg","driver-gabriel-bortoleto"),
        ("driver-sergio-perez","driver-valtteri-bottas")];
}
