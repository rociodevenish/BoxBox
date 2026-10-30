using System.Collections.Immutable;
using Game.Domain;
namespace Game.Simulation;

// Own source capabilities already include permanent development. No setup, entropy or opponent data.
public sealed record PackageSelectionObservation(string CircuitId,ImmutableDictionary<string,double> CircuitDemands,
 ImmutableDictionary<string,double> SourceRatings,ImmutableArray<RacePackageVersion> Library,bool ReliabilityActive=false);
public sealed record PackageFitComparison(RacePackageSelection Selection,string Name,double FitScore);
public sealed record PackageSelectionRecommendation(string PolicyVersion,RacePackageSelection Selection,
 ImmutableArray<PackageFitComparison> Comparisons,string Explanation);
public sealed record RecordedPackageSelection(string Source,PackageSelectionObservation Observation,
 PackageSelectionRecommendation Recommendation,RacePackageSelection Selection);
public static class PackageSelectionDecisionPolicy
{
 public const string Version="public-package-demand-fit-v1";
 public static PackageSelectionRecommendation Evaluate(PackageSelectionObservation observation)
 {
  if(string.IsNullOrWhiteSpace(observation.CircuitId)||observation.Library.IsDefaultOrEmpty
   ||observation.Library.GroupBy(p=>(p.PackageId,p.Version)).Any(g=>g.Count()!=1)
   ||WeekendPreparation.CarKeys.Any(k=>!observation.SourceRatings.TryGetValue(k,out var v)||!double.IsFinite(v)||v is <1 or >100)
   ||WeekendPreparation.DemandKeys.Any(k=>!observation.CircuitDemands.TryGetValue(k,out var v)||!double.IsFinite(v)||v is <1 or >100))
   throw new ArgumentException("Observe the own completed library, bounded source capabilities and public circuit demands.");
  var entries=observation.Library.Where(p=>p.Completed&&(p.ApplicableCircuits.IsDefaultOrEmpty||p.ApplicableCircuits.Contains(observation.CircuitId))).ToArray();
  foreach(var p in entries)
   if(p.ModelVersion!=RacePackages.Version||p.Version<1||string.IsNullOrWhiteSpace(p.PackageId)||string.IsNullOrWhiteSpace(p.Provenance)
    ||p.Strengths.Any(c=>!WeekendPreparation.CarKeys.Contains(c.Key)||!double.IsFinite(c.Value)||c.Value is <0 or >6)
    ||p.Compromises.Any(c=>!WeekendPreparation.CarKeys.Contains(c.Key)||!double.IsFinite(c.Value)||c.Value is >0 or < -6))
    throw new ArgumentException("Observe valid completed package versions.");
  if(!entries.Any(p=>p==RacePackages.Baseline||p.PackageId=="neutral-package-v1"&&p.Version==1&&p.Strengths.Count==0&&p.Compromises.Count==0))
   throw new ArgumentException("A completed neutral fallback is required.");
  var comparisons=entries.Select(p=>new PackageFitComparison(new(p.PackageId,p.Version),p.Name,
   CircuitDemandFit.Score(p.Strengths,observation.SourceRatings,observation.CircuitDemands,observation.ReliabilityActive,normalization:1000,decimalPlaces:null,compromises:p.Compromises)))
   .OrderByDescending(p=>p.FitScore).ThenBy(p=>p.Selection.PackageId,StringComparer.Ordinal).ThenByDescending(p=>p.Selection.Version).ToImmutableArray();
  // Prefer the free neutral fallback when every usable option has non-positive net fit.
  var selected=comparisons[0].FitScore<=0?new RacePackageSelection("neutral-package-v1",1):comparisons[0].Selection;
  return new(Version,selected,comparisons,"Free selection compares all ten clamped strength and compromise changes against public circuit demands. Reliability has weight 50 only with an active mechanical-failure consumer, otherwise zero. Equal positive fit uses stable package identity then newest version; non-positive fit retains neutral. This bounded design-score comparison is not a lap-time prediction; driver fit, race conditions and outcomes remain uncertain. No development or credits are spent.");
 }
}
