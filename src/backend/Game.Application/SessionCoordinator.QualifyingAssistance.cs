using Game.Domain;
using Game.Simulation;
namespace Game.Application;

public sealed partial class SessionCoordinator
{
    private const string QualifyingTimeoutDisclosure="Unclaimed and missing teams use frozen observation-only choices (qualifying-observation-v1). Safe lowers ceiling/error exposure; Push is normal maximum effort; All-Out raises potential, variance and error exposure. Outcomes remain uncertain. Explicit auto-accept uses recorded advice only at expiry; accepted human locks are preserved.";
    private sealed record RecordedQualifyingAdvice(QualifyingPolicyPlan Plan,QualifyingAssistance Assistance);
    private static QualifyingState FreezeQualifyingPolicies(Lobby lobby,QualifyingState state)
    {
        var preparation=ReadPreparation(lobby);var advice=new Dictionary<string,RecordedQualifyingAdvice>();
        foreach(var group in QualifyingDrivers(state).GroupBy(d=>d.TeamId))
        {
            var feedback=preparation.Teams[group.Key].Practice?.Result.Feedback;
            var observation=new QualifyingPolicyObservation(state.Session,group.Key,group.Select(driver=>
            {
                var disclosed=feedback?.Drivers.FirstOrDefault(d=>d.DriverId==driver.DriverId);
                return new QualifyingPolicyDriver(driver.EntryId,disclosed?.Fit,disclosed?.Confidence);
            }).ToArray(),feedback?.ModelVersion??"no-practice-feedback-v1");
            var plan=QualifyingDecisionPolicy.Evaluate(observation);
            var assignment=lobby.Assignments.GetValueOrDefault(group.Key);
            var settings=assignment is null?new PlayerAssistanceSettings():AssistanceSettings(lobby,assignment.PlayerId);
            advice[group.Key]=new(plan,new(settings.HelpLevel,settings.AutoAcceptRecommendation,state.Session,state.WindowRevision,plan.PolicyVersion,plan.Cars));
            var choices=plan.Cars.Select(c=>c.Choice).ToArray();
            if(lobby.ScriptedTeams.Contains(group.Key))state.Locks[group.Key]=new("scripted-ai",plan.PolicyVersion,choices);
            else state.TimeoutLocks[group.Key]=new(settings.AutoAcceptRecommendation?"recommendation-timeout":"timeout-ai",plan.PolicyVersion,choices);
        }
        return state with{Assistance=advice};
    }
    private static QualifyingAssistance? ProjectQualifyingAdvice(QualifyingState state,string team)
    {
        var advice=state.Assistance?.GetValueOrDefault(team)?.Assistance;
        return advice?.HelpLevel=="Standard"?advice with{Cars=null}:advice;
    }
}
