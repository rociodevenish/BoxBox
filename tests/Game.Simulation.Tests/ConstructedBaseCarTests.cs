using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class ConstructedBaseCarTests
{
    [Fact]
    public void Confirmed_car_materializes_both_entries_and_preserves_personnel_and_replay()
    {
        var source = WeekendPresetCatalog.Open("track-monza");
        var ratings = source.Cars[0].Car.Ratings.SetItem("topSpeed", 85.5).SetItem("reliability", 86);
        var built = ConstructedBaseCar.Apply(source, "base-team-1", "catalogue-v1", ratings, 1);
        var effective = WeekendPreparation.Prepare(built, 3, new(0, 0, 0));
        Assert.Equal("base-team-1", effective.Source.BaseCarId);
        Assert.All(effective.Cars, car => Assert.Equal(85.5, car.Source.Car.Ratings["topSpeed"]));
        Assert.Equal(source.Cars[0].Driver, built.Cars[0].Driver);
        Assert.Equal(source.Engineer, built.Engineer);
        Assert.Equal(82, source.Cars[0].Car.Ratings["topSpeed"]);
        Assert.Equal(built, ConstructedBaseCar.Apply(built, "base-team-1", "catalogue-v1", ratings, 1));
        Assert.Throws<ArgumentException>(() => ConstructedBaseCar.Apply(built, "another-base", "catalogue-v1", ratings, 1));
    }

    [Fact]
    public void Construction_and_setup_handling_combine_once_without_mutating_the_base()
    {
        var source = WeekendPresetCatalog.Open("track-monza");
        var built = ConstructedBaseCar.Apply(source, "base-team-1", "catalogue-v1", source.Cars[0].Car.Ratings, 1);
        var combined = WeekendPreparation.Prepare(built, 3, new(0, -1, 0));
        var neutral = WeekendPreparation.Prepare(source, 3, new(0, 0, 0));
        Assert.Equal(neutral.Cars[0].RaceDriver.Pace, combined.Cars[0].RaceDriver.Pace);
        Assert.Equal(neutral.DriverFits[0], combined.DriverFits[0]);
        Assert.Equal(82, combined.Source.Cars[0].Car.Ratings["traction"]);
        Assert.NotEqual(WeekendPreparation.Prepare(built, 3).Cars[0].RaceDriver.Pace, neutral.Cars[0].RaceDriver.Pace);
        Assert.Equal(WeekendPreparation.Prepare(built, 3, new(0, 2, 0)).DriverFits[0],
            WeekendPreparation.Prepare(built, 3, new(0, 1, 0)).DriverFits[0]);
    }

    [Fact]
    public void Reliability_is_consumed_by_the_existing_explicit_incident_inputs()
    {
        var source = WeekendPresetCatalog.Open("track-monza");
        var built = ConstructedBaseCar.Apply(source, "base-team-1", "catalogue-v1",
            source.Cars[0].Car.Ratings.SetItem("reliability", 86), 0);
        var effective = WeekendPreparation.Prepare(built, 3);
        Assert.All(effective.Cars, car => Assert.Null(car.RaceDriver.IncidentProfile));
        var incidents = WeekendPresetCatalog.IncidentInputs(effective, new IncidentTuning());
        Assert.All(incidents.Drivers, car => Assert.Equal(86, car.IncidentProfile!.Reliability));
    }

    [Fact]
    public void Confirmed_straight_speed_reaches_public_race_outcomes_and_replays()
    {
        var source = WeekendPresetCatalog.Open("track-monza");
        double Finish(WeekendPreset preset)
        {
            var effective = WeekendPreparation.Prepare(preset, 3, new(0, 0, 0));
            var race = WeekendPreparation.CreateRace(effective, 17);
            PhaseResult result;
            do { result = race.ResolvePhase(); } while (!result.Finished);
            return result.Classification.Single(c => c.DriverId == "car-1").FinishTime!.Value;
        }
        var baseline = ConstructedBaseCar.Apply(source, "baseline", "catalogue-v1", source.Cars[0].Car.Ratings, 0);
        var speed = ConstructedBaseCar.Apply(source, "speed", "catalogue-v1", source.Cars[0].Car.Ratings.SetItem("topSpeed", 85.5), 0);
        Assert.True(Finish(speed) < Finish(baseline));
        Assert.Equal(Finish(speed), Finish(speed));
    }
}
