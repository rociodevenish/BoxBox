using System.Collections.Immutable;
using Game.Domain;
namespace Game.Simulation;
public static partial class DevelopmentDecisionPolicy
{
 public const string PackageVersion="public-library-demand-credit-reserve-v2";
 private sealed record Alternative(string Action,string Id,int Cost,double Value,PackageDevelopmentQuote? Package=null);
 private static DevelopmentRecommendation EvaluateAlternatives(DevelopmentPolicyObservation observation)
 {
  if(observation.CircuitDemands is null||observation.BaseRatings is null||observation.Library.IsDefaultOrEmpty
   ||!observation.Library.Any(p=>p.PackageId=="neutral-package-v1"&&p.Version==1&&p.Completed)
   ||observation.Library.Any(p=>!p.Completed||p.ModelVersion!=RacePackages.Version||p.Version<1||string.IsNullOrWhiteSpace(p.PackageId)||string.IsNullOrWhiteSpace(p.Provenance)||!p.ApplicableCircuits.IsDefaultOrEmpty||p.Strengths.Any(c=>!WeekendPreparation.CarKeys.Contains(c.Key)||!double.IsFinite(c.Value)||c.Value is <0 or >6)||p.Compromises.Any(c=>!WeekendPreparation.CarKeys.Contains(c.Key)||!double.IsFinite(c.Value)||c.Value is >0 or < -6))
   ||observation.Library.Select(p=>(p.PackageId,p.Version)).Distinct().Count()!=observation.Library.Length
   ||observation.BaseRatings.Count!=WeekendPreparation.CarKeys.Length||WeekendPreparation.CarKeys.Any(key=>!observation.BaseRatings.TryGetValue(key,out var value)||!double.IsFinite(value)||value is <1 or >100))throw new ArgumentException("Observe the actual completed all-circuit library, baseline and own frozen car ratings.");
  var calendar=observation.UpcomingCircuits.IsDefaultOrEmpty?ImmutableArray.Create(observation.CircuitDemands):observation.UpcomingCircuits;
  if(calendar.Length!=observation.RemainingOpportunities+1&& !observation.UpcomingCircuits.IsDefaultOrEmpty)throw new ArgumentException("Observe exactly the known remaining circuit demands.");
  foreach(var demands in calendar)
   if(demands.Count!=WeekendPreparation.DemandKeys.Length||WeekendPreparation.DemandKeys.Any(key=>!demands.TryGetValue(key,out var value)||!double.IsFinite(value)||value is <0 or >100))throw new ArgumentException("Observe bounded public circuit demands.");
  var candidates=new List<Alternative>();
  foreach(var quote in observation.Options)
  {
   if(observation.GainCapacity[quote.Option.Id]<=0)continue;
   var distribution=quote.Distribution??new DevelopmentRiskControls(DevelopmentRiskControls.Model,quote.NormalProbability,quote.PartialProbability,0,0,quote.DelayProbability);
   distribution.Validate();var value=0d;
   foreach(var (kind,probability) in Categories(distribution))
   for(var index=kind=="delay"?1:0;index<calendar.Length;index++)
   {
    var gain=kind is "partial" or "compromised"?quote.Option.MinimumGain:kind=="breakthrough"?quote.Option.MaximumGain+1:(quote.Option.MinimumGain+quote.Option.MaximumGain)/2d;
    gain=Math.Min(gain,observation.GainCapacity[quote.Option.Id]);
    var changes=ImmutableDictionary<string,double>.Empty.Add(quote.Option.Attribute,gain);
    if(kind!="partial")changes=changes.SetItem(quote.Option.Id switch{"handling"=>"topSpeed","tyres"=>"tyreTemperature",_=>"accelerationDeployment"},-1);
    if(kind=="compromised")changes=changes.SetItem(quote.Option.Attribute=="reliability"?"topSpeed":"reliability",-1);
    value+=probability*Fit(changes,observation.BaseRatings,calendar[index],observation.IncidentsEnabled&&index==0);
   }
   candidates.Add(new("PermanentRefinement",quote.Id,quote.Cost,Math.Round(value,6)));
  }
  if(observation.PackageOptions.Select(q=>q.Id).Distinct().Count()!=observation.PackageOptions.Length)throw new ArgumentException("Observe distinct package quotes.");
  foreach(var quote in observation.PackageOptions)
  {
   PackageDevelopment.ValidateQuote(quote);
   if(quote.SourcePackage is {} source&&!observation.Library.Any(p=>SameVersion(p,source)))throw new ArgumentException("Refinement quote must reference an exact completed own version.");
   var distribution=quote.Distribution??DevelopmentRiskControls.Only("normal");var value=0d;
   foreach(var (kind,probability) in Categories(distribution))
   {
    var outcome=PackageDevelopment.PreviewOutcome(quote,kind=="delay"?"normal":kind);
    for(var index=kind=="delay"?1:0;index<calendar.Length;index++)
    {
     var best=observation.Library.Max(p=>PackageFit(p,observation.BaseRatings,calendar[index],observation.IncidentsEnabled&&index==0));
     // Free selection retains an existing completed fallback whenever new work is worse.
     value+=probability*Math.Max(0,PackageFit(outcome,observation.BaseRatings,calendar[index],observation.IncidentsEnabled&&index==0)-best);
    }
   }
   candidates.Add(new(quote.SourcePackage is null?"PackageCreation":"PackageRefinement",quote.Id,quote.Cost,Math.Round(value,6),quote));
  }
  var useful=candidates.Where(c=>c.Value>0).ToArray();
  var reserve=observation.RemainingOpportunities>0&&useful.Length>0?useful.Min(c=>c.Cost):0;
  var affordable=useful.Where(c=>c.Cost<=observation.Balance).ToArray();
  var chosen=affordable.Where(c=>observation.Balance-c.Cost>=reserve).OrderByDescending(c=>Math.Round(c.Value/c.Cost,6)).ThenBy(c=>c.Cost).ThenBy(c=>c.Id,StringComparer.Ordinal).FirstOrDefault();
  const string uncertainty="Uses only quoted outcome ranges, own completed library, frozen car ratings and known circuit demands. Indicative demand-fit points per credit, not lap time or a guaranteed result. Existing package fallbacks remain available. Reliability counts only for an explicitly active target-event mechanical-failure consumer; later activation is not assumed. Future draws, opponent plans and future weather are unknown.";
  if(chosen is null)return new(PackageVersion,"Save",null,null,0,observation.Balance,reserve,affordable.Length,$"Save retains all {observation.Balance} credits. No positive quoted improvement fits the balance and later-opportunity reserve of {reserve}. "+uncertainty);
  var package=chosen.Package;var permanent=package is null?observation.Options.Single(q=>q.Id==chosen.Id):null;
  var label=package is null?permanent!.Option.Name:(package.Rush is null?"":"Rush ")+package.Template.Name+(package.Refinement is null?"":" / "+package.Refinement);
  return new(PackageVersion,chosen.Action,package?.Template.Id??permanent!.Option.Id,chosen.Id,chosen.Cost,observation.Balance-chosen.Cost,reserve,affordable.Length,$"Consider {label} for {chosen.Cost} total credits, retaining {observation.Balance-chosen.Cost}; keep {reserve} for a later usable opportunity. Compares permanent work, creation, bounded refinement, Rush and Save from the same one decision. "+uncertainty,package?.SourcePackage is {} own?new(own.PackageId,own.Version):null,package?.Refinement,package?.Rush is not null);
 }
 private static IEnumerable<(string Kind,double Probability)> Categories(DevelopmentRiskControls d)=>new[]{("normal",d.Normal),("partial",d.Partial),("breakthrough",d.Breakthrough),("compromised",d.Compromised),("delay",d.Delay)};
 private static double PackageFit(RacePackageVersion package,ImmutableDictionary<string,double> car,ImmutableDictionary<string,double> demands,bool incidents)=>Fit(WeekendPreparation.CarKeys.ToImmutableDictionary(key=>key,key=>package.Strengths.GetValueOrDefault(key)+package.Compromises.GetValueOrDefault(key)),car,demands,incidents);
 private static double Fit(ImmutableDictionary<string,double> changes,ImmutableDictionary<string,double> car,ImmutableDictionary<string,double> demands,bool incidents)
 {
  double Weight(string key)=>key switch{"highSpeedAero"=>demands["highSpeedCornering"],"lowSpeedAero"=>demands["lowSpeedCornering"],"tyreWear"=>demands["tyreDegradation"],"tyreTemperature"=>demands["tyreThermalDemand"],"reliability"=>incidents?50:0,_=>demands[key]};
  return Math.Round(WeekendPreparation.CarKeys.Sum(key=>(Math.Clamp(car[key]+changes.GetValueOrDefault(key),1,100)-car[key])*Weight(key)/100),6);
 }
 private static bool SameVersion(RacePackageVersion a,RacePackageVersion b)=>a.PackageId==b.PackageId&&a.Version==b.Version&&a.Name==b.Name&&a.ModelVersion==b.ModelVersion&&a.Completed==b.Completed&&a.Provenance==b.Provenance&&a.Applicability==b.Applicability&&a.ApplicableCircuits.SequenceEqual(b.ApplicableCircuits)&&a.RefinementHistory.SequenceEqual(b.RefinementHistory)&&a.Strengths.OrderBy(p=>p.Key).SequenceEqual(b.Strengths.OrderBy(p=>p.Key))&&a.Compromises.OrderBy(p=>p.Key).SequenceEqual(b.Compromises.OrderBy(p=>p.Key));
}
