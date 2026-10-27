using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class ConstructionConfirmationTests
{
 [Fact]
 public void An_eighteen_credit_baseline_can_leave_zero_without_allowing_unrelated_ledger_debits()
 {
  var pool=PersonnelCatalog.Open();pool=pool with{StartingCredit=25,People=pool.People.Select(p=>p with{Price=1}).ToImmutableArray()};
  var personnel=PersonnelAcquisition.Open(pool,["team-a"]);
  while(personnel.Offer is {} offer)personnel=PersonnelAcquisition.Hire(personnel,new(Guid.NewGuid().ToString(),personnel.Revision,"team-a",offer.PersonIds[0]));
  var state=CarConstruction.Open(ConstructionCatalog.Open(),["team-a"]);
  var expensive=state.Catalogue.Categories.Select(c=>c.Options.OrderByDescending(o=>o.Cost).First().Id).ToImmutableArray();
  var previouslyAffordable=CarConstruction.Edit(state,new(Guid.NewGuid().ToString(),1,"team-a",state.Catalogue.Version,expensive),24);
  Assert.Throws<InvalidOperationException>(()=>CarConstruction.Confirm(previouslyAffordable,personnel,new(Guid.NewGuid().ToString(),2,"team-a",state.Catalogue.Version)));
  Assert.Equal(18,PersonnelAcquisition.Balance(personnel,"team-a"));Assert.Null(previouslyAffordable.Confirmed);
  var saved=CarConstruction.Confirm(state,personnel,new(Guid.NewGuid().ToString(),1,"team-a",state.Catalogue.Version));
  Assert.Equal(0,PersonnelAcquisition.Balance(saved.Acquisition,"team-a"));Assert.True(PersonnelAcquisition.RosterReady(saved.Acquisition,"team-a"));PersonnelAcquisition.Validate(saved.Acquisition);
  var debit=saved.Acquisition.Ledger[^1];
  Assert.Throws<ArgumentException>(()=>PersonnelAcquisition.Validate(saved.Acquisition with{Ledger=saved.Acquisition.Ledger.SetItem(saved.Acquisition.Ledger.Length-1,debit with{Credits=-17})}));
  Assert.Throws<ArgumentException>(()=>PersonnelAcquisition.Validate(saved.Acquisition with{Ledger=saved.Acquisition.Ledger.SetItem(saved.Acquisition.Ledger.Length-1,debit with{CommandId=Guid.NewGuid().ToString()})}));
  Assert.Throws<ArgumentException>(()=>PersonnelAcquisition.Validate(saved.Acquisition with{Ledger=saved.Acquisition.Ledger.Add(new(Guid.NewGuid().ToString(),"team-a",0,"unrecognised"))}));
 }
 [Fact]
 public void Confirmation_spends_once_freezes_the_saved_draft_and_preserves_personnel_history()
 {
  var personnel=PersonnelAcquisition.Open(PersonnelCatalog.Open(),["team-a"]);
  while(personnel.Offer is {} offer)
  {
   var person=offer.PersonIds.Where(id=>PersonnelAcquisition.HireUnavailable(personnel,"team-a",id) is null).OrderBy(id=>personnel.Pool.People.Single(p=>p.Id==id).Price).First();
   personnel=PersonnelAcquisition.Hire(personnel,new(Guid.NewGuid().ToString(),personnel.Revision,"team-a",person));
  }
  var state=CarConstruction.Open(ConstructionCatalog.Open(),["team-a"]);
  var command=new ConstructionConfirm(Guid.NewGuid().ToString(),1,"team-a",state.Catalogue.Version);
  var before=PersonnelAcquisition.Balance(personnel,"team-a");
  var saved=CarConstruction.Confirm(state,personnel,command);
  Assert.Equal(before-18,PersonnelAcquisition.Balance(saved.Acquisition,"team-a"));
  Assert.Equal(personnel.Commands,saved.Acquisition.Commands);
  Assert.Equal(85,saved.Construction.Confirmed!["team-a"].Ratings["reliability"]);
  var retry=CarConstruction.Confirm(saved.Construction,saved.Acquisition,command);
  Assert.Same(saved.Construction,retry.Construction);Assert.Same(saved.Acquisition,retry.Acquisition);
  Assert.Throws<InvalidOperationException>(()=>CarConstruction.Confirm(saved.Construction,saved.Acquisition,command with{CommandId=Guid.NewGuid().ToString()}));
  Assert.Throws<InvalidOperationException>(()=>CarConstruction.Edit(saved.Construction,new(Guid.NewGuid().ToString(),1,"team-a",state.Catalogue.Version,state.Drafts["team-a"].OptionIds),before));
  PersonnelAcquisition.Validate(saved.Acquisition);CarConstruction.Validate(saved.Construction,["team-a"]);
 }
}
