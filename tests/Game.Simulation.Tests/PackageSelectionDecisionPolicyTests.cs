using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class PackageSelectionDecisionPolicyTests
{
 private static PackageSelectionObservation Observation()=>new("track-monza",WeekendPresetCatalog.Open("track-monza").Circuit.Demands,WeekendPresetCatalog.Open("track-monza").Cars[0].Car.Ratings,[RacePackages.Baseline]);
 private static RacePackageVersion Speed(string id,int version=1)=>RacePackages.Baseline with{PackageId=id,Version=version,Name=id,Strengths=ImmutableDictionary<string,double>.Empty.Add("topSpeed",3)};
 [Fact]
 public void Complete_library_tradeoffs_and_saturated_ratings_can_favour_neutral_over_a_headline_strength()
 {
  var input=Observation();
  var trap=Speed("trap") with{Strengths=ImmutableDictionary<string,double>.Empty.Add("topSpeed",6),Compromises=ImmutableDictionary<string,double>.Empty.Add("mechanicalGrip",-6)};
  var choice=PackageSelectionDecisionPolicy.Evaluate(input with{SourceRatings=input.SourceRatings.SetItem("topSpeed",100),Library=[trap,RacePackages.Baseline]});
  Assert.Equal(new RacePackageSelection("neutral-package-v1",1),choice.Selection);
  Assert.Contains("not a lap-time prediction",choice.Explanation);
 }
 [Fact]
 public void Pending_and_inapplicable_versions_never_displace_completed_versions_and_ties_are_stable()
 {
  var input=Observation() with{Library=[Speed("pending") with{Completed=false},Speed("wrong-circuit") with{ApplicableCircuits=["track-monaco"]},Speed("b"),Speed("a"),Speed("a",2),RacePackages.Baseline]};
  var choice=PackageSelectionDecisionPolicy.Evaluate(input);
  Assert.Equal(new RacePackageSelection("a",2),choice.Selection);
  Assert.Equal(4,choice.Comparisons.Length);
  Assert.Equal(choice.Selection,PackageSelectionDecisionPolicy.Evaluate(input with{Library=input.Library.Reverse().ToImmutableArray()}).Selection);
 }
 [Fact]
 public void Reliability_investment_is_only_recommended_when_the_failure_consumer_is_active()
 {
  var cooling=RacePackages.Baseline with{PackageId="cooling",Name="Cooling",Strengths=ImmutableDictionary<string,double>.Empty.Add("reliability",6)};
  var input=Observation() with{Library=[cooling,RacePackages.Baseline]};
  Assert.Equal("neutral-package-v1",PackageSelectionDecisionPolicy.Evaluate(input).Selection.PackageId);
  Assert.Equal("cooling",PackageSelectionDecisionPolicy.Evaluate(input with{ReliabilityActive=true}).Selection.PackageId);
 }
 [Fact]
 public void Missing_neutral_fallback_is_rejected_instead_of_silently_using_zero_modifiers()
 {
  Assert.Throws<ArgumentException>(()=>PackageSelectionDecisionPolicy.Evaluate(Observation() with{Library=[Speed("only-specialist")]}));
 }
}
