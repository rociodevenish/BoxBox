using System.Collections.Immutable;
using Game.Domain;
namespace Game.Simulation;

public sealed record ConstructionTrackPreview(string TrackId,string Name,string Slug,string SourceVersion,ImmutableArray<string> Strengths,ImmutableArray<string> Compromises,string DemandSummary);
public sealed record ConstructionPreviewResult(string Version,int Cost,int Balance,int RemainingCredits,bool Affordable,ImmutableDictionary<string,double> Ratings,ImmutableDictionary<string,double> Groups,ImmutableArray<DriverSetupFit> DriverFits,ImmutableArray<ConstructionTrackPreview> Tracks,string Explanation);
public static class ConstructionPreview
{
 public const string Version="construction-preview-v1";
 public static ConstructionPreviewResult Preview(ConstructionCatalogue catalogue,ImmutableArray<string> choices,WeekendPreset roster,int balance)
 {
  var ratings=CarConstruction.Ratings(catalogue,choices);
  var cost=CarConstruction.Cost(catalogue,choices);
  // Locked source headlineDerivation weights summarize detailed points; they never
  // become simulation inputs or convert card stars back into performance.
  var groups=new Dictionary<string,double>{
   {"Speed",ratings["topSpeed"]*.5+ratings["accelerationDeployment"]*.5},
   {"Cornering",ratings["highSpeedAero"]*.55+ratings["lowSpeedAero"]*.45},
   {"Mechanical Grip",ratings["mechanicalGrip"]*.4+ratings["braking"]*.3+ratings["traction"]*.3},
   {"Tyres",ratings["tyreWear"]*.55+ratings["tyreTemperature"]*.45},
   {"Reliability",ratings["reliability"]}}.ToImmutableDictionary();
  // Supported directional compatibility is one existing mechanical axis. Six authored
  // handling labels do not create six new physics inputs. No staff pace effects.
  var mechanical=CarConstruction.MechanicalAxis(catalogue,choices);
  if(roster.Cars.Length!=2)throw new ArgumentException("Preview requires both acquired drivers.");
  var fits=roster.Cars.Select(c=>WeekendSetupTuning.Fit(c.Driver,new(Mechanical:mechanical)) with{Description="Indicative Base Car compatibility using the supported responsive/stable preference axis. Adaptability moderates mismatch; not a lap-time prediction."}).ToImmutableArray();
  var tracks=new[]{"track-monza","track-monaco"}.Select(id=>
  {
   var circuit=WeekendPresetCatalog.Open(id).Circuit;
   var gains=ImmutableArray.CreateBuilder<string>();var weaknesses=ImmutableArray.CreateBuilder<string>();
   var d=circuit.Demands;
   void Compare(string label,double delta){if(delta>.05)gains.Add(label);if(delta< -.05)weaknesses.Add(label);}
   double Delta(string key,string demand)=> (ratings[key]-catalogue.Baseline[key])*d[demand]/100;
   Compare("Straight performance",(Delta("topSpeed","topSpeed")+Delta("accelerationDeployment","accelerationDeployment"))/2);
   Compare("Cornering support",(Delta("highSpeedAero","highSpeedCornering")+Delta("lowSpeedAero","lowSpeedCornering")+Delta("mechanicalGrip","mechanicalGrip")+Delta("braking","braking")+Delta("traction","traction"))/5);
   Compare("Stint preservation",Delta("tyreWear","tyreDegradation"));
   Compare("Thermal control",Delta("tyreTemperature","tyreThermalDemand"));
   Compare("Reliability margin",ratings["reliability"]-catalogue.Baseline["reliability"]);
   var straight=(d["topSpeed"]+d["accelerationDeployment"])/2;
   var corners=(d["highSpeedCornering"]+d["lowSpeedCornering"]+d["mechanicalGrip"]+d["braking"]+d["traction"])/5;
   return new ConstructionTrackPreview(circuit.Id,circuit.Name,circuit.Slug,circuit.Version,gains.ToImmutable(),weaknesses.ToImmutable(),straight>corners?"Greater relative straight demand":"Greater relative cornering demand");
  }).ToImmutableArray();
  return new(Version,cost,balance,balance-cost,cost<=balance,ratings,groups,fits,tracks,"Free editable draft; 1 budgetUnit = 1 credit. Remaining credits fund future flexibility. First two stacked modifier points apply fully, further points at half strength, capped at six; final detailed values stay 1–100. Indicative comparisons against the supplied baseline (Reliability 84), not guaranteed pace. No future weather or incidents are observed. Base Car, permanent development, Race Package and Weekend Setup are separate layers. Reliability affects incident resilience only when the existing incident policy is enabled; otherwise it is inactive.");
 }
}
