using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class RichDevelopmentTests
{
 [Fact]
 public void Default_rich_draws_are_keyed_to_project_identity_and_independent_of_saved_decision_enumeration()
 {
  var teams=Enumerable.Range(1,40).Select(i=>"synthetic-team-"+i).ToArray();var staff=new DevelopmentPersonnel("frozen",0,0,0,0);
  var quote=PackageDevelopment.QuoteRich("low-drag",staff);
  var op=PermanentDevelopment.Open("stable-projects","one","two",teams);
  var ledger=teams.Select(team=>new CreditEntry("start",team,100,"starting-credit")).ToImmutableArray();
  foreach(var team in teams)
  {
   op=PermanentDevelopment.Edit(op,team,new(Guid.NewGuid().ToString(),op.Id,op.Revision,"PackageCreation","low-drag",quote.Id),staff,risk:DevelopmentRiskControls.Default);
   var paid=PermanentDevelopment.Commit(op,ledger,team,new(Guid.NewGuid().ToString(),op.Id,op.Revision,1,quote.Id),staff);op=paid.State;ledger=paid.Ledger;
  }
  var state=new DevelopmentState(PermanentDevelopment.Version,[op],new string('9',64));
  var first=PermanentDevelopment.Close(state,op.Id,["one","two"]);
  var restored=System.Text.Json.JsonSerializer.Deserialize<DevelopmentState>(System.Text.Json.JsonSerializer.Serialize(state with{Opportunities=[op with{Decisions=op.Decisions.Reverse().ToImmutableArray()}]}))!;
  var second=PermanentDevelopment.Close(restored,op.Id,["one","two"]);
  Assert.Equal(System.Text.Json.JsonSerializer.Serialize(first.PackageOutcomes),System.Text.Json.JsonSerializer.Serialize(second.PackageOutcomes));
  Assert.InRange(first.PackageOutcomes.Select(o=>o.Kind).Distinct().Count(),3,5);
  Assert.All(first.PackageOutcomes.Where(o=>o.Kind=="delay"),o=>{Assert.Null(o.CompletionEventId);Assert.False(o.Package!.Completed);});
  var legacy=PackageDevelopment.Quote("low-drag",staff);
  Assert.Throws<ArgumentException>(()=>PackageDevelopment.ValidateQuote(legacy with{Distribution=DevelopmentRiskControls.Only("breakthrough")}));
 }
 [Fact]
 public void Synthetic_repeated_circuit_opportunities_purchase_then_delay_and_refine_without_colliding_versions_or_repayment()
 {
  // Explicit multi-opportunity domain scenario, not the supported two-event production calendar.
  var calendar=new[]{"one","two","three","four"};var staff=new DevelopmentPersonnel("frozen",0,0,0,0);
  var state=new DevelopmentState(PermanentDevelopment.Version,[],new string('7',64));ImmutableArray<CreditEntry> ledger=[new("start","team-1",100,"starting-credit")];
  void Commit(string id,string from,string to,PackageDevelopmentQuote quote)
  {
   var library=(state.PackageOutcomes.IsDefault?[]:state.PackageOutcomes).Where(o=>o.Package is not null).Select(o=>o.Package!).ToArray();
   var op=PermanentDevelopment.Open(id,from,to,["team-1"]);
   op=PermanentDevelopment.Edit(op,"team-1",new(Guid.NewGuid().ToString(),id,1,quote.SourcePackage is null?"PackageCreation":"PackageRefinement",quote.Template.Id,quote.Id,Package:quote.SourcePackage is {} source?new(source.PackageId,source.Version):null,Refinement:quote.Refinement),staff,risk:quote.Risk,library:library);
   var paid=PermanentDevelopment.Commit(op,ledger,"team-1",new(Guid.NewGuid().ToString(),id,op.Revision,1,quote.Id),staff);ledger=paid.Ledger;
   var pending=state with{Opportunities=state.Opportunities.Add(paid.State)};
   var restored=System.Text.Json.JsonSerializer.Deserialize<DevelopmentState>(System.Text.Json.JsonSerializer.Serialize(pending))!;
   state=PermanentDevelopment.Close(pending,id,calendar);
   Assert.Equal(System.Text.Json.JsonSerializer.Serialize(state),System.Text.Json.JsonSerializer.Serialize(PermanentDevelopment.Close(restored,id,calendar)));
  }
  Commit("purchase","one","two",PackageDevelopment.QuoteRich("downforce",staff,DevelopmentRiskControls.Only("normal")));
  var version1=Assert.Single(state.PackageOutcomes).Package!;
  var first=RacePackages.Complete(WeekendPresetCatalog.Open("track-monza"),state.PackageOutcomes);
  var frozen=WeekendPreparation.Prepare(RacePackages.Select(first,new(version1.PackageId,1)),6,new());
  Commit("delayed-refinement","two","three",PackageDevelopment.QuoteRefinement(version1,"strengthen",staff,DevelopmentRiskControls.Only("delay")));
  Assert.False(state.PackageOutcomes[1].Package!.Completed);Assert.Equal(2,state.PackageOutcomes[1].Package!.Version);
  Commit("next-refinement","three","four",PackageDevelopment.QuoteRefinement(version1,"reduce-compromise",staff,DevelopmentRiskControls.Only("normal"),3));
  Assert.Equal(62,ledger.Sum(e=>e.Credits));Assert.Equal(3,ledger.Count(e=>e.Reason=="race-package-development"));
  Assert.Equal(new[]{1,2,3},state.PackageOutcomes.Select(o=>o.Package!.Version));Assert.All(state.PackageOutcomes,o=>Assert.True(o.Package!.Completed));
  Assert.Equal(4,RacePackages.Complete(WeekendPresetCatalog.Open("track-monaco"),state.PackageOutcomes).PackageLibrary.Length);
  Assert.Equal(1,frozen.Source.SelectedPackage!.Version);Assert.Equal(86,frozen.Cars[0].Source.Car.Ratings["highSpeedAero"]);
 }
 [Theory]
 [InlineData("strengthen")]
 [InlineData("reduce-compromise")]
 [InlineData("broaden-usefulness")]
 public void Refining_exact_completed_version_creates_a_bounded_new_version_and_preserves_frozen_sources(string refinement)
 {
  var staff=new DevelopmentPersonnel("frozen",0,0,0,0);var template=PackageDevelopment.Templates.Single(t=>t.Id=="downforce");
  var source=new RacePackageVersion("purchased",1,template.Name,"race-package-v1",template.Strengths,template.Compromises,[],template.Applicability,"public-domain-synthetic-purchase",[]);
  var original=RacePackages.Migrate(WeekendPresetCatalog.Open("track-monza"));
  var library=original with{PackageLibrary=original.PackageLibrary.Add(source)};
  var frozen=WeekendPreparation.Prepare(RacePackages.Select(library,new(source.PackageId,1)),6,new());
  var quote=PackageDevelopment.QuoteRefinement(source,refinement,staff,DevelopmentRiskControls.Only("normal"));
  var op=PermanentDevelopment.Open("refine","two","three",["team-1"]);
  op=PermanentDevelopment.Edit(op,"team-1",new(Guid.NewGuid().ToString(),op.Id,1,"PackageRefinement","downforce",quote.Id,Package:new(source.PackageId,1),Refinement:refinement),staff,risk:DevelopmentRiskControls.Only("normal"),library:library.PackageLibrary);
  var paid=PermanentDevelopment.Commit(op,[new("start","team-1",40,"starting-credit")],"team-1",new(Guid.NewGuid().ToString(),op.Id,op.Revision,1,quote.Id),staff);
  var state=PermanentDevelopment.Close(new(PermanentDevelopment.Version,[paid.State],new string('4',64)),op.Id,["one","two","three"]);
  var result=Assert.Single(state.PackageOutcomes).Package!;
  Assert.Equal(source.PackageId,result.PackageId);Assert.Equal(2,result.Version);Assert.Single(result.RefinementHistory);
  if(refinement=="strengthen")Assert.Equal(5,result.Strengths["highSpeedAero"]);
  if(refinement=="reduce-compromise")Assert.Equal(-2,result.Compromises["topSpeed"]);
  if(refinement=="broaden-usefulness"){Assert.Equal(1,result.Strengths["topSpeed"]);Assert.Equal(3.5,result.Strengths["highSpeedAero"]);}
  Assert.Equal(1,frozen.Source.SelectedPackage!.Version);Assert.Equal(86,frozen.Cars[0].Source.Car.Ratings["highSpeedAero"]);
  Assert.Throws<ArgumentException>(()=>PackageDevelopment.QuoteRefinement(RacePackages.Baseline,refinement,staff));
  Assert.Throws<ArgumentException>(()=>PackageDevelopment.QuoteRefinement(source with{Completed=false},refinement,staff));
 }
 [Theory]
 [InlineData("normal",3)]
 [InlineData("partial",1)]
 [InlineData("breakthrough",4)]
 [InlineData("compromised",1)]
 [InlineData("delay",3)]
 public void New_permanent_contracts_support_rich_outcomes_while_old_quotes_remain_three_category(string kind,int gain)
 {
  var staff=new DevelopmentPersonnel("frozen",0,0,0,0);var risk=DevelopmentRiskControls.Only(kind);
  var controls=new DevelopmentOutcomeControls("normal-controls-v1",0,0,0);
  var quote=PermanentDevelopment.Quote("speed",staff,controls,risk);
  var op=PermanentDevelopment.Open("op","one","two",["team-1"]);
  op=PermanentDevelopment.Edit(op,"team-1",new(Guid.NewGuid().ToString(),op.Id,1,"PermanentRefinement","speed",quote.Id),staff,controls,risk);
  var paid=PermanentDevelopment.Commit(op,[new("start","team-1",100,"starting-credit")],"team-1",new(Guid.NewGuid().ToString(),op.Id,op.Revision,1,quote.Id),staff);
  var completed=PermanentDevelopment.Close(new(PermanentDevelopment.Version,[paid.State],new string('4',64)),op.Id,["one","two","three"]);
  var outcome=Assert.Single(completed.Outcomes);Assert.Equal(kind,outcome.Kind);Assert.Equal(gain,outcome.Gain);
  if(kind=="compromised")Assert.Equal(-1,Assert.Single(completed.Modifiers).Changes["reliability"]);
  if(kind=="delay")Assert.Empty(completed.Modifiers);
  Assert.Equal(PermanentDevelopment.OutcomeVersion,PermanentDevelopment.Quote("speed",staff,controls).OutcomeVersion);
 }
 [Theory]
 [InlineData("normal",4,-2,"two")]
 [InlineData("partial",2,-2,"two")]
 [InlineData("breakthrough",5,-2,"two")]
 [InlineData("compromised",3,-2,"two")]
 [InlineData("delay",4,-2,"three")]
 public void Frozen_package_policy_resolves_all_categories_and_delay_completes_without_repayment(string kind,double strength,double compromise,string due)
 {
  var staff=new DevelopmentPersonnel("frozen",0,0,0,0);var controls=DevelopmentRiskControls.Only(kind);
  var quote=PackageDevelopment.QuoteRich("low-drag",staff,controls);
  var op=PermanentDevelopment.Open("op","one","two",["team-1"]);
  op=PermanentDevelopment.Edit(op,"team-1",new(Guid.NewGuid().ToString(),op.Id,1,"PackageCreation","low-drag",quote.Id),staff,risk:controls);
  var paid=PermanentDevelopment.Commit(op,[new("start","team-1",100,"starting-credit")],"team-1",new(Guid.NewGuid().ToString(),op.Id,op.Revision,1,quote.Id),staff);
  var state=PermanentDevelopment.Close(new(PermanentDevelopment.Version,[paid.State],new string('4',64)),op.Id,["one","two","three"]);
  var outcome=Assert.Single(state.PackageOutcomes);Assert.Equal(kind,outcome.Kind);Assert.Equal(due,outcome.CompletionEventId);
  if(kind=="delay")
  {
   Assert.False(outcome.Package!.Completed);
   var next=PermanentDevelopment.Open("next","two","three",["team-1"]);
   next=PermanentDevelopment.Edit(next,"team-1",new(Guid.NewGuid().ToString(),next.Id,1,"Save",null,null),staff);
   var saved=PermanentDevelopment.Commit(next,paid.Ledger,"team-1",new(Guid.NewGuid().ToString(),next.Id,next.Revision,1,null),staff);
   state=PermanentDevelopment.Close(state with{Opportunities=state.Opportunities.Add(saved.State)},next.Id,["one","two","three"]);
   outcome=Assert.Single(state.PackageOutcomes);Assert.True(outcome.Package!.Completed);
   Assert.Equal(82,saved.Ledger.Sum(e=>e.Credits));
  }
  Assert.Equal(strength,outcome.Package!.Strengths["topSpeed"]);Assert.Equal(compromise,outcome.Package.Compromises["highSpeedAero"]);
  if(kind=="compromised")Assert.Equal(-2,outcome.Package.Compromises["reliability"]);
  var library=RacePackages.Complete(RacePackages.Migrate(WeekendPresetCatalog.Open("track-monza")),state.PackageOutcomes);
  _=WeekendPreparation.Prepare(RacePackages.Select(library,new(outcome.Package.PackageId,outcome.Package.Version)),6);
 }
}
