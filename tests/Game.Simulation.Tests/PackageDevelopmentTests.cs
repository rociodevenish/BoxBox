using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class PackageDevelopmentTests
{
 [Theory]
 [InlineData("low-drag",18,"topSpeed",4,"highSpeedAero",-2)]
 [InlineData("downforce",18,"highSpeedAero",4,"topSpeed",-3)]
 [InlineData("tyre-management",14,"tyreWear",4,"traction",-2)]
 [InlineData("cooling-reliability",14,"reliability",4,"accelerationDeployment",-2)]
 [InlineData("mechanical-grip",16,"mechanicalGrip",4,"topSpeed",-2)]
 public void All_five_templates_disclose_costs_compromises_and_complete_immutable_versions(string template,int cost,string strength,double gain,string compromise,double loss)
 {
  var staff=new DevelopmentPersonnel("frozen-roster",0,0,0,0);var quote=PackageDevelopment.Quote(template,staff);
  Assert.Equal(cost,quote.Cost);Assert.Equal(gain,quote.Template.Strengths[strength]);Assert.Equal(loss,quote.Template.Compromises[compromise]);
  var op=PermanentDevelopment.Open("op","one","two",["team-1"]);
  op=PermanentDevelopment.Edit(op,"team-1",new(Guid.NewGuid().ToString(),op.Id,op.Revision,"PackageCreation",template,quote.Id),staff);
  var command=new DevelopmentCommit(Guid.NewGuid().ToString(),op.Id,op.Revision,1,quote.Id);
  Assert.Throws<InvalidOperationException>(()=>PermanentDevelopment.Commit(op,[new("start","team-1",cost-1,"starting-credit")],"team-1",command,staff));
  var paid=PermanentDevelopment.Commit(op,[new("start","team-1",cost,"starting-credit")],"team-1",command,staff);
  var saved=System.Text.Json.JsonSerializer.Deserialize<DevelopmentState>(System.Text.Json.JsonSerializer.Serialize(new DevelopmentState(PermanentDevelopment.Version,[paid.State],new string('3',64))))!;
  var completed=PermanentDevelopment.Close(saved,op.Id,["one","two"]);
  var original=RacePackages.Migrate(WeekendPresetCatalog.Open("track-monza"));
  Assert.Single(original.PackageLibrary);
  Assert.Throws<ArgumentException>(()=>RacePackages.Select(original,new("op/team-1/package",1)));
  var library=RacePackages.Complete(original,completed.PackageOutcomes);
  Assert.Equal(2,library.PackageLibrary.Length);Assert.Equal("neutral-package-v1",library.SelectedPackage!.PackageId);
  var package=Assert.Single(completed.PackageOutcomes).Package!;
  var selected=RacePackages.Select(library,new(package.PackageId,package.Version));
  Assert.Equal(82+gain,WeekendPreparation.Prepare(selected,6).Cars[0].Source.Car.Ratings[strength]);
  Assert.Equal(82,original.Cars[0].Car.Ratings[strength]);
  Assert.Equal(completed,PermanentDevelopment.Close(completed,op.Id,["one","two"]));
  PackageDevelopment.ValidateQuote(quote);
  Assert.Throws<ArgumentException>(()=>PackageDevelopment.ValidateQuote(quote with{Cost=cost+1}));
 }
 [Fact]
 public void Creating_a_package_uses_one_quoted_decision_and_one_debit_then_completes_at_close()
 {
  var staff=new DevelopmentPersonnel("frozen-roster",0,0,0,0);
  var quote=PackageDevelopment.Quote("low-drag",staff);
  Assert.Equal(18,quote.Cost);
  var opportunity=PermanentDevelopment.Open("development","one","two",["team-1"]);
  var draft=PermanentDevelopment.Edit(opportunity,"team-1",new(Guid.NewGuid().ToString(),opportunity.Id,1,"PackageCreation","low-drag",quote.Id),staff);
  ImmutableArray<CreditEntry> ledger=[new("starting","team-1",100,"starting-credit")];
  var command=new DevelopmentCommit(Guid.NewGuid().ToString(),draft.Id,draft.Revision,1,quote.Id);
  var paid=PermanentDevelopment.Commit(draft,ledger,"team-1",command,staff);
  Assert.Equal(82,paid.Ledger.Sum(e=>e.Credits));
  Assert.Equal(paid, PermanentDevelopment.Commit(paid.State,paid.Ledger,"team-1",command,staff));
  Assert.Throws<InvalidOperationException>(()=>PermanentDevelopment.Edit(paid.State,"team-1",new(Guid.NewGuid().ToString(),paid.State.Id,paid.State.Revision,"Save",null,null),staff));
  var state=new DevelopmentState(PermanentDevelopment.Version,[paid.State],new string('2',64));
  Assert.True(state.PackageOutcomes.IsDefaultOrEmpty);
  var completed=PermanentDevelopment.Close(state,paid.State.Id,["one","two"]);
  var outcome=Assert.Single(completed.PackageOutcomes);
  Assert.Equal("normal",outcome.Kind);Assert.Equal("two",outcome.CompletionEventId);
  Assert.Equal(4,outcome.Package!.Strengths["topSpeed"]);Assert.Equal(-2,outcome.Package.Compromises["highSpeedAero"]);
  Assert.Equal(quote.Id,outcome.Contract.Id);Assert.Equal(staff,outcome.Contract.Personnel);
  Assert.Empty(completed.Modifiers);
 }
}
