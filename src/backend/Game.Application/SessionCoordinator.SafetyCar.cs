using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public sealed partial class SessionCoordinator
{
    private static SafetyCarWindow? ProjectSafetyCar(Session session)=>session.Engine.GetState().Interruption is {} interruption
        ?new(interruption.Identity,interruption.Time,interruption.DriverId,interruption.Kind.ToString()):null;
    private static string[] ExpectedCommandDrivers(Session session,string team)=>session.WindowKind=="SafetyCar"
        ?session.Engine.GetSafetyCarOptions().Where(option=>option.Status=="available"&&session.Drivers.Single(driver=>driver.Id==option.DriverId).Team==team).Select(option=>option.DriverId).ToArray()
        :session.Engine.GetSafetyCarOptions().Where(option=>option.Status is not ("retired" or "finished")&&session.Drivers.Single(driver=>driver.Id==option.DriverId).Team==team).Select(option=>option.DriverId).ToArray();
    private static PhaseResult ResolveOperation(RaceEngine engine,string kind,IReadOnlyDictionary<string,DriverDecision> plan)=>BrowserReplay.ResolveOperation(engine,kind,plan);
    private static IReadOnlyDictionary<string,DriverDecision> FreezeSafetyCar(Session session,IEnumerable<string> teams)
    {
        var legal=session.Engine.GetSafetyCarOptions().Where(option=>option.Status=="available").Select(option=>option.DriverId).ToHashSet();
        return OpponentScript.Freeze(session.Engine,teams,session.Configuration.Laps,session.ScriptVersion)
            .Where(pair=>legal.Contains(pair.Key)).ToDictionary(pair=>pair.Key,pair=>new DriverDecision(PitCompound:pair.Value.PitCompound));
    }
    public Task<SubmissionAck> SubmitSafetyCar(string id,string? capability,SafetyCarTeamCommand input)=>Submit(id,capability,NarrowSafetyCar(input));
    public Task<SubmissionAck> SubmitHouseholdSafetyCar(string id,AuthIdentity identity,string deviceId,string teamId,SafetyCarTeamCommand input)
        =>SubmitHousehold(id,identity,deviceId,teamId,NarrowSafetyCar(input));
    private static TeamCommand NarrowSafetyCar(SafetyCarTeamCommand input)
    {
        if(input?.Cars is null||input.Cars.Any(car=>car is null))throw new SessionFault(SessionFailure.InvalidInput,"Choose legal Safety Car pit instructions.");
        return new(input.CommandId,input.Phase,input.Revision,input.Cars.Select(car=>new CarCommand(car.DriverId,"Standard",car.Pit,car.Compound,"Neutral")).ToArray(),"SafetyCar");
    }
}
