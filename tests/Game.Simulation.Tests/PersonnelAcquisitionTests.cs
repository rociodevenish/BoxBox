using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class PersonnelAcquisitionTests
{
 [Theory]
 [InlineData(-1)]
 [InlineData(0)]
 [InlineData(72)]
 public void Eleven_teams_complete_all_seventy_seven_saved_picks_without_duplicate_ownership(int seed)
 {
  var state=PersonnelAcquisition.Open(PersonnelCatalog.Open(),Enumerable.Range(1,11).Select(i=>$"team-{i:00}").ToArray(),seed);
  while(state.Offer is {} offer)
  {
   var person=offer.PersonIds.Select(id=>state.Pool.People.Single(p=>p.Id==id)).OrderBy(p=>p.Price).ThenBy(p=>p.Id).First();
   state=PersonnelAcquisition.Hire(state,new(Guid.NewGuid().ToString(),state.Revision,offer.TeamId,person.Id));
   PersonnelAcquisition.Validate(state);
  }
  Assert.Equal(77,state.Carousel!.Cursor);Assert.Equal(77,state.Hires.Select(h=>h.PersonId).Distinct().Count());
  Assert.All(state.TeamIds,team=>Assert.Equal(7,state.Hires.Count(h=>h.TeamId==team)));
 }

 [Fact]
 public void Insufficient_alternatives_and_exhausted_tokens_preserve_the_saved_offer_and_revision()
 {
  var pool=PersonnelCatalog.Open();
  var small=pool with{People=pool.People.Where(p=>p.Role!="Manager").Concat(pool.People.Where(p=>p.Role=="Manager").Take(5)).ToImmutableArray()};
  var state=PersonnelAcquisition.Open(small,["team-01"]);
  var before=System.Text.Json.JsonSerializer.Serialize(state);
  Assert.Contains("Insufficient alternatives",PersonnelAcquisition.ScoutUnavailable(state,"team-01"));
  Assert.Throws<InvalidOperationException>(()=>PersonnelAcquisition.Scout(state,new(Guid.NewGuid().ToString(),state.Revision,"team-01")));
  Assert.Equal(before,System.Text.Json.JsonSerializer.Serialize(state));
  var zero=PersonnelAcquisition.Open(pool,["team-01"],123,new("bounded-scouts-v1",0,0,0));
  Assert.Contains("No Scout tokens",PersonnelAcquisition.ScoutUnavailable(zero,"team-01"));
  Assert.Throws<InvalidOperationException>(()=>PersonnelAcquisition.Scout(zero,new(Guid.NewGuid().ToString(),zero.Revision,"team-01")));
  Assert.Throws<ArgumentException>(()=>PersonnelAcquisition.Open(pool,["team-01"],123,new("bounded-scouts-v1",4,1,4)));
 }

 [Fact]
 public void Serialised_cursor_draws_and_tokens_resume_without_a_free_scout_or_duplicate_hire()
 {
  var state=PersonnelAcquisition.Open(PersonnelCatalog.Open(),["team-01","team-02"]);
  var scout=new PersonnelScout(Guid.NewGuid().ToString(),state.Revision,state.Offer!.TeamId);
  state=PersonnelAcquisition.Scout(state,scout);
  var hire=new PersonnelHire(Guid.NewGuid().ToString(),state.Revision,state.Offer!.TeamId,state.Offer.PersonIds[0]);
  state=PersonnelAcquisition.Hire(state,hire);
  var saved=System.Text.Json.JsonSerializer.Serialize(state);
  var recovered=System.Text.Json.JsonSerializer.Deserialize<AcquisitionState>(saved)!;
  PersonnelAcquisition.Validate(recovered);
  Assert.Same(recovered,PersonnelAcquisition.Scout(recovered,scout));
  Assert.Same(recovered,PersonnelAcquisition.Hire(recovered,hire));
  Assert.Equal(saved,System.Text.Json.JsonSerializer.Serialize(recovered));
  Assert.Throws<ArgumentException>(()=>PersonnelAcquisition.Validate(recovered with{Version="personnel-acquisition-v1"}));
 }

 [Fact]
 public void Scout_replaces_the_whole_offer_once_and_returns_reserved_people_for_later_picks()
 {
  var pool=PersonnelCatalog.Open();
  pool=pool with{People=pool.People.Where(p=>p.Role!="Manager").Concat(pool.People.Where(p=>p.Role=="Manager").Take(6)).ToImmutableArray()};
  var state=PersonnelAcquisition.Open(pool,["team-01","team-02","team-03"],123);
  Assert.Equal(2,state.ScoutTokens![state.Carousel!.InitialOrder[0]]);
  Assert.Equal(3,state.ScoutTokens[state.Carousel.InitialOrder[2]]);
  var original=state.Offer!;
  var command=new PersonnelScout(Guid.NewGuid().ToString(),state.Revision,original.TeamId);
  var scouted=PersonnelAcquisition.Scout(state,command);
  Assert.Empty(scouted.Offer!.PersonIds.Intersect(original.PersonIds));
  Assert.Equal(state.ScoutTokens[original.TeamId]-1,scouted.ScoutTokens![original.TeamId]);
  Assert.Equal(state.Carousel.Cursor,scouted.Carousel!.Cursor);
  Assert.Equal(2,scouted.Draws.Length);
  Assert.Same(scouted,PersonnelAcquisition.Scout(scouted,command));
  Assert.Throws<InvalidOperationException>(()=>PersonnelAcquisition.Hire(scouted,new(command.CommandId,scouted.Revision,original.TeamId,scouted.Offer.PersonIds[0])));
  var hired=scouted.Offer.PersonIds[0];
  // Six managers ensure that a three-card next offer must include at least one returned card.
  // Keep frozen history intact; the smaller valid pool was established at opening below.
  var next=PersonnelAcquisition.Hire(scouted,new(Guid.NewGuid().ToString(),scouted.Revision,original.TeamId,hired));
  Assert.NotEmpty(original.PersonIds.Intersect(next.Offer!.PersonIds));
  PersonnelAcquisition.Validate(next);
 }

 [Fact]
 public void Saved_seed_schedule_rotates_categories_and_reverses_second_slots_independent_of_join_order()
 {
  var pool=PersonnelCatalog.Open();
  var state=PersonnelAcquisition.Open(pool,["team-01","team-02","team-03"],123);
  var reordered=PersonnelAcquisition.Open(pool,["team-03","team-01","team-02"],123);
  Assert.Equal(state.Carousel!.InitialOrder.ToArray(),reordered.Carousel!.InitialOrder.ToArray());
  Assert.Equal(state.Carousel.Picks.ToArray(),reordered.Carousel.Picks.ToArray());
  var order=state.Carousel.InitialOrder;
  Assert.Equal(order,state.Carousel.Picks.Take(3).Select(p=>p.TeamId));
  Assert.Equal(new[]{order[1],order[2],order[0]},state.Carousel.Picks.Skip(3).Take(3).Select(p=>p.TeamId));
  Assert.Equal(new[]{order[0],order[2],order[1]},state.Carousel.Picks.Skip(6).Take(3).Select(p=>p.TeamId));
  for(var pick=0;pick<21;pick++)
  {
   Assert.Equal(state.Carousel!.Picks[pick].TeamId,state.Offer!.TeamId);
   Assert.Equal(state.Carousel!.Picks[pick].Slot,state.Offer.Slot);
   state=PersonnelAcquisition.Hire(state,new(Guid.NewGuid().ToString(),state.Revision,state.Offer.TeamId,state.Offer.PersonIds.OrderBy(id=>pool.People.Single(p=>p.Id==id).Price).First()));
   PersonnelAcquisition.Validate(state);
  }
  Assert.Null(state.Offer);Assert.Equal(21,state.Hires.Length);Assert.Equal(21,state.Carousel!.Cursor);
 }

 [Fact]
 public void Unaffordable_offer_preserves_the_recovered_roster_and_credit_entries()
 {
  var state=PersonnelAcquisition.Open(PersonnelCatalog.Open(),["team-01"]);
  var first=state.Offer!.PersonIds[0];
  state=state with{Pool=state.Pool with{People=state.Pool.People.Select(p=>p.Id==first?p with{Price=98}:p).ToImmutableArray()}};
  state=PersonnelAcquisition.Hire(state,new(Guid.NewGuid().ToString(),state.Revision,"team-01",first));
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
  var active=state.Offer.TeamId;var waiting=state.TeamIds.Single(t=>t!=active);
  var command=new PersonnelHire(Guid.NewGuid().ToString(),state.Revision,active,person.Id);
  var purchased=PersonnelAcquisition.Hire(state,command);
  Assert.Equal(100-person.Price,PersonnelAcquisition.Balance(purchased,active));
  Assert.Equal(100,PersonnelAcquisition.Balance(purchased,waiting));
  Assert.Equal(person.Id,Assert.Single(purchased.Hires).PersonId);
  Assert.Equal("manager",Assert.Single(purchased.Hires).Slot);
  Assert.Same(purchased,PersonnelAcquisition.Hire(purchased,command));
  Assert.Equal(100,PersonnelAcquisition.Balance(state,"team-01"));
 }
}
