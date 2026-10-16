using Game.Domain;
using Game.Simulation;
using System.Collections.ObjectModel;
namespace Game.Application;
internal static class OpponentScript
{
    public const string Version=ObservationDecisionPolicy.Version;
    public const string LegacyVersion="dry-opponents-v1";
    public static bool Supports(string version)=>version is Version or LegacyVersion;
    public static IReadOnlyDictionary<string,DriverDecision> Freeze(RaceEngine engine,IEnumerable<string> teams,int laps,string version=Version)
    {
        var decisions=new Dictionary<string,DriverDecision>();
        foreach(var team in teams.Order(StringComparer.Ordinal))
        {
            var observation=engine.GetTeamObservation(team);
            if(version==LegacyVersion)
            {
                foreach(var tyre in observation.TeamTyres)
                {
                    var pace=tyre.EstimatedCondition<.5?PaceInstruction.Conserve:PaceInstruction.Standard;
                    var needsStop=observation.LeaderLap>=laps/2&&tyre.Compound==TyreCompound.Medium;
                    TyreCompound? pit=!tyre.InPits&&!tyre.StopQueued&&(needsStop||tyre.EstimatedCondition<.35)
                        ?tyre.Compound==TyreCompound.Hard?TyreCompound.Soft:TyreCompound.Hard:null;
                    decisions.Add(tyre.DriverId,new(pace,pit));
                }
            }
            else
            {
                var plan=ObservationDecisionPolicy.Evaluate(new(observation.LeaderLap/5,laps,observation,engine.GetTimingObservation()));
                foreach(var car in plan.Cars)decisions.Add(car.DriverId,car.Decision);
            }
        }
        return new ReadOnlyDictionary<string,DriverDecision>(decisions);
    }
}
public interface ISessionNotifications
{ Task PublishQualifying(QualifyingPublicSnapshot display,IReadOnlyList<QualifyingTeamSnapshot> teams,IReadOnlyList<QualifyingTeamSnapshot> openTeams)=>Task.CompletedTask;
  Task Publish(PublicSnapshot display,TeamSnapshot team);
  Task PublishHousehold(PublicSnapshot display,IReadOnlyList<TeamSnapshot> teams,IReadOnlyList<OpenTeamSnapshot> openTeams); }
