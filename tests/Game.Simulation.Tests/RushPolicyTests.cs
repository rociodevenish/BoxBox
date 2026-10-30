using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class RushPolicyTests
{
 [Fact]
 public void Neutral_Monaco_worked_example_prefers_standard_mechanical_package_from_the_full_public_menu()
 {
  var preset=WeekendPresetCatalog.Open("track-monaco");var staff=new DevelopmentPersonnel("preset-development-neutral-v1",0,0,0,0);
  var options=PermanentDevelopment.Options.Select(o=>PermanentDevelopment.Quote(o.Id,staff,risk:DevelopmentRiskControls.Default)).ToImmutableArray();
  var packages=PackageDevelopment.Templates.SelectMany(t=>{var quote=PackageDevelopment.QuoteRich(t.Id,staff);return new[]{quote,PackageDevelopment.Rush(quote)};}).ToImmutableArray();
  var observation=new DevelopmentPolicyObservation(106,0,options,PermanentDevelopment.Options.ToImmutableDictionary(o=>o.Id,_=>70d),PermanentDevelopment.Options.ToImmutableDictionary(o=>o.Id,_=>6d),packages,[RacePackages.Baseline],preset.Circuit.Demands,preset.Cars[0].Car.Ratings,[preset.Circuit.Demands]);
  var choice=DevelopmentDecisionPolicy.Evaluate(observation);
  Assert.Equal("PackageCreation",choice.ActionKind);Assert.Equal("mechanical-grip",choice.OptionId);Assert.False(choice.Rush);Assert.Equal(16,choice.Cost);Assert.Equal(90,choice.RemainingCredits);
 }
 [Fact]
 public void Staff_effects_are_bounded_once_and_zero_delay_scenarios_disclose_no_arrival_benefit()
 {
  var staff=new DevelopmentPersonnel("maximum",100,100,100,100);
  var standard=PackageDevelopment.QuoteRich("downforce",staff);var rush=PackageDevelopment.Rush(standard);
  Assert.Equal(16,standard.Cost);Assert.Equal(22,rush.Cost);Assert.Equal(6,rush.Rush!.ExtraCost);
  Assert.Equal(.51,rush.Distribution!.Normal,8);Assert.Equal(.2,rush.Distribution.Compromised,8);Assert.Equal(.04,rush.Distribution.Delay,8);
  var guaranteedScenario=PackageDevelopment.Rush(PackageDevelopment.QuoteRich("downforce",staff,DevelopmentRiskControls.Only("normal")));
  Assert.Contains("cannot improve its arrival chance",guaranteedScenario.Timing);
  Assert.Throws<ArgumentException>(()=>PackageDevelopment.Rush(PackageDevelopment.Quote("downforce",staff)));
 }
 [Fact]
 public void Legitimate_project_policy_compares_library_alternatives_and_preserves_old_observations()
 {
  var preset=WeekendPresetCatalog.Open("track-monaco");var staff=new DevelopmentPersonnel("frozen",0,0,0,0);
  var risk=DevelopmentRiskControls.Only("normal");var creation=PackageDevelopment.QuoteRich("downforce",staff,risk);
  var observation=new DevelopmentPolicyObservation(40,0,PermanentDevelopment.Options.Select(o=>PermanentDevelopment.Quote(o.Id,staff)).ToImmutableArray(),PermanentDevelopment.Options.ToImmutableDictionary(o=>o.Id,_=>70d),PermanentDevelopment.Options.ToImmutableDictionary(o=>o.Id,_=>0d),PackageOptions:[creation,PackageDevelopment.Rush(creation)],Library:[RacePackages.Baseline],CircuitDemands:preset.Circuit.Demands,BaseRatings:preset.Cars[0].Car.Ratings);
  var chosen=DevelopmentDecisionPolicy.Evaluate(observation);
  Assert.Equal("PackageCreation",chosen.ActionKind);Assert.Equal(creation.Id,chosen.QuoteId);Assert.False(chosen.Rush);Assert.Equal(22,chosen.RemainingCredits);
  var existing=PackageDevelopment.PreviewOutcome(creation,"normal") with{PackageId="owned",Provenance="paid-existing-version"};
  Assert.Equal("Save",DevelopmentDecisionPolicy.Evaluate(observation with{Library=[RacePackages.Baseline,existing]}).ActionKind);
  var refinement=PackageDevelopment.QuoteRefinement(existing,"strengthen",staff,risk);
  var refine=DevelopmentDecisionPolicy.Evaluate(observation with{PackageOptions=[creation,refinement],Library=[RacePackages.Baseline,existing]});
  Assert.Equal("PackageRefinement",refine.ActionKind);Assert.Equal(new RacePackageSelection("owned",1),refine.Package);Assert.Equal("strengthen",refine.Refinement);
  Assert.Equal("Save",DevelopmentDecisionPolicy.Evaluate(observation with{Balance=0}).ActionKind);
  var old=observation with{PackageOptions=default,Library=default,CircuitDemands=null,BaseRatings=null};
  Assert.Equal(DevelopmentDecisionPolicy.Version,DevelopmentDecisionPolicy.Evaluate(old).Version);
 }
 [Fact]
 public void Rush_quotes_one_total_debit_and_better_intended_event_chance_with_more_compromise_using_the_same_saved_draw()
 {
  var staff=new DevelopmentPersonnel("frozen",0,0,0,0);var standard=PackageDevelopment.QuoteRich("downforce",staff);
  var rush=PackageDevelopment.Rush(standard);
  Assert.Equal(18,rush.Rush!.StandardCost);Assert.Equal(6,rush.Rush.ExtraCost);Assert.Equal(24,rush.Cost);
  Assert.Equal(.04,rush.Distribution!.Delay,8);Assert.Equal(.25,rush.Distribution.Compromised,8);
  Assert.Equal(standard.OutcomeVersion,rush.OutcomeVersion);
  Assert.True(rush.Distribution.Delay>0);Assert.Contains("not guaranteed",rush.Timing);
  var op=PermanentDevelopment.Open("rush-op","one","two",["team"]);
  op=PermanentDevelopment.Edit(op,"team",new(Guid.NewGuid().ToString(),op.Id,1,"PackageCreation","downforce",rush.Id,Rush:true),staff,risk:DevelopmentRiskControls.Default);
  var commit=new DevelopmentCommit(Guid.NewGuid().ToString(),op.Id,op.Revision,1,rush.Id);
  Assert.Throws<InvalidOperationException>(()=>PermanentDevelopment.Commit(op,[new("start","team",23,"starting-credit")],"team",commit,staff));
  var paid=PermanentDevelopment.Commit(op,[new("start","team",24,"starting-credit")],"team",commit,staff);
  Assert.Equal(0,paid.Ledger.Sum(e=>e.Credits));Assert.Single(paid.Ledger,e=>e.Reason=="race-package-development");
  Assert.Equal(paid,PermanentDevelopment.Commit(paid.State,paid.Ledger,"team",commit,staff));
  var state=new DevelopmentState(PermanentDevelopment.Version,[paid.State],new string('6',64));
  var restored=System.Text.Json.JsonSerializer.Deserialize<DevelopmentState>(System.Text.Json.JsonSerializer.Serialize(state))!;
  Assert.Equal(System.Text.Json.JsonSerializer.Serialize(PermanentDevelopment.Close(state,op.Id,["one","two"])),System.Text.Json.JsonSerializer.Serialize(PermanentDevelopment.Close(restored,op.Id,["one","two"])));
 }
}
