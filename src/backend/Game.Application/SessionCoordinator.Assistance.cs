using System.Text.Json;
using Game.Domain;
namespace Game.Application;
public sealed partial class SessionCoordinator
{
    private const string AssistancePrefix="assistance:";
    private const string AssistanceHistoryKey="assistance-history";
    public async Task<LobbySnapshot> SetPlayerAssistance(string id,AuthIdentity identity,string playerId,PlayerAssistanceInput input)
    {
        if(input is null||input.HelpLevel is not ("Standard" or "Assisted" or "Guided"))throw new SessionFault(SessionFailure.InvalidInput,"Choose Standard, Assisted or Guided help.");
        await mutations.WaitAsync();try
        {
            lock(gate)
            {
                var lobby=RequireLobby(id);RequireHost(lobby,identity);
                if(!lobby.Players.Any(player=>player.Id==playerId))throw new SessionFault(SessionFailure.InvalidInput,"Choose a household player.");
                lobby.ModuleState[AssistancePrefix+playerId]=JsonSerializer.Serialize(new PlayerAssistanceSettings(input.HelpLevel,input.AutoAcceptRecommendation),BrowserReplay.JsonOptions);
                lobby.Revision++;Persist(id);return ProjectLobby(lobby);
            }
        }finally{mutations.Release();}
    }
    private static PlayerAssistanceSettings AssistanceSettings(Lobby lobby,string playerId)=>
        lobby.ModuleState.TryGetValue(AssistancePrefix+playerId,out var saved)?JsonSerializer.Deserialize<PlayerAssistanceSettings>(saved,BrowserReplay.JsonOptions)!:new();
    private void ConfigureSafetyCarAdvice(Session session,string teamId,ref string source)
    {
        var lobby=RequireLobby(session.Id);var settings=AssistanceSettings(lobby,lobby.Assignments[teamId].PlayerId);
        var observation=session.Engine.GetTeamObservation(teamId);
        var cars=ExpectedCommandDrivers(session,teamId).Select(id=>{
            var decision=session.FrozenTimeoutDecisions[id];
            return new SafetyCarAdvice(id,new(id,decision.PitCompound is null?"Stay Out":"Pit",decision.PitCompound?.ToString()),
                ["Safety Car deployed after the observed "+session.Engine.GetState().Interruption!.Kind+". Racing is neutralised; accepted stops remain committed.",
                 decision.PitCompound is null?"Keep track position with the current estimated tyres.":"Consider fresh tyres and a different dry compound at reduced Safety Car Pit Loss."],
                "Saved from this window's own tyre estimates and public incident facts. Re-entry order and future failures remain uncertain.");
        }).ToArray();
        var recommendation=new AssistanceRecommendation(session.Phase,session.WindowRevision,observation.Time,[],
            "Choose only Stay Out or Pit with a compound for available cars. Queued and ongoing stops cannot be replaced.","safety-car-observed-v1",cars);
        session.ModuleState[AssistancePrefix+teamId]=JsonSerializer.Serialize(new TeamAssistance(settings.HelpLevel,settings.AutoAcceptRecommendation,session.Phase,recommendation),BrowserReplay.JsonOptions);
        var history=AssistanceHistory(session)?.ToList()??[];
        if(!history.Any(entry=>entry.Phase==session.Phase&&entry.TeamId==teamId&&entry.PolicyVersion=="safety-car-observed-v1"))
        {history.Add(new(session.Phase,teamId,settings.HelpLevel,settings.AutoAcceptRecommendation,"safety-car-observed-v1",recommendation));session.ModuleState[AssistanceHistoryKey]=JsonSerializer.Serialize(history,BrowserReplay.JsonOptions);}
        if(settings.AutoAcceptRecommendation){source="recommendation-timeout";session.ModuleState["automatic-policy:"+teamId]="safety-car-observed-v1";}
    }
    partial void ConfigureTimeoutPlan(Session session,string teamId,ref Dictionary<string,DriverDecision> decisions,ref string source)
    {
        session.ModuleState.Remove("automatic-policy:"+teamId);
        var lobby=RequireLobby(session.Id);var playerId=lobby.Assignments[teamId].PlayerId;var settings=AssistanceSettings(lobby,playerId);
        var plan=ObservationDecisionPolicy.Evaluate(new(session.Phase,session.Configuration.Laps,session.Engine.GetTeamObservation(teamId),session.Engine.GetTimingObservation()));
        var cars=plan.Cars.Select(car=>new AssistanceCar(car.DriverId,new(car.DriverId,car.Decision.Pace.ToString(),car.Decision.PitCompound is null?"Stay Out":"Pit",car.Decision.PitCompound?.ToString(),car.Decision.Battle.ToString()),
            car.Reasons,car.Uncertainty,car.PaceOptions.Select(option=>option.ToString()).ToArray(),car.PitOptions.Select(option=>option?.ToString()).ToArray(),car.BattleOptions.Select(option=>option.ToString()).ToArray(),car.Confident)).ToArray();
        var recommendation=new AssistanceRecommendation(session.Phase,session.WindowRevision,plan.ObservationTime,cars,
            "All legal choices remain available because the estimates do not justify narrowing them. Forced stop or battle states are explained for each car.",plan.PolicyVersion);
        // Save the entire phase-start advice even for Standard. Its private projection omits it.
        session.ModuleState[AssistancePrefix+teamId]=JsonSerializer.Serialize(new TeamAssistance(settings.HelpLevel,settings.AutoAcceptRecommendation,session.Phase,recommendation),BrowserReplay.JsonOptions);
        var history=AssistanceHistory(session)?.ToList()??[];
        if(!history.Any(entry=>entry.Phase==session.Phase&&entry.TeamId==teamId))
        {
            history.Add(new(session.Phase,teamId,settings.HelpLevel,settings.AutoAcceptRecommendation,plan.PolicyVersion,recommendation));
            session.ModuleState[AssistanceHistoryKey]=JsonSerializer.Serialize(history,BrowserReplay.JsonOptions);
        }
        if(settings.AutoAcceptRecommendation)
        {
            decisions=plan.Cars.ToDictionary(car=>car.DriverId,car=>car.Decision);source="recommendation-timeout";session.ModuleState["automatic-policy:"+teamId]=plan.PolicyVersion;
        }
    }
    private static IReadOnlyList<RecordedAssistance>? AssistanceHistory(Session session)=>
        session.ModuleState.TryGetValue(AssistanceHistoryKey,out var history)?JsonSerializer.Deserialize<RecordedAssistance[]>(history,BrowserReplay.JsonOptions):null;
    private static TeamAssistance? ProjectAssistance(Session session,string teamId)
    {
        if(!session.ModuleState.TryGetValue(AssistancePrefix+teamId,out var saved))return null;
        var help=JsonSerializer.Deserialize<TeamAssistance>(saved,BrowserReplay.JsonOptions)!;
        return help.HelpLevel=="Standard"||help.Phase!=session.Phase?help with{Phase=session.Phase,Recommendation=null}:help;
    }
}
