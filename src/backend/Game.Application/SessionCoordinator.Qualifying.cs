using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;

public sealed partial class SessionCoordinator
{
    private const string QualifyingModule="qualifying-session-v1";
    private sealed record AcceptedQualifyingStart(string Fingerprint,StartQualifyingAck Ack);
    private sealed record AcceptedQualifying(string Fingerprint,QualifyingAck Ack);
    private sealed record QualifyingLock(string Source,string PolicyVersion,IReadOnlyList<QualifyingChoice> Choices);
    private sealed record QualifyingState(string Version,string Session,long WindowRevision,long Revision,
        FrozenPreparationContext Preparation,QualifyingAttemptInput Input,Dictionary<string,QualifyingLock> Locks,Dictionary<string,QualifyingLock> TimeoutLocks,
        Dictionary<string,AcceptedQualifying> Commands,Dictionary<string,AcceptedQualifyingStart> Starts,
        DecisionTimingPolicy TimingPolicy,DateTimeOffset? Deadline,ScheduledWakeup? Wakeup,
        string Lifecycle="awaiting-decisions",bool Paused=false,double? Remaining=null,QualifyingResult? Result=null,QualifyingAttemptInput[]? Attempts=null,QualifyingResult[]? Results=null,QualifyingRank[]? Grid=null,Dictionary<string,DueTransitionInput>? ContinueCommands=null,Dictionary<string,RecordedQualifyingAdvice>? Assistance=null);

