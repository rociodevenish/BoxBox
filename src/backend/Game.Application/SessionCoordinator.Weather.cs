using Game.Domain;
namespace Game.Application;
public sealed partial class SessionCoordinator
{
    private static bool WeatherSafetyCarActive(Session session)=>session.Engine.GetState().WeatherReaction?.SafetyCarActive==true;
    // A small legitimate fallback until the complete observation-only weather advice policy lands.
    // It uses only current public wetness and own-car legal stop state, never the seeded future.
    private static IReadOnlyDictionary<string,DriverDecision> FreezeWeather(Session session,IEnumerable<string> teams)
    {
        var decisions=new Dictionary<string,DriverDecision>();
        foreach(var team in teams)
        {
            var observed=session.Engine.GetTeamObservation(team);
            var compound=observed.Weather!.Wetness>=session.Configuration.WeatherReactions!.WetBoundary?TyreCompound.Wet
                :observed.Weather.Wetness>=session.Configuration.WeatherReactions.DryBoundary?TyreCompound.Intermediate:TyreCompound.Medium;
            foreach(var tyre in observed.TeamTyres)
            {
                if(!ExpectedCommandDrivers(session,team).Contains(tyre.DriverId))continue;
                decisions[tyre.DriverId]=new(WeatherSafetyCarActive(session)?PaceInstruction.Conserve:PaceInstruction.Standard,
                    !tyre.InPits&&!tyre.StopQueued&&tyre.Compound!=compound?compound:null);
            }
        }
        return decisions;
    }
}
