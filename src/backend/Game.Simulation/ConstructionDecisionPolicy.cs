using System.Collections.Immutable;
using Game.Domain;
namespace Game.Simulation;

// This observation cannot carry opponent choices, future weather/incidents, seed or extra credit.
public sealed record ConstructionPolicyObservation(ConstructionCatalogue Catalogue,int Balance,ImmutableArray<DriverPreset> Drivers);
public sealed record ConstructionRecommendation(string Version,ImmutableArray<string> OptionIds,int Cost,int RemainingCredits,int AffordableAlternatives,string Explanation);
public static class ConstructionDecisionPolicy
{
 public const string Version="own-roster-credit-preserving-v1";
 public static ConstructionRecommendation Evaluate(ConstructionPolicyObservation observation)
 {
  CarConstruction.ValidateCatalogue(observation.Catalogue);
  if(observation.Drivers.IsDefault||observation.Drivers.Length!=2||observation.Drivers.Select(d=>d.Id).Distinct().Count()!=2)throw new ArgumentException("Observe both acquired drivers.");
  IEnumerable<ImmutableArray<string>> combinations=[ImmutableArray<string>.Empty];
  foreach(var category in observation.Catalogue.Categories)
   combinations=combinations.SelectMany(choices=>category.Options.Select(option=>choices.Add(option.Id))).ToArray();
  var affordable=combinations.Select(choices=>new{Choices=choices,Cost=CarConstruction.Cost(observation.Catalogue,choices),Fit=observation.Drivers.Average(driver=>WeekendSetupTuning.Fit(driver,new(Mechanical:CarConstruction.MechanicalAxis(observation.Catalogue,choices))).Compatibility)})
   .Where(candidate=>candidate.Cost<=observation.Balance).ToArray();
  var chosen=affordable.OrderBy(candidate=>candidate.Cost).ThenByDescending(candidate=>candidate.Fit).ThenBy(candidate=>string.Join('/',candidate.Choices),StringComparer.Ordinal).FirstOrDefault()
   ??throw new ArgumentException("No complete build is affordable. Preserve the saved draft and review the actual ledger.");
  return new(Version,chosen.Choices,chosen.Cost,observation.Balance-chosen.Cost,affordable.Length,
   $"Preserve credits first: {chosen.Cost} credits buys a complete car, leaving {observation.Balance-chosen.Cost} for future flexibility. Among equally priced builds, prefer indicative compatibility across both acquired drivers, then stable option identity. This is not a fastest-car prediction. Spending more can buy different strengths at the cost of that flexibility. All {affordable.Length} affordable manual alternatives remain available; compare their circuit compromises before confirming. No future weather, incidents or opponent choices are observed.");
 }
}
