using Game.Domain;
using Game.Simulation;
using System.Collections.ObjectModel;
namespace Game.Application;
internal static class OpponentScript
{
    public const string Version = "dry-opponents-v1";
    public static IReadOnlyDictionary<string, DriverDecision> Freeze(RaceEngine engine, IEnumerable<string> teams, int laps)
    {
        var decisions = new Dictionary<string, DriverDecision>();
        foreach (var team in teams.Where(team => team != "team-01"))
            {
            var observation = engine.GetTeamObservation(team);
            foreach (var tyre in observation.TeamTyres)
            {
                var pace = tyre.EstimatedCondition < .5 ? PaceInstruction.Conserve : PaceInstruction.Standard;
                var needsScheduledStop = observation.LeaderLap >= laps / 2 && tyre.Compound == TyreCompound.Medium;
                TyreCompound? pit = !tyre.InPits && !tyre.StopQueued && (needsScheduledStop || tyre.EstimatedCondition < .35)
                    ? tyre.Compound == TyreCompound.Hard ? TyreCompound.Soft : TyreCompound.Hard : null;
                decisions.Add(tyre.DriverId, new(pace, pit, BattleInstruction.Neutral));
            }
        }
        return new ReadOnlyDictionary<string, DriverDecision>(decisions);
    }
}
public interface ISessionNotifications
{ Task Publish(PublicSnapshot display, TeamSnapshot team); }
