using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class PersonnelCandidateTests
{
 [Theory]
 [InlineData("low-drag",false,18)][InlineData("low-drag",true,24)]
 [InlineData("downforce",false,18)][InlineData("downforce",true,24)]
 [InlineData("tyre-management",false,14)][InlineData("tyre-management",true,20)]
 [InlineData("cooling-reliability",false,14)][InlineData("cooling-reliability",true,20)]
 [InlineData("mechanical-grip",false,16)][InlineData("mechanical-grip",true,22)]
 public void Paid_candidate_option_spends_its_printed_cost_once_and_completes_an_alternative_without_granting_free_ownership(string template,bool rush,int price)
 {
  var pack=ReleaseContent.PersonnelCandidate();var quote=pack.PaidPackageOptions.Single(q=>q.Template.Id==template&&(q.Rush is not null)==rush);
  var baseline=RacePackages.Migrate(WeekendPresetCatalog.Open("track-monza"));var team=baseline.Cars[0].TeamId;
  Assert.Equal("neutral-package-v1",Assert.Single(pack.Packages).PackageId);Assert.Equal(price,quote.Cost);
  var opportunity=PermanentDevelopment.Open("quoted-purchase","one","two",[team]);
  opportunity=PermanentDevelopment.Edit(opportunity,team,new(Guid.NewGuid().ToString(),opportunity.Id,opportunity.Revision,"PackageCreation",template,quote.Id,Rush:rush),quote.Personnel,risk:quote.Risk);
  ImmutableArray<CreditEntry> credits=[new("start",team,100,"starting-credit")];
  var command=new DevelopmentCommit(Guid.NewGuid().ToString(),opportunity.Id,opportunity.Revision,opportunity.Drafts[team].Revision,quote.Id);
  var paid=PermanentDevelopment.Commit(opportunity,credits,team,command,quote.Personnel);
  Assert.Equal(100-price,paid.Ledger.Sum(e=>e.Credits));Assert.Equal(2,paid.Ledger.Length);
  Assert.Equal(paid,PermanentDevelopment.Commit(paid.State,paid.Ledger,team,command,quote.Personnel));
  var state=PermanentDevelopment.Close(new(PermanentDevelopment.Version,[paid.State],new string('7',64)),opportunity.Id,["one","two","three"]);
  var following=PermanentDevelopment.Open("complete-delayed-work","two","three",[team]);
  following=PermanentDevelopment.Edit(following,team,new(Guid.NewGuid().ToString(),following.Id,following.Revision,"Save",null,null),quote.Personnel);
  following=PermanentDevelopment.Commit(following,paid.Ledger,team,new(Guid.NewGuid().ToString(),following.Id,following.Revision,following.Drafts[team].Revision,null),quote.Personnel).State;
  state=PermanentDevelopment.Close(state with{Opportunities=state.Opportunities.Add(following)},following.Id,["one","two","three"]);
  var library=RacePackages.Complete(baseline,state.PackageOutcomes);
  Assert.Equal(2,library.PackageLibrary.Length);Assert.Equal("neutral-package-v1",library.SelectedPackage!.PackageId);
  var completed=Assert.Single(state.PackageOutcomes).Package!;Assert.True(completed.Completed);
  var selected=RacePackages.Select(library,new(completed.PackageId,completed.Version));
  var prepared=WeekendPreparation.Prepare(selected,6,new());Assert.Equal(2,prepared.Cars.Length);
  Assert.All(prepared.Cars.SelectMany(c=>c.Source.Car.Ratings.Values),value=>Assert.InRange(value,1,100));
 }
}
