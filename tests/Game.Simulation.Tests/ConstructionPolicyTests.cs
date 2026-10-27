using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class ConstructionPolicyTests
{
 [Fact]
 public void Advice_preserves_every_affordable_alternative_and_credits_without_observing_future_or_opponents()
 {
  var catalogue=ConstructionCatalog.Open();var roster=WeekendPresetCatalog.Open("track-monza");
  var observation=new ConstructionPolicyObservation(catalogue,24,roster.Cars.Select(c=>c.Driver).ToImmutableArray());
  var plan=ConstructionDecisionPolicy.Evaluate(observation);
  Assert.Equal(18,plan.Cost);Assert.Equal(6,plan.RemainingCredits);Assert.Equal(625,plan.AffordableAlternatives);
  Assert.Contains("manual",plan.Explanation);Assert.Equal(4,plan.OptionIds.Length);
  var restored=JsonSerializer.Deserialize<ConstructionPolicyObservation>(JsonSerializer.Serialize(observation))!;
  Assert.Equal(JsonSerializer.Serialize(plan),JsonSerializer.Serialize(ConstructionDecisionPolicy.Evaluate(restored)));
  var onlyMinimum=ConstructionDecisionPolicy.Evaluate(observation with{Balance=18});
  Assert.Equal(0,onlyMinimum.RemainingCredits);Assert.True(onlyMinimum.AffordableAlternatives>1);
  Assert.Throws<ArgumentException>(()=>ConstructionDecisionPolicy.Evaluate(observation with{Balance=17}));
 }
}
