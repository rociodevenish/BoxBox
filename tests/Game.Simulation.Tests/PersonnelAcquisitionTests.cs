using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class PersonnelAcquisitionTests
{
 [Fact]
 public void Unaffordable_offer_preserves_the_recovered_roster_and_credit_entries()
 {
  // A valid recovered snapshot after an earlier expensive Driver pick. The carousel is a later consumer.
  var state=PersonnelAcquisition.Open(PersonnelCatalog.Open(),["team-01"]);
  var driver=state.Pool.People.First(p=>p.Role=="Driver") with{Price=98};
  var earlier=Guid.NewGuid().ToString();
  state=state with{Revision=2,Pool=state.Pool with{People=state.Pool.People.Select(p=>p.Id==driver.Id?driver:p).ToImmutableArray()},
   Hires=[new("team-01","driver-1",driver.Id,earlier)],Ledger=state.Ledger.Add(new(earlier,"team-01",-98,"personnel-hire",driver.Id)),Commands=[new(earlier,"earlier-accepted-driver-hire")]};
  PersonnelAcquisition.Validate(state);
  Assert.Throws<InvalidOperationException>(()=>PersonnelAcquisition.Hire(state,new(Guid.NewGuid().ToString(),2,"team-01",state.Offer!.PersonIds[0])));
  Assert.Equal(2,PersonnelAcquisition.Balance(state,"team-01"));Assert.Single(state.Hires);Assert.Equal(2,state.Ledger.Length);
 }

 [Fact]
 public void Invalid_content_version_or_missing_mandatory_people_fails_before_acquisition()
 {
  var pool=PersonnelCatalog.Open();
  Assert.Throws<ArgumentException>(()=>PersonnelAcquisition.Open(pool with{Version="unsupported-v99"},["team-01"]));
  Assert.Throws<ArgumentException>(()=>PersonnelAcquisition.Open(pool with{People=pool.People.Where(p=>p.Role!="Technical Engineer").ToImmutableArray()},["team-01"]));
  Assert.Equal(11,PersonnelAcquisition.Open(pool,Enumerable.Range(1,11).Select(i=>$"team-{i:00}").ToArray()).TeamIds.Length);
 }
 [Fact]
 public void First_hire_commits_one_unique_person_slot_and_immutable_debit_and_retry_is_identical()
 {
  var state=PersonnelAcquisition.Open(PersonnelCatalog.Open(),["team-01","team-02"]);
  Assert.Equal(3,state.Offer!.PersonIds.Length);
  var person=state.Pool.People.Single(p=>p.Id==state.Offer.PersonIds[0]);
  var command=new PersonnelHire(Guid.NewGuid().ToString(),state.Revision,"team-01",person.Id);
  var purchased=PersonnelAcquisition.Hire(state,command);
  Assert.Equal(100-person.Price,PersonnelAcquisition.Balance(purchased,"team-01"));
  Assert.Equal(100,PersonnelAcquisition.Balance(purchased,"team-02"));
  Assert.Equal(person.Id,Assert.Single(purchased.Hires).PersonId);
  Assert.Equal("manager",Assert.Single(purchased.Hires).Slot);
  Assert.Same(purchased,PersonnelAcquisition.Hire(purchased,command));
  Assert.Equal(100,PersonnelAcquisition.Balance(state,"team-01"));
 }
}
