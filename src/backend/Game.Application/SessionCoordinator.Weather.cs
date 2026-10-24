using Game.Domain;
namespace Game.Application;
public sealed partial class SessionCoordinator
{
    private static bool WeatherSafetyCarActive(Session session)=>session.Engine.GetState().WeatherReaction?.SafetyCarActive==true;
    private static IReadOnlyDictionary<string,DriverDecision> FreezeWeather(Session session,IEnumerable<string> teams)
    {
        var decisions=new Dictionary<string,DriverDecision>();
        foreach(var team in teams)
        {
            var plan=ObservationDecisionPolicy.Evaluate(new(session.Phase,session.Configuration.Laps,
                session.Engine.GetTeamObservation(team),session.Engine.GetTimingObservation(),
                session.Configuration.WetTyres is not null,WeatherSafetyCarActive(session)));
            foreach(var car in plan.Cars)decisions[car.DriverId]=car.Decision;
        }
        return decisions;
    }
}
