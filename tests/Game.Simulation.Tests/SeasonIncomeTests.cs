using System.Collections.Immutable;
using Game.Domain;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class SeasonIncomeTests
{
 [Fact]
 public void Completed_event_grants_every_team_the_same_frozen_credits_once_and_retains_spending()
 {
  ImmutableArray<CreditEntry> ledger=[new("starting-credit","a",100,"starting-credit"),new("hire","a",-24,"personnel-hire"),new("starting-credit","b",100,"starting-credit")];
  var granted=SeasonIncome.Grant(ledger,["a","b"],"event-1",SeasonIncome.Default);
  Assert.Equal(82,granted.Where(e=>e.TeamId=="a").Sum(e=>e.Credits));
  Assert.Equal(106,granted.Where(e=>e.TeamId=="b").Sum(e=>e.Credits));
  Assert.Equal(granted,SeasonIncome.Grant(granted,["a","b"],"event-1",SeasonIncome.Default));
  Assert.All(granted.Where(e=>e.Reason=="event-income"),e=>Assert.Equal("flat-event-income-v1",e.Version));
 }
 [Fact]
 public void A_conflicting_grant_is_rejected_without_changing_the_saved_ledger()
 {
  ImmutableArray<CreditEntry> ledger=[new("event-income/event-1","a",7,"event-income",Version:"flat-event-income-v1",EventId:"event-1")];
  Assert.Throws<ArgumentException>(()=>SeasonIncome.Grant(ledger,["a"],"event-1",SeasonIncome.Default));
  Assert.Equal(7,Assert.Single(ledger).Credits);
 }
 [Fact]
 public void Acquired_roster_history_remains_valid_after_event_income_and_serialization()
 {
  var state=PersonnelAcquisition.Open(Game.Simulation.PersonnelCatalog.Open(),["a"]);
  while(state.Offer is {} offer)
  {
   var cheapest=offer.PersonIds.Select(id=>state.Pool.People.Single(p=>p.Id==id)).OrderBy(p=>p.Price).First();
   state=PersonnelAcquisition.Hire(state,new(Guid.NewGuid().ToString(),state.Revision,"a",cheapest.Id));
  }
  var before=PersonnelAcquisition.Balance(state,"a");
  state=state with{Ledger=SeasonIncome.Grant(state.Ledger,["a"],"event-1",SeasonIncome.Default)};
  var recovered=System.Text.Json.JsonSerializer.Deserialize<AcquisitionState>(System.Text.Json.JsonSerializer.Serialize(state))!;
  PersonnelAcquisition.Validate(recovered);
  Assert.Equal(before+6,PersonnelAcquisition.Balance(recovered,"a"));
 }
}
