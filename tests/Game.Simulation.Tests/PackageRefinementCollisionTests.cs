using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class PackageRefinementCollisionTests
{
 [Fact]
 public void Paid_history_cannot_offer_new_broadening_when_the_weakest_and_strongest_capability_collide()
 {
  var staff=new DevelopmentPersonnel("fixture",0,0,0,0);
  var calendar=new[]{"one","two","three","four","five","six"};
  var state=new DevelopmentState(PermanentDevelopment.Version,[],new string('5',64));
  ImmutableArray<CreditEntry> ledger=[new("start","team-1",100,"starting-credit")];
  RacePackageVersion Pay(PackageDevelopmentQuote quote,int index)
  {
   var op=PermanentDevelopment.Open("paid-"+index,calendar[index],calendar[index+1],["team-1"]);
   var library=state.PackageOutcomes.IsDefault?[]:state.PackageOutcomes.Select(o=>o.Package!).ToArray();
   op=PermanentDevelopment.Edit(op,"team-1",new(Guid.NewGuid().ToString(),op.Id,op.Revision,quote.SourcePackage is null?"PackageCreation":"PackageRefinement",quote.Template.Id,quote.Id,Package:quote.SourcePackage is {} p?new(p.PackageId,p.Version):null,Refinement:quote.Refinement),staff,risk:quote.Risk,library:library);
   var paid=PermanentDevelopment.Commit(op,ledger,"team-1",new(Guid.NewGuid().ToString(),op.Id,op.Revision,1,quote.Id),staff);ledger=paid.Ledger;
   state=PermanentDevelopment.Close(state with{Opportunities=state.Opportunities.Add(paid.State)},op.Id,calendar);
   return state.PackageOutcomes.Last().Package!;
  }
  var v1=Pay(PackageDevelopment.QuoteRich("cooling-reliability",staff,DevelopmentRiskControls.Only("partial")),0);
  var v2=Pay(PackageDevelopment.QuoteRefinement(v1,"broaden-usefulness",staff,DevelopmentRiskControls.Only("compromised")),1);
  var v3=Pay(PackageDevelopment.QuoteRefinement(v2,"reduce-compromise",staff,DevelopmentRiskControls.Only("normal")),2);
  var v4=Pay(PackageDevelopment.QuoteRefinement(v3,"reduce-compromise",staff,DevelopmentRiskControls.Only("normal")),3);
  Assert.Equal(1.75,v4.Strengths["reliability"]);Assert.Equal(-1,v4.Compromises["reliability"]);Assert.Equal(0,v4.Compromises["accelerationDeployment"]);
  Assert.Throws<ArgumentException>(()=>PackageDevelopment.QuoteRefinement(v4,"broaden-usefulness",staff,DevelopmentRiskControls.Only("normal")));
  Assert.Equal(56,ledger.Sum(e=>e.Credits));
  Assert.Equal(4,ledger.Count(e=>e.Reason=="race-package-development"));
 }
 [Fact]
 public void Already_paid_overlapping_broadening_retains_its_original_quote_and_concrete_outcome_on_replay()
 {
  var saved=JsonSerializer.Deserialize<DevelopmentState>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","overlapping-broadening-v2.json")))!;
  var final=saved.Opportunities.Last();var decision=Assert.Single(final.Decisions);
  var quote=decision.PackageQuote!;PackageDevelopment.ValidateQuote(quote);
  Assert.Equal(quote.Id,PackageDevelopment.Requote(quote,quote.Personnel).Id);
  // Re-resolve the final accepted project from its original saved contract, not today's draft eligibility.
  var pending=saved with{Opportunities=saved.Opportunities.SetItem(saved.Opportunities.Length-1,final with{Closed=false}),PackageOutcomes=saved.PackageOutcomes.RemoveAt(saved.PackageOutcomes.Length-1)};
  var replayed=PermanentDevelopment.Close(pending,final.Id,["one","two","three","four","five","six"]);
  Assert.Equal(JsonSerializer.Serialize(saved.PackageOutcomes),JsonSerializer.Serialize(replayed.PackageOutcomes));
  Assert.Equal(1.25,replayed.PackageOutcomes.Last().Package!.Strengths["reliability"]);
 }
}

