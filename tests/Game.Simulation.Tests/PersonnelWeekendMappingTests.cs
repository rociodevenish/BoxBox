using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class PersonnelWeekendMappingTests
{
 [Fact]
 public void Completed_roster_uses_frozen_driver_inputs_without_changing_constructor_or_entry_identity()
 {
  var source=WeekendPresetCatalog.Open("track-monza");
  var team=source.Cars[0].TeamId;
  var state=Complete(team);
  var hire=state.Hires.Single(h=>h.Slot=="driver-1");
  var frozen=state.Pool.People.Single(p=>p.Id==hire.PersonId).Driver!;
  state=state with{Pool=state.Pool with{People=state.Pool.People.Select(p=>p.Id==hire.PersonId?p with{Driver=frozen with{Ratings=frozen.Ratings.SetItem("racePace",63)}}:p).ToImmutableArray()}};
  var result=PersonnelWeekendMapping.Apply(state,team,source);
  Assert.Equal(source.Cars.Select(c=>(c.EntryId,c.TeamId,c.Car)),result.Cars.Select(c=>(c.EntryId,c.TeamId,c.Car)));
  Assert.Equal(frozen.Id,result.Cars[0].Driver.Id);
  Assert.Equal(63,result.Cars[0].Driver.Ratings["racePace"]);
  Assert.Equal(frozen.Preferences,result.Cars[0].Driver.Preferences);
  Assert.Equal(state.Hires.Single(h=>h.Slot=="race-engineer-1").PersonId,result.Cars[0].Engineer!.Id);
 }
 [Fact]
 public void Each_drivers_engineer_narrows_only_their_practice_information_without_a_pace_bonus()
 {
  var source=WeekendPresetCatalog.Open("track-monza");
  source=source with{Cars=source.Cars.Select((car,index)=>car with{
   Driver=car.Driver with{Ratings=car.Driver.Ratings.SetItem("setupFeedback",50)},
   Engineer=new("engineer-"+index,"Assigned engineer","authored-engineer-v1",index==0?10:100)
  }).ToImmutableArray()};
  var result=WeekendPreparation.Assess(source,6,new(0,0,0),73);
  Assert.Equal("Low",result.Feedback.Drivers[0].Confidence);
  Assert.Equal("Moderate",result.Feedback.Drivers[1].Confidence);
  Assert.NotEqual(result.Feedback.Drivers[0].Uncertainty,result.Feedback.Drivers[1].Uncertainty);
  var swapped=source with{Cars=source.Cars.Select(car=>car with{Engineer=car.Engineer! with{Setup=100}}).ToImmutableArray()};
  Assert.Equal(WeekendPreparation.Prepare(source,6,new(0,0,0)).Cars.Select(car=>car.RaceDriver),
   WeekendPreparation.Prepare(swapped,6,new(0,0,0)).Cars.Select(car=>car.RaceDriver));
 }
 [Fact]
 public void Incomplete_rosters_and_other_constructors_cannot_replace_a_weekend_roster()
 {
  var source=WeekendPresetCatalog.Open("track-monza");
  var team=source.Cars[0].TeamId;
  Assert.Throws<ArgumentException>(()=>PersonnelWeekendMapping.Apply(PersonnelAcquisition.Open(PersonnelCatalog.Open(),[team]),team,source));
  Assert.Throws<ArgumentException>(()=>PersonnelWeekendMapping.Apply(Complete("another-constructor"),"another-constructor",source));
 }
 private static AcquisitionState Complete(string team)
 {
  var state=PersonnelAcquisition.Open(PersonnelCatalog.Open(),[team]);
  while(state.Offer is {} offer)
  {
   var id=offer.PersonIds.First(id=>PersonnelAcquisition.HireUnavailable(state,team,id) is null);
   state=PersonnelAcquisition.Hire(state,new(Guid.NewGuid().ToString(),state.Revision,team,id));
  }
  return state;
 }
}


