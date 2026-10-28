using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class CarConstructionTests
{
 [Fact]
 public void Every_team_can_retain_the_same_complete_minimum_build_for_eighteen_credits()
 {
  var state=CarConstruction.Open(ConstructionCatalog.Open(),["team-a","team-b"]);
  Assert.Equal(18,CarConstruction.Cost(state.Catalogue,state.Drafts["team-a"].OptionIds));
  Assert.Equal(state.Drafts["team-a"].OptionIds,state.Drafts["team-b"].OptionIds);
  Assert.Equal(4,state.Catalogue.Categories.Length);
  Assert.Equal(84,state.Catalogue.Baseline["reliability"]);
 }
 [Fact]
 public void Draft_editing_uses_actual_credits_and_retries_without_spending_or_replacing_a_revision()
 {
  var state=CarConstruction.Open(ConstructionCatalog.Open(),["team-a"]);
  var options=new[]{"car-aero-low-drag-efficiency","car-chassis-braking","car-power-top-end","car-thermal-low-drag"}.ToImmutableArray();
  var command=new ConstructionEdit(Guid.NewGuid().ToString(),1,"team-a",state.Catalogue.Version,options);
  Assert.Throws<InvalidOperationException>(()=>CarConstruction.Edit(state,command,18));
  Assert.Equal(1,state.Drafts["team-a"].Revision);
  var saved=CarConstruction.Edit(state,command,19);
  Assert.Equal(2,saved.Drafts["team-a"].Revision);
  Assert.Same(saved,CarConstruction.Edit(saved,command,19));
  Assert.Throws<InvalidOperationException>(()=>CarConstruction.Edit(saved,command with{CommandId=Guid.NewGuid().ToString()},19));
  Assert.Equal(85.5,CarConstruction.Ratings(saved.Catalogue,options)["topSpeed"]);
  Assert.Equal(82,CarConstruction.Ratings(saved.Catalogue,options)["reliability"]);
  CarConstruction.Validate(saved,["team-a"]);
 } [Fact]
 public void A_preview_shows_both_drivers_and_representative_circuit_compromises_without_mutating_the_preset()
 {
  var catalogue=ConstructionCatalog.Open();
  var preset=WeekendPresetCatalog.Open("track-monza");
  var options=new[]{"car-aero-low-drag-efficiency","car-chassis-braking","car-power-top-end","car-thermal-low-drag"}.ToImmutableArray();
  var preview=ConstructionPreview.Preview(catalogue,options,preset,19);
  Assert.Equal(2,preview.DriverFits.Length);
  Assert.Equal(0,preview.RemainingCredits);
  Assert.Equal(2,preview.Tracks.Length);
  Assert.Contains(preview.Tracks,t=>t.TrackId=="track-monza"&&t.Strengths.Contains("Straight performance"));
  Assert.Contains(preview.Tracks,t=>t.Compromises.Contains("Reliability margin"));
  Assert.Equal(82,preset.Cars[0].Car.Ratings["topSpeed"]);
 } [Fact]
 public void All_six_hundred_twenty_five_source_combinations_are_bounded_and_no_choice_is_scarce()
 {
  var catalogue=ConstructionCatalog.Open();var count=0;var costs=new List<int>();
  foreach(var aero in catalogue.Categories[0].Options)
  foreach(var chassis in catalogue.Categories[1].Options)
  foreach(var power in catalogue.Categories[2].Options)
  foreach(var thermal in catalogue.Categories[3].Options)
  {
   ImmutableArray<string> choices=[aero.Id,chassis.Id,power.Id,thermal.Id];
   var source=CarConstruction.Open(catalogue,["a","b"]);
   foreach(var team in new[]{"a","b"})source=CarConstruction.Edit(source,new(Guid.NewGuid().ToString(),1,team,catalogue.Version,choices),24);
   Assert.All(CarConstruction.Ratings(catalogue,choices).Values,value=>Assert.InRange(value,1,100));
   Assert.Equal(source.Drafts["a"].OptionIds,source.Drafts["b"].OptionIds);
   costs.Add(CarConstruction.Cost(catalogue,choices));count++;
  }
  Assert.Equal(625,count);Assert.Equal(18,costs.Min());Assert.Equal(24,costs.Max());
  Assert.Throws<ArgumentException>(()=>CarConstruction.Ratings(catalogue,["car-aero-high-downforce","car-aero-low-drag-efficiency","car-power-robust","car-thermal-low-drag"]));
 } [Fact]
 public void Simple_groups_use_the_five_locked_source_weighted_summaries_of_detailed_values()
 {
  var preview=ConstructionPreview.Preview(ConstructionCatalog.Open(),["car-aero-low-drag-efficiency","car-chassis-braking","car-power-top-end","car-thermal-low-drag"],WeekendPresetCatalog.Open("track-monza"),19);
  Assert.Equal(5,preview.Groups.Count);
  Assert.Equal(84.75,preview.Groups["Speed"],6);
  Assert.Equal(81.45,preview.Groups["Cornering"],6);
  Assert.Equal(82.7,preview.Groups["Mechanical Grip"],6);
  Assert.Equal(81.55,preview.Groups["Tyres"],6);
  Assert.Equal(82,preview.Groups["Reliability"]);
 }}




