using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class RacePackageTests
{
 [Fact]
 public void Permanent_gains_package_and_setup_are_separate_immutable_layers_for_both_cars()
 {
  var baseline=RacePackages.Migrate(WeekendPresetCatalog.Open("track-monza"));
  var package=RacePackages.Baseline with{PackageId="thermal",Strengths=ImmutableDictionary<string,double>.Empty.Add("tyreTemperature",3),Compromises=ImmutableDictionary<string,double>.Empty.Add("reliability",-2)};
  var developed=DevelopedCar.Apply(baseline,[new("permanent","team-1","two",ImmutableDictionary<string,double>.Empty.Add("tyreTemperature",2).Add("reliability",1))]);
  var selected=RacePackages.Select(developed with{PackageLibrary=[RacePackages.Baseline,package]},new("thermal",1));
  var prepared=WeekendPreparation.Prepare(selected,3,new(0,0,1));
  Assert.All(prepared.Cars,car=>{
   Assert.Equal(87,car.Source.Car.Ratings["tyreTemperature"]);
   Assert.Equal(81,car.Source.Car.Ratings["reliability"]);
  });
  Assert.Equal(82,baseline.Cars[0].Car.Ratings["tyreTemperature"]);
  Assert.Equal(84,prepared.Source.Cars[0].Car.Ratings["tyreTemperature"]);
  var refined=package with{Version=2,Strengths=package.Strengths.SetItem("tyreTemperature",4),RefinementHistory=["refinement-project"]};
  var later=selected with{PackageLibrary=selected.PackageLibrary.Add(refined)};
  _=WeekendPreparation.Prepare(RacePackages.Select(later,new("thermal",2)),3,new());
  Assert.Equal(1,prepared.Source.SelectedPackage!.Version);
  Assert.Equal(87,prepared.Cars[0].Source.Car.Ratings["tyreTemperature"]);
 }
 [Fact]
 public void Only_selected_completed_version_composes_once_before_setup_and_replays()
 {
  var baseline=RacePackages.Migrate(WeekendPresetCatalog.Open("track-monza"));
  var fast=RacePackages.Baseline with{PackageId="low-drag",Name="Low drag",Strengths=ImmutableDictionary<string,double>.Empty.Add("topSpeed",4),Compromises=ImmutableDictionary<string,double>.Empty.Add("highSpeedAero",-2)};
  var pending=fast with{Version=2,Completed=false};
  var library=baseline with{PackageLibrary=[RacePackages.Baseline,fast,pending]};
  var selected=RacePackages.Select(library,new("low-drag",1));
  var effective=WeekendPreparation.Prepare(selected,3,new());
  Assert.Equal(82,effective.Source.Cars[0].Car.Ratings["topSpeed"]);
  Assert.Equal(86,effective.Cars[0].Source.Car.Ratings["topSpeed"]);
  Assert.Equal(80,effective.Cars[0].Source.Car.Ratings["highSpeedAero"]);
  Assert.True(effective.Cars[0].RaceDriver.Speed>WeekendPreparation.Prepare(baseline,3,new()).Cars[0].RaceDriver.Speed);
  Assert.Equal(WeekendPreparation.Prepare(selected,3,new()).Cars[0].RaceDriver,effective.Cars[0].RaceDriver);
  Assert.Equal(WeekendPreparation.Prepare(baseline,3,new()).Cars[0].RaceDriver,WeekendPreparation.Prepare(RacePackages.Select(selected,new("neutral-package-v1",1)),3,new()).Cars[0].RaceDriver);
  Assert.Throws<ArgumentException>(()=>RacePackages.Select(library,new("low-drag",2)));
  Assert.Throws<ArgumentException>(()=>RacePackages.Select(library,new("missing",1)));
  var unavailable=library with{PackageId="low-drag",SelectedPackage=new("low-drag",2)};
  Assert.Throws<ArgumentException>(()=>WeekendPreparation.Prepare(unavailable,3));
  Assert.Equal("neutral-package-v1",RacePackages.Select(unavailable,new("neutral-package-v1",1)).PackageId);
  var replay=System.Text.Json.JsonSerializer.Deserialize<EffectiveWeekend>(System.Text.Json.JsonSerializer.Serialize(effective))!;
  Assert.Equal(WeekendPreparation.CreateRace(effective,3).GetState().Classification,WeekendPreparation.CreateRace(replay,3).GetState().Classification);
 }
 [Fact]
 public void Explicit_neutral_migration_preserves_performance_and_rejects_unknown_legacy_context()
 {
  var preset=WeekendPresetCatalog.Open("track-monza");
  var migrated=RacePackages.Migrate(preset);
  Assert.Equal("neutral-package-v1",migrated.SelectedPackage!.PackageId);
  Assert.Single(migrated.PackageLibrary);
  Assert.Equal(WeekendPreparation.Prepare(preset,3).Cars.Select(c=>c.RaceDriver),WeekendPreparation.Prepare(migrated,3).Cars.Select(c=>c.RaceDriver));
  Assert.Throws<ArgumentException>(()=>RacePackages.Migrate(preset with{PackageId="missing",SelectedPackage=null,PackageLibrary=default}));
 }
}