    public StartQualifyingAck StartQualifying(string id,AuthIdentity identity,StartQualifyingCommand command)
    {
        mutations.Wait();try{lock(gate)
        {
            var lobby=RequireLobby(id);RequireHost(lobby,identity);
            if(command is null||!Guid.TryParse(command.CommandId,out var guid)||guid==Guid.Empty||command.Revision is null)
                throw new SessionFault(SessionFailure.InvalidInput,"Supply command identity and current preparation revision.");
            var key=guid.ToString("D");var fingerprint=JsonSerializer.Serialize(new{Operation="start-qualifying",id,command.Revision,Version=QualifyingModule});
            if(lobby.ModuleState.ContainsKey(QualifyingModule))
            {
                var existing=ReadQualifying(lobby);
                if(existing.Starts.TryGetValue(key,out var accepted)&&accepted.Fingerprint==fingerprint)return accepted.Ack;
                throw new SessionFault(SessionFailure.Conflict,"Qualifying has already begun or command identity changed.");
            }
            var preparation=ReadPreparation(lobby);
            if(preparation.Teams.Values.Any(t=>t.Commands.ContainsKey(key))||preparation.FreezeCommands?.ContainsKey(key)==true)
                throw new SessionFault(SessionFailure.Conflict,"Command identity already belongs to preparation.");
            if(lobby.Lifecycle!="preparing"||command.Revision!=lobby.Revision||preparation.Frozen is not null
                ||preparation.Teams.Count!=ConstructorCatalogue.Length||preparation.Teams.Values.Any(t=>t.Lifecycle!="final-ready"||t.Practice is null))
                throw new SessionFault(SessionFailure.Conflict,"Every team must be final-ready. Accepted human preparation is never replaced.");
            foreach(var team in preparation.Teams.Values)
            {
                team.Setup.Validate();
                if(team.Effective.ModelVersion!=WeekendPreparation.ModelVersion||team.Effective.Setup!=team.Setup)
                    throw new SessionFault(SessionFailure.Conflict,"Saved effective preparation is incompatible.");
            }
            var frozen=new FrozenPreparationContext(Guid.NewGuid().ToString("D"),PreparationModule,
                preparation.Teams.OrderBy(t=>t.Key,StringComparer.Ordinal).Select(t=>new FrozenPreparedTeam(t.Key,t.Value.Revision,
                    t.Value.Source,t.Value.Preset,t.Value.Setup,t.Value.Effective,t.Value.AutomaticChoices)).ToImmutableArray());
            var order=frozen.Teams.SelectMany(t=>t.Effective.Cars.Select(c=>c.Source.EntryId)).ToArray();
            var input=new QualifyingAttemptInput("Q1",1,lobby.Input.Seed!.Value,frozen.Teams.Select(t=>t.Effective).ToArray(),[],order,new());
            var timing=BuildTimingPolicy(lobby,lobby.RequiredTeams) with{TimeoutPolicy=QualifyingTimeoutDisclosure};
            var deadline=clock.GetUtcNow().AddSeconds(timing.EffectiveSeconds);var revision=lobby.Revision+1;
            var locks=new Dictionary<string,QualifyingLock>();
            var ack=new StartQualifyingAck(key,id,revision,"Q1",frozen.ContextId);
            var state=new QualifyingState(QualifyingModule,"Q1",revision,revision,frozen,input,locks,[],[],new(){{key,new(fingerprint,ack)}},timing,deadline,
                new(id,Guid.NewGuid().ToString("N"),"qualifying-decision",1,revision,deadline));
            state=FreezeQualifyingPolicies(lobby,state);
            // Validate the frozen complete policy choices before any mutation.
            _=Qualifying.Resolve(input with{Choices=state.Locks.Values.Concat(state.TimeoutLocks.Values).SelectMany(plan=>plan.Choices).ToArray()});
            foreach(var (teamId,team) in preparation.Teams.ToArray())preparation.Teams[teamId]=team with{Lifecycle="locked",Revision=team.Revision+1};
            WritePreparation(lobby,preparation with{Frozen=frozen});
            lobby.Lifecycle="qualifying";lobby.Revision=revision;WriteQualifying(lobby,state);Persist(id);return ack;
        }}finally{mutations.Release();}
    }
    public QualifyingPublicSnapshot QualifyingPublic(string id)
    {lock(gate){var lobby=RequireLobby(id);return ProjectQualifying(lobby,ReadQualifying(lobby));}}
    public QualifyingTeamSnapshot HouseholdQualifying(string id,AuthIdentity identity,string deviceId,string teamId)
    {lock(gate){var access=AuthorisePreparationController(id,identity,deviceId,teamId);return ProjectQualifyingTeam(access.Lobby,ReadQualifying(access.Lobby),teamId,access.Assignment);}}
    public QualifyingTeamSnapshot OpenQualifying(string id,string teamId)
    {lock(gate){var lobby=RequireLobby(id);var assignment=lobby.Assignments.GetValueOrDefault(teamId);
        if(lobby.Input.Mode=="Competitive"||assignment is null||!assignment.Approved||assignment.Control!="Open")throw new SessionFault(SessionFailure.Forbidden,"This team's qualifying choices are private.");
        return ProjectQualifyingTeam(lobby,ReadQualifying(lobby),teamId,assignment);}}
    public QualifyingAck SubmitQualifying(string id,AuthIdentity identity,string deviceId,string teamId,QualifyingCommand command)
    {
        mutations.Wait();try{lock(gate)
        {
            var access=AuthorisePreparationController(id,identity,deviceId,teamId);var lobby=access.Lobby;var state=ReadQualifying(lobby);
            if(command is null||command.Choices is null||command.Choices.Any(c=>c is null||string.IsNullOrWhiteSpace(c.EntryId)))throw new SessionFault(SessionFailure.InvalidInput,"Supply complete eligible-driver choices.");
            var key=ValidatePreparationCommand(command.CommandId,command.Revision,command.AssignmentRevision,access.Assignment.Revision);
            var choices=command.Choices.OrderBy(c=>c.EntryId,StringComparer.Ordinal).ToImmutableArray();
            var fingerprint=JsonSerializer.Serialize(new{Operation="qualifying-lock",id,teamId,command.Session,command.Revision,command.AssignmentRevision,Choices=choices,Version=QualifyingModule});
            if(state.Commands.TryGetValue(key,out var accepted))
            {
                if(accepted.Fingerprint!=fingerprint)throw new SessionFault(SessionFailure.Conflict,"Command identity was already used differently.");
                if(lobby.Race is {} committed)committed.StorageError=false;
                return accepted.Ack;
            }
            if(state.Starts.ContainsKey(key)||state.ContinueCommands?.ContainsKey(key)==true||ReadPreparation(lobby).Teams.Values.Any(t=>t.Commands.ContainsKey(key)))throw new SessionFault(SessionFailure.Conflict,"Command identity already belongs to another operation.");
            var eligible=QualifyingDrivers(state).Where(c=>c.TeamId==teamId).Select(c=>c.EntryId).Order(StringComparer.Ordinal).ToArray();
            if(eligible.Length==0||!choices.Select(c=>c.EntryId).SequenceEqual(eligible)||choices.Any(c=>c.Approach is not ("Safe" or "Push" or "All-Out")))
                throw new SessionFault(SessionFailure.InvalidInput,"Lock exactly one Safe, Push or All-Out choice for each eligible driver.");
            if(command.Session!=state.Session||command.Revision!=state.WindowRevision||state.Paused||state.Lifecycle!="awaiting-decisions"||state.Locks.ContainsKey(teamId))
                throw new SessionFault(SessionFailure.Conflict,"This qualifying window changed or your team is already locked.");
            if(state.Deadline is {} deadline&&clock.GetUtcNow()>=deadline)
                throw new SessionFault(SessionFailure.Conflict,"The qualifying deadline has passed. Refresh the recorded result.");
            state.Locks[teamId]=new("human","human-v1",choices);state=state with{Revision=state.Revision+1};
            if(RequiredQualifyingTeams(lobby,state).All(state.Locks.ContainsKey))state=ResolveQualifying(lobby,state);
            var ack=new QualifyingAck(key,id,teamId,state.Session,state.Revision,state.Lifecycle);state.Commands[key]=new(fingerprint,ack);
            lobby.Revision++;WriteQualifying(lobby,state);Persist(id);return ack;
        }}finally{mutations.Release();}
    }
    private QualifyingState ResolveQualifying(Lobby lobby,QualifyingState state)
    {
        var input=state.Input with{Choices=state.Locks.OrderBy(p=>p.Key,StringComparer.Ordinal).SelectMany(p=>p.Value.Choices).ToArray()};
        var result=Qualifying.Resolve(input);var results=(state.Results??[]).Append(result).ToArray();
        var due=clock.GetUtcNow().AddSeconds(PresentationSeconds);
        var grid=state.Session=="Q3"?Qualifying.Grid(results):null;
        if(grid is not null)CreateOpening(lobby,state,grid);
        return state with{Input=input,Result=result,Results=results,Attempts=(state.Attempts??[]).Append(input).ToArray(),Grid=grid,
            Lifecycle=grid is null?"session-result":"grid-ready",Deadline=grid is null&&!state.Paused?due:null,
            Wakeup=grid is null&&!state.Paused?new(lobby.Id,Guid.NewGuid().ToString("N"),"qualifying-presentation",Stage(state),state.WindowRevision,due):null};
    }
    private static QualifyingState ReadQualifying(Lobby lobby)
    {
        if(!lobby.ModuleState.TryGetValue(QualifyingModule,out var json))throw new SessionFault(SessionFailure.Conflict,"Begin qualifying first.");
        var state=JsonSerializer.Deserialize<QualifyingState>(json,BrowserReplay.JsonOptions)!;
        if(state.Version!=QualifyingModule||state.Input.ModelVersion!=Qualifying.ModelVersion||state.Input.RandomnessVersion!=Qualifying.RandomnessVersion||state.Input.Rules.Version!=Qualifying.RulesVersion||state.Input.Weekends.Any(w=>w.ModelVersion!=WeekendPreparation.ModelVersion||w.SetupModelVersion!=WeekendSetup.Version))
            throw new SessionFault(SessionFailure.Conflict,"Saved qualifying needs its compatible deployment; inputs are preserved.");
        if(state.Result is not null)
        {
            try
            {
                var replay=Qualifying.Resolve(state.Input);
                if(replay.ModelVersion!=state.Result.ModelVersion||replay.RandomnessVersion!=state.Result.RandomnessVersion
                    ||replay.RulesVersion!=state.Result.RulesVersion||!replay.Ranking.SequenceEqual(state.Result.Ranking))
                    throw new ArgumentException("Recorded qualifying differs from its replay.");
            }
            catch(ArgumentException){throw new SessionFault(SessionFailure.Conflict,"Saved qualifying replay is incompatible. Preserve its recorded inputs and result.");}
        }
        if(state.Result is not null&&state.Results is null)state=state with{Results=[state.Result],Attempts=[state.Input]};
        if(state.Attempts is {} attempts&&state.Results is {} results)
        {
            if(attempts.Length!=results.Length)throw new SessionFault(SessionFailure.Conflict,"Recorded qualifying history is incomplete.");
            for(var i=0;i<attempts.Length;i++)
                if(!Qualifying.Resolve(attempts[i]).Ranking.SequenceEqual(results[i].Ranking))throw new SessionFault(SessionFailure.Conflict,"Recorded qualifying history is incompatible.");
            if(state.Grid is {} grid&&!grid.SequenceEqual(Qualifying.Grid(results)))throw new SessionFault(SessionFailure.Conflict,"Recorded grid is incompatible.");
        }
        return state;
    }
    private static void WriteQualifying(Lobby lobby,QualifyingState state)=>lobby.ModuleState[QualifyingModule]=JsonSerializer.Serialize(state,BrowserReplay.JsonOptions);
    private static int Stage(QualifyingState state)=>int.Parse(state.Session[1..],System.Globalization.CultureInfo.InvariantCulture);
    private static string[] RequiredQualifyingTeams(Lobby lobby,QualifyingState state)=>lobby.RequiredTeams.Where(team=>QualifyingDrivers(state).Any(d=>d.TeamId==team)).ToArray();
    private static QualifyingDriver[] AllQualifyingDrivers(QualifyingState state)=>state.Input.Weekends.SelectMany(w=>w.Cars)
        .Select(c=>new QualifyingDriver(c.Source.EntryId,c.Source.Driver.Id,c.Source.Driver.Name,c.Source.Driver.Slug,c.Source.TeamId)).ToArray();
    private static QualifyingDriver[] QualifyingDrivers(QualifyingState state)=>state.Input.Weekends.SelectMany(w=>w.Cars).Where(c=>state.Input.PreSessionOrder.Contains(c.Source.EntryId))
        .Select(c=>new QualifyingDriver(c.Source.EntryId,c.Source.Driver.Id,c.Source.Driver.Name,c.Source.Driver.Slug,c.Source.TeamId)).ToArray();
    private static QualifyingPublicSnapshot ProjectQualifying(Lobby lobby,QualifyingState state)
    {
        var circuit=state.Input.Weekends[0].Source.Circuit;
        return new(lobby.Id,state.Revision,state.Session,state.Lifecycle,state.Paused,circuit.Id,circuit.Name,circuit.Slug,AllQualifyingDrivers(state),
            state.Input.Weekends.Select(w=>w.Cars[0].Source.TeamId).Where(team=>QualifyingDrivers(state).Any(d=>d.TeamId==team)).Select(team=>new QualifyingTeamStatus(team,state.Locks.ContainsKey(team),state.Locks.GetValueOrDefault(team)?.Source)).ToArray(),
            state.Result,state.Lifecycle=="awaiting-decisions"?state.Deadline:null,state.TimingPolicy,state.Input.Rules.Version,state.Input.Rules.Retain,state.TimingPolicy.TimeoutPolicy,state.Results??[],state.Grid??[],state.WindowRevision,state.Lifecycle=="session-result"?state.Deadline:null);
    }
    private static QualifyingTeamSnapshot ProjectQualifyingTeam(Lobby lobby,QualifyingState state,string team,Assignment assignment)=>new(lobby.Id,team,state.WindowRevision,assignment.Revision,state.Session,
        state.Lifecycle,state.Locks.ContainsKey(team),QualifyingDrivers(state).Where(c=>c.TeamId==team).ToArray(),state.Locks.GetValueOrDefault(team)?.Choices,ProjectQualifyingAdvice(state,team),state.Locks.GetValueOrDefault(team)?.Source,state.Locks.GetValueOrDefault(team)?.PolicyVersion);
    public IReadOnlyList<QualifyingTeamSnapshot> QualifyingControllerUpdates(string id)
    {lock(gate){var lobby=RequireLobby(id);var state=ReadQualifying(lobby);return lobby.Assignments.Select(p=>ProjectQualifyingTeam(lobby,state,p.Key,p.Value)).ToArray();}}
}
