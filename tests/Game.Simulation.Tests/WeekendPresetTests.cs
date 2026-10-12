using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class WeekendPresetTests
{
    [Fact]
    public void Team_setup_preserves_sources_and_exchanges_speed_for_cornering_for_both_cars()
    {
        var preset = WeekendPresetCatalog.Open("track-monza");
        var speed = WeekendPreparation.Prepare(preset, 3, new(-2, 0, 0));
        var downforce = WeekendPreparation.Prepare(preset, 3, new(2, 0, 0));
        Assert.All(speed.Cars, car => Assert.Equal(82, car.Source.Car.Ratings["topSpeed"]));
        for (var i = 0; i < 2; i++)
        {
            Assert.True(speed.Cars[i].RaceDriver.Speed > downforce.Cars[i].RaceDriver.Speed);
            Assert.True(speed.Cars[i].RaceDriver.Cornering < downforce.Cars[i].RaceDriver.Cornering);
        }
        Assert.Equal(new(-2, 0, 0), speed.Setup);
        Assert.Throws<ArgumentException>(() => WeekendPreparation.Prepare(preset, 3, new(3, 0, 0)));
    }
    [Fact]
    public void Aero_setup_race_advantage_reverses_between_monza_and_monaco()
    {
        double Difference(string circuit)
        {
            double Time(int aero)
            {
                var weekend=WeekendPreparation.Prepare(WeekendPresetCatalog.Open(circuit),3,new(aero,0,0));
                weekend=weekend with {Configuration=weekend.Configuration with {Traffic=new(Enabled:false),Tyres=new(DegradationPerLap:0,WarmupPenalty:0)}};
                return Finish(weekend).Classification.Single(c=>c.DriverId=="car-1").FinishTime!.Value;
            }
            return Time(-2)-Time(2);
        }
        Assert.True(Difference("track-monza")<0);
        Assert.True(Difference("track-monaco")>0);
    }

    [Fact]
    public void Warm_up_choice_buys_first_lap_response_while_preservation_buys_stint_life()
    {
        double Time(int laps,int tyres)
        {
            var source=WeekendPresetCatalog.Open("track-monza");
            var weekend=WeekendPreparation.Prepare(source,laps,new(0,0,tyres));
            weekend=weekend with { Configuration=weekend.Configuration with { Traffic=new(Enabled:false) } };
            return Finish(weekend).Classification.Single(c=>c.DriverId=="car-1").FinishTime!.Value;
        }
        Assert.True(Time(1,-2)<Time(1,2));
        Assert.True(Time(20,2)<Time(20,-2));
    }

    [Fact]
    public void Adaptability_moderates_different_driver_preferences_and_information_has_no_pace_effect()
    {
        var source=WeekendPresetCatalog.Open("track-monza");
        var first=source.Cars[0]; var second=source.Cars[1];
        var cars=ImmutableArray.Create(first with {Driver=first.Driver with {Preferences=[new("responsive-front","Responsive front")],Ratings=first.Driver.Ratings.SetItem("adaptability",10)}},
            second with {Driver=first.Driver with {Id=second.Driver.Id,Preferences=[new("stable-rear","Stable rear")],Ratings=first.Driver.Ratings.SetItem("adaptability",10)}});
        var preset=source with {Cars=cars};
        var stable=WeekendPreparation.Prepare(preset,3,new(0,2,0));
        Assert.True(stable.DriverFits[1].Compatibility>stable.DriverFits[0].Compatibility);
        var adaptable=WeekendPreparation.Prepare(preset with {Cars=cars.SetItem(0,cars[0] with {Driver=cars[0].Driver with {Ratings=cars[0].Driver.Ratings.SetItem("adaptability",100)}})},3,new(0,2,0));
        Assert.True(adaptable.DriverFits[0].Compatibility>stable.DriverFits[0].Compatibility);
        var information=WeekendPreparation.Prepare(preset with {Engineer=preset.Engineer with {Setup=100},Cars=cars.Select(c=>c with {Driver=c.Driver with {Ratings=c.Driver.Ratings.SetItem("setupFeedback",100)}}).ToImmutableArray()},3,new(0,2,0));
        Assert.Equal(stable.Cars.Select(c=>c.RaceDriver),information.Cars.Select(c=>c.RaceDriver));
        var replay=System.Text.Json.JsonSerializer.Deserialize<EffectiveWeekend>(System.Text.Json.JsonSerializer.Serialize(stable))!;
        Assert.Equal(Finish(stable).Classification,Finish(replay).Classification);
    }

    [Fact]
    public void Prepared_weekend_retains_two_car_sources_and_can_replay_without_content_files()
    {
        var weekend = WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"), 3);
        Assert.Equal(2, weekend.Cars.Length);
        Assert.All(weekend.Cars, c => Assert.Equal(82, c.Source.Car.Ratings["topSpeed"]));
        var first = Finish(weekend);
        var replay = System.Text.Json.JsonSerializer.Deserialize<EffectiveWeekend>(
            System.Text.Json.JsonSerializer.Serialize(weekend))!;
        Assert.Equal(first.Classification, Finish(replay).Classification);
    }

    [Fact]
    public void Speed_and_cornering_strengths_reverse_their_advantage_on_contrasting_supplied_circuits()
    {
        double Difference(string circuit)
        {
            var preset = WeekendPresetCatalog.Open(circuit);
            var neutral = preset.Cars[0];
            var speed = neutral.Car with { Ratings = neutral.Car.Ratings.SetItem("topSpeed", 92).SetItem("accelerationDeployment", 92) };
            var corners = neutral.Car with { Ratings = neutral.Car.Ratings.SetItems(new[] { "highSpeedAero", "lowSpeedAero", "mechanicalGrip", "braking", "traction" }.Select(k => new KeyValuePair<string, double>(k, 92))) };
            var sources = System.Collections.Immutable.ImmutableArray.Create(
                neutral with { EntryId = "speed", Car = speed }, neutral with { EntryId = "corners", Car = corners });
            var weekend = WeekendPreparation.Prepare(preset with { Cars = sources }, 3);
            weekend = weekend with { Configuration = weekend.Configuration with { Tyres = new(DegradationPerLap: 0, WarmupPenalty: 0), Traffic = new(Enabled: false) } };
            var result = Finish(weekend);
            return result.Classification.Single(c => c.DriverId == "speed").FinishTime!.Value
                - result.Classification.Single(c => c.DriverId == "corners").FinishTime!.Value;
        }
        Assert.True(Difference("track-monza") < 0);
        Assert.True(Difference("track-monaco") > 0);
    }

    [Fact]
    public void Unknown_content_versions_are_rejected_instead_of_silently_changing_replay_semantics()
    {
        var source = WeekendPresetCatalog.Open("track-monza");
        var unsupported = source with { Circuit = source.Circuit with { Version = "future-version" } };
        Assert.Throws<ArgumentException>(() => WeekendPreparation.Prepare(unsupported, 3));
    }

    [Fact]
    public void Subdividing_representative_segments_does_not_multiply_effective_car_gains()
    {
        var source = WeekendPresetCatalog.Open("track-monza");
        var stronger = source.Cars.Select(c => c with { Car = c.Car with { Ratings = c.Car.Ratings.SetItem("topSpeed", 92) } }).ToImmutableArray();
        var weekend = WeekendPreparation.Prepare(source with { Cars = stronger }, 3);
        var split = weekend.Configuration.Track.Segments!.SelectMany(s => new[] {
            s with { Id = s.Id + "-a", Length = s.Length / 2, BaselineTime = s.BaselineTime / 2 },
            s with { Id = s.Id + "-b", Length = s.Length / 2, BaselineTime = s.BaselineTime / 2 } }).ToArray();
        var configuration = weekend.Configuration with { Tyres = new(DegradationPerLap: 0, WarmupPenalty: 0), Traffic = new(Enabled: false) };
        weekend = weekend with { Configuration = configuration };
        var first = Finish(weekend);
        var second = Finish(weekend with { Configuration = configuration with { Track = configuration.Track with { Segments = split } } });
        Assert.Equal(first.Classification[0].FinishTime!.Value, second.Classification[0].FinishTime!.Value, 8);
    }

    [Fact]
    public void Future_effective_replay_model_is_rejected_at_the_public_race_boundary()
    {
        var weekend = WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"), 3);
        Assert.Throws<ArgumentException>(() => WeekendPreparation.CreateRace(weekend with { ModelVersion = "future" }, 17));
    }

    private static PhaseResult Finish(EffectiveWeekend weekend)
    {
        var race = WeekendPreparation.CreateRace(weekend, 17);
        PhaseResult result;
        do { result = race.ResolvePhase(); } while (!result.Finished);
        return result;
    }
}




