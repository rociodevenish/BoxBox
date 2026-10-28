using System.Collections.Immutable;
using Game.Domain;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class PermanentDevelopmentTests
{
 [Fact] public void Draft_is_free_and_one_committed_investment_debits_once_with_frozen_quote()
 {
  var state=PermanentDevelopment.Open("op-1","event-1","event-2",["a"]);
  var staff=new DevelopmentPersonnel("frozen-roster",60,70,65,75);
  var quote=PermanentDevelopment.Quote("speed",staff);
  var edit=new DevelopmentEdit(Guid.NewGuid().ToString(),"op-1",1,"PermanentRefinement","speed",quote.Id);
  state=PermanentDevelopment.Edit(state,"a",edit,staff);
  ImmutableArray<CreditEntry> ledger=[new("starting-credit","a",100,"starting-credit")];
  Assert.Equal(100,ledger.Sum(e=>e.Credits));
  var command=new DevelopmentCommit(Guid.NewGuid().ToString(),"op-1",2,1,quote.Id);
  var committed=PermanentDevelopment.Commit(state,ledger,"a",command,staff);
  Assert.Equal(85,committed.Ledger.Sum(e=>e.Credits));
  Assert.Equal("event-2",Assert.Single(committed.State.Decisions).IntendedEventId);
  Assert.Equal(staff,Assert.Single(committed.State.Decisions).Quote!.Personnel);
  Assert.Equal(committed,PermanentDevelopment.Commit(committed.State,committed.Ledger,"a",command,staff));
  Assert.Throws<InvalidOperationException>(()=>PermanentDevelopment.Commit(committed.State,committed.Ledger,"a",command with{CommandId=Guid.NewGuid().ToString()},staff));
 }
 [Fact] public void Invalid_or_unaffordable_choices_never_become_Save_and_explicit_Save_is_free()
 {
  var state=PermanentDevelopment.Open("op","from","next",["a"]);var staff=new DevelopmentPersonnel("preset",0,0,0,0);
  var quote=PermanentDevelopment.Quote("speed",staff);
  Assert.Throws<ArgumentException>(()=>PermanentDevelopment.Edit(state,"a",null!,staff));
  Assert.Throws<ArgumentException>(()=>PermanentDevelopment.Edit(state,"a",new(Guid.NewGuid().ToString(),"op",1,"Package",null,null),staff));
  state=PermanentDevelopment.Edit(state,"a",new(Guid.NewGuid().ToString(),"op",1,"PermanentRefinement","speed",quote.Id),staff);
  ImmutableArray<CreditEntry> ledger=[new("starting-credit","a",5,"starting-credit")];
  Assert.Throws<InvalidOperationException>(()=>PermanentDevelopment.Commit(state,ledger,"a",new(Guid.NewGuid().ToString(),"op",2,1,quote.Id),staff));
  Assert.Empty(state.Decisions);Assert.Equal(5,ledger.Sum(e=>e.Credits));
  state=PermanentDevelopment.Edit(state,"a",new(Guid.NewGuid().ToString(),"op",2,"Save",null,null),staff);
  var saved=PermanentDevelopment.Commit(state,ledger,"a",new(Guid.NewGuid().ToString(),"op",3,2,null),staff);
  Assert.Equal("Save",Assert.Single(saved.State.Decisions).ActionKind);Assert.Equal(ledger,saved.Ledger);
 } [Fact] public void Acquired_campaign_uses_the_same_ledger_and_preserves_roster_and_Base_Car_after_restart()
 {
  var acquisition=PersonnelAcquisition.Open(Game.Simulation.PersonnelCatalog.Open(),["a"]);
  while(acquisition.Offer is {} offer){var person=offer.PersonIds.Where(id=>PersonnelAcquisition.HireUnavailable(acquisition,"a",id) is null).OrderBy(id=>acquisition.Pool.People.Single(p=>p.Id==id).Price).First();acquisition=PersonnelAcquisition.Hire(acquisition,new(Guid.NewGuid().ToString(),acquisition.Revision,"a",person));}
  var construction=CarConstruction.Open(Game.Simulation.ConstructionCatalog.Open(),["a"]);
  acquisition=CarConstruction.Confirm(construction,acquisition,new(Guid.NewGuid().ToString(),1,"a",construction.Catalogue.Version)).Acquisition;
  acquisition=acquisition with{Ledger=SeasonIncome.Grant(acquisition.Ledger,["a"],"from",SeasonIncome.Default)};
  var before=PersonnelAcquisition.Balance(acquisition,"a");var car=System.Text.Json.JsonSerializer.Serialize(acquisition.ConstructedCars);var hires=acquisition.Hires;
  var staff=new DevelopmentPersonnel("frozen",0,0,0,0);var quote=PermanentDevelopment.Quote("tyres",staff);var opportunity=PermanentDevelopment.Open("op","from","next",["a"]);
  opportunity=PermanentDevelopment.Edit(opportunity,"a",new(Guid.NewGuid().ToString(),"op",1,"PermanentRefinement","tyres",quote.Id),staff);
  var accepted=PermanentDevelopment.Commit(opportunity,acquisition.Ledger,"a",new(Guid.NewGuid().ToString(),"op",2,1,quote.Id),staff);
  acquisition=System.Text.Json.JsonSerializer.Deserialize<AcquisitionState>(System.Text.Json.JsonSerializer.Serialize(acquisition with{Ledger=accepted.Ledger}))!;
  PersonnelAcquisition.Validate(acquisition);Assert.Equal(before-12,PersonnelAcquisition.Balance(acquisition,"a"));Assert.Equal(car,System.Text.Json.JsonSerializer.Serialize(acquisition.ConstructedCars));Assert.Equal(hires.ToArray(),acquisition.Hires.ToArray());
  Assert.Throws<ArgumentException>(()=>PersonnelAcquisition.Validate(acquisition with{Ledger=acquisition.Ledger.SetItem(acquisition.Ledger.Length-1,acquisition.Ledger[^1] with{Credits=0})}));
 }}




