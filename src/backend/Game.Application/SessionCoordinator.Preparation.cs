using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;

public sealed partial class SessionCoordinator
{
    private const string PreparationModule = "weekend-preparation-v1";
    private sealed record AcceptedSetup(string Fingerprint, SetupAck Ack);
    private sealed record PreparedTeam(WeekendPreset Preset, WeekendSetup Setup, EffectiveWeekend Effective,
        long Revision, string Lifecycle, Dictionary<string, AcceptedSetup> Commands);
    private sealed record PreparedLobby(string Version, Dictionary<string, PreparedTeam> Teams);

    public PreparationPublicSnapshot BeginPreparation(string id, AuthIdentity identity, BeginPreparationInput input)
    {
        mutations.Wait();
        try { lock (gate)
        {
            var lobby = RequireLobby(id); RequireHost(lobby, identity);
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
                    var opened = WeekendPresetCatalog.Open(input.CircuitId, pair.First, pair.Second);
                    var preset = opened with { Cars = opened.Cars.Select((car,i) => car with
                        { TeamId=teamId, EntryId=$"{teamId}-car-{i+1}" }).ToImmutableArray() };
                    var setup = new WeekendSetup();
                    teams.Add(teamId,new(preset,setup,WeekendPreparation.Prepare(preset,lobby.Input.Laps!.Value,setup),1,"provisional",[]));
                }
            }
            catch (ArgumentException error) { throw new SessionFault(SessionFailure.InvalidInput,error.Message); }
            lobby.RequiredTeams=lobby.Assignments.Keys.Order(StringComparer.Ordinal).ToArray();
            lobby.ScriptedTeams=teams.Keys.Except(lobby.RequiredTeams).Order(StringComparer.Ordinal).ToArray();
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
            if(command is null || !Guid.TryParse(command.CommandId,out var commandId) || commandId==Guid.Empty
                || command.Revision is null || command.AssignmentRevision is null || command.Setup is null)
                throw new SessionFault(SessionFailure.InvalidInput,"Supply command identity, current revisions and all three setup choices.");
            if(command.AssignmentRevision!=access.Assignment.Revision)
                throw new SessionFault(SessionFailure.Conflict,"Controller assignment changed. Refresh current authority.");
            try { command.Setup.Validate(); }
            catch(ArgumentException error) { throw new SessionFault(SessionFailure.InvalidInput,error.Message); }
            var fingerprint=JsonSerializer.Serialize(new {Operation="setup",Session=id,Team=teamId,command.Revision,
                command.AssignmentRevision,command.Setup,Version=PreparationModule});
            var key=commandId.ToString("D");
            if(team.Commands.TryGetValue(key,out var accepted))
            {
                if(accepted.Fingerprint!=fingerprint)throw new SessionFault(SessionFailure.Conflict,"Command identity was already used with different setup inputs.");
                return accepted.Ack;
            }
            if(lobby.Lifecycle!="preparing" || team.Lifecycle!="provisional")
                throw new SessionFault(SessionFailure.Conflict,"Setup is not editable in this lifecycle.");
            if(command.Revision!=team.Revision)throw new SessionFault(SessionFailure.Conflict,"Setup changed. Recover the saved revision before editing.");
            var effective=WeekendPreparation.Prepare(team.Preset,lobby.Input.Laps!.Value,command.Setup);
            var next=team with {Setup=command.Setup,Effective=effective,Revision=team.Revision+1};
            var ack=new SetupAck(key,id,teamId,next.Revision,next.Lifecycle);
            next.Commands[key]=new(fingerprint,ack); state.Teams[teamId]=next;
            lobby.Revision++; WritePreparation(lobby,state); Persist(id); return ack;
        }} finally { mutations.Release(); }
    }

    private static PreparedLobby ReadPreparation(Lobby lobby)
    {
        if(!lobby.ModuleState.TryGetValue(PreparationModule,out var json))
            throw new SessionFault(SessionFailure.Conflict,"Begin weekend preparation first.");
        var state=JsonSerializer.Deserialize<PreparedLobby>(json,BrowserReplay.JsonOptions);
        if(state is null || state.Version!=PreparationModule || state.Teams.Count!=11
            || state.Teams.Values.Any(t=>t.Effective.SetupModelVersion!=WeekendSetup.Version))
            throw new SessionFault(SessionFailure.Conflict,"Saved preparation model is incompatible; preserved inputs require the compatible deployment.");
        return state;
    }
    private static void WritePreparation(Lobby lobby,PreparedLobby state)=>
        lobby.ModuleState[PreparationModule]=JsonSerializer.Serialize(state,BrowserReplay.JsonOptions);
    private static PreparationPublicSnapshot ProjectPreparation(Lobby lobby,PreparedLobby state)=>
        new(lobby.Id,lobby.Revision,lobby.Lifecycle,state.Teams.First().Value.Preset.Circuit,state.Teams.Keys.ToArray());
    private static PreparationTeamSnapshot ProjectPreparationTeam(Lobby lobby,PreparedTeam team,string teamId,Assignment assignment)=>
        new(lobby.Id,teamId,team.Revision,assignment.Revision,team.Lifecycle,team.Setup,team.Preset,
            team.Effective.DriverFits,team.Effective.Summary,team.Effective.SetupModelVersion);

    // Explicit preset identity; constructor metadata and illustrative card stars never provide coefficients.
    private static readonly (string First,string Second)[] PreparationDrivers = [
        ("driver-lando-norris","driver-oscar-piastri"), ("driver-charles-leclerc","driver-lewis-hamilton"),
        ("driver-max-verstappen","driver-isack-hadjar"), ("driver-george-russell","driver-andrea-kimi-antonelli"),
        ("driver-fernando-alonso","driver-lance-stroll"), ("driver-pierre-gasly","driver-franco-colapinto"),
        ("driver-esteban-ocon","driver-oliver-bearman"), ("driver-liam-lawson","driver-arvid-lindblad"),
        ("driver-alexander-albon","driver-carlos-sainz"), ("driver-nico-hulkenberg","driver-gabriel-bortoleto"),
        ("driver-sergio-perez","driver-valtteri-bottas")];
}
