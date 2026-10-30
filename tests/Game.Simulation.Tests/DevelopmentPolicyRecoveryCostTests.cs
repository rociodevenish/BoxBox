using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;
using Xunit;
using Xunit.Abstractions;
namespace Game.Simulation.Tests;
public sealed class DevelopmentPolicyRecoveryCostTests(ITestOutputHelper output)
{
 private static DevelopmentPolicyObservation Observation()
 {
  var preset=RacePackages.Migrate(WeekendPresetCatalog.Open("track-monza"));
  var staff=new DevelopmentPersonnel("recovery-cost-public-operations-v1/"+Guid.NewGuid(),0,0,0,0);
  var purchased=PackageDevelopment.PreviewOutcome(PackageDevelopment.QuoteRich("downforce",staff),"normal");
  var quotes=PackageDevelopment.Templates.SelectMany(t=>new[]{PackageDevelopment.QuoteRich(t.Id,staff),PackageDevelopment.Rush(PackageDevelopment.QuoteRich(t.Id,staff))}).ToImmutableArray();
  var refinements=new[]{"strengthen","reduce-compromise","broaden-usefulness"}.SelectMany(kind=>new[]{PackageDevelopment.QuoteRefinement(purchased,kind,staff),PackageDevelopment.Rush(PackageDevelopment.QuoteRefinement(purchased,kind,staff))});
  return new(100,22,DevelopmentPersonnelMapping.PresetQuotes(),PermanentDevelopment.Options.ToImmutableDictionary(o=>o.Id,_=>70d),PermanentDevelopment.Options.ToImmutableDictionary(o=>o.Id,_=>6d),quotes.AddRange(refinements),[RacePackages.Baseline,purchased],preset.Circuit.Demands,preset.Cars[0].Car.Ratings,Enumerable.Repeat(preset.Circuit.Demands,23).ToImmutableArray());
 }
 [Fact]
 public void Replaying_identical_content_after_deserialization_avoids_reallocating_all_policy_alternatives()
 {
  var observation=Observation();var timer=System.Diagnostics.Stopwatch.StartNew();var before=GC.GetAllocatedBytesForCurrentThread();
  var expected=DevelopmentDecisionPolicy.Evaluate(observation);var cold=GC.GetAllocatedBytesForCurrentThread()-before;var coldMs=timer.Elapsed.TotalMilliseconds;
  var restored=JsonSerializer.Deserialize<DevelopmentPolicyObservation>(JsonSerializer.Serialize(observation))!;
  // Warm the independently deserialized value as dictionary enumeration order need not be identical.
  Assert.Equal(expected,DevelopmentDecisionPolicy.Evaluate(restored));
  timer.Restart();before=GC.GetAllocatedBytesForCurrentThread();
  for(var n=0;n<10;n++)Assert.Equal(expected,DevelopmentDecisionPolicy.Evaluate(restored));
  var repeated=GC.GetAllocatedBytesForCurrentThread()-before;var warmMs=timer.Elapsed.TotalMilliseconds;
  output.WriteLine("Cold full policy allocated {0} bytes / {2:F2}ms; ten identical recovered evaluations allocated {1} bytes / {3:F2}ms",cold,repeated,coldMs,warmMs);
  Assert.True(repeated<cold*3,"Repeated saved advice should not rebuild every quoted outcome and circuit alternative.");
 }
 [Fact]
 public void Content_changes_with_identical_quote_and_package_identities_never_reuse_old_advice_or_errors()
 {
  var o=Observation();var original=DevelopmentDecisionPolicy.Evaluate(o);
  Assert.Equal("Save",DevelopmentDecisionPolicy.Evaluate(o with{Balance=0}).ActionKind);
  Assert.Equal(original,DevelopmentDecisionPolicy.Evaluate(JsonSerializer.Deserialize<DevelopmentPolicyObservation>(JsonSerializer.Serialize(o))!));
  var changed=new[]{
   o with{RemainingOpportunities=1},
   o with{Options=o.Options.SetItem(0,o.Options[0] with{Cost=999})},
   o with{CircuitRelevance=o.CircuitRelevance.SetItem(o.Options[0].Option.Id,double.NaN)},
   o with{GainCapacity=o.GainCapacity.SetItem(o.Options[0].Option.Id,-1)},
   o with{PackageOptions=o.PackageOptions.SetItem(0,o.PackageOptions[0] with{Cost=999})},
   o with{Library=o.Library.SetItem(1,o.Library[1] with{Completed=false})},
   o with{BaseRatings=o.BaseRatings!.SetItem("topSpeed",double.PositiveInfinity)},
   o with{UpcomingCircuits=o.UpcomingCircuits.SetItem(22,o.CircuitDemands!.SetItem("topSpeed",101))}
  };
  foreach(var invalid in changed)Assert.Throws<ArgumentException>(()=>DevelopmentDecisionPolicy.Evaluate(invalid));
  Assert.Equal(original,DevelopmentDecisionPolicy.Evaluate(o));
 }
 [Fact]
 public void Nonordinal_dictionary_semantics_cannot_share_cached_advice_with_identical_serialized_entries()
 {
  var unique=Guid.NewGuid().ToString();
  var observation=new DevelopmentPolicyObservation(100,0,DevelopmentPersonnelMapping.PresetQuotes(),PermanentDevelopment.Options.ToImmutableDictionary(o=>o.Id.ToUpperInvariant(),_=>70d,StringComparer.OrdinalIgnoreCase),PermanentDevelopment.Options.ToImmutableDictionary(o=>o.Id,_=>6d));
  // Distinct valid quoted personnel isolates this observation from other policy tests.
  var staff=new DevelopmentPersonnel(unique,0,0,0,0);
  observation=observation with{Options=PermanentDevelopment.Options.Select(o=>PermanentDevelopment.Quote(o.Id,staff)).ToImmutableArray()};
  Assert.Equal("PermanentRefinement",DevelopmentDecisionPolicy.Evaluate(observation).ActionKind);
  var ordinal=observation with{CircuitRelevance=observation.CircuitRelevance.WithComparers(StringComparer.Ordinal)};
  Assert.Equal(JsonSerializer.Serialize(observation),JsonSerializer.Serialize(ordinal));
  Assert.Throws<ArgumentException>(()=>DevelopmentDecisionPolicy.Evaluate(ordinal));
 }}
