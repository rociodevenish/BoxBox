using System.Text.Json;
using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class PracticeAssessmentTests
{
    [Fact]
    public void Assessment_is_reproducible_two_driver_feedback_separate_from_diagnostics_and_leaves_race_inputs_unchanged()
    {
        var preset = WeekendPresetCatalog.Open("track-monza");
        var setup = new WeekendSetup(-2, 2, 2);
        var before = WeekendPreparation.Prepare(preset, 6, setup);
        var result = WeekendPreparation.Assess(preset, 6, setup, 73);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(WeekendPreparation.Assess(preset, 6, setup, 73)));
        Assert.Equal(2, result.Feedback.Drivers.Length);
        Assert.All(result.Feedback.Drivers, driver => Assert.False(string.IsNullOrWhiteSpace(driver.Uncertainty)));
        Assert.NotEmpty(result.Feedback.Compromises);
        Assert.DoesNotContain("raceDriver", JsonSerializer.Serialize(result.Feedback), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(WeekendPreparation.Prepare(preset, 6, setup)));
    }

    [Fact]
    public void Better_engineer_and_driver_information_improves_confidence_without_improving_the_car()
    {
        var source = WeekendPresetCatalog.Open("track-monza");
        WeekendPreset Information(double value) => source with
        {
            Engineer = source.Engineer with { Setup = value },
            Cars = source.Cars.Select(car => car with
            {
                Driver = car.Driver with { Ratings = car.Driver.Ratings.SetItem("setupFeedback", value) }
            }).ToImmutableArray()
        };
        var low = WeekendPreparation.Assess(Information(10), 6, new(0, 2, 0), 73);
        var high = WeekendPreparation.Assess(Information(95), 6, new(0, 2, 0), 73);
        Assert.All(low.Feedback.Drivers, driver => Assert.Equal("Low", driver.Confidence));
        Assert.All(high.Feedback.Drivers, driver => Assert.Equal("High", driver.Confidence));
        Assert.NotEqual(low.Feedback.Drivers[0].Uncertainty, high.Feedback.Drivers[0].Uncertainty);
        Assert.Equal(low.Diagnostics.Effective.Cars.Select(car => car.RaceDriver),
            high.Diagnostics.Effective.Cars.Select(car => car.RaceDriver));
        Assert.Equal(low.Diagnostics.ExactFits.ToArray(), high.Diagnostics.ExactFits.ToArray());
    }

    [Fact]
    public void Shared_stable_setup_can_please_one_driver_and_require_a_compromise_for_the_other()
    {
        var source = WeekendPresetCatalog.Open("track-monza");
        var preset = source with { Cars = source.Cars.Select((car, index) => car with
        {
            Driver = car.Driver with
            {
                Preferences = index == 0 ? [new("responsive-front", "Responsive front")] : [new("stable-rear", "Stable rear")],
                Ratings = car.Driver.Ratings.SetItem("adaptability", 10).SetItem("setupFeedback", 100)
            }
        }).ToImmutableArray(), Engineer = source.Engineer with { Setup = 100 } };
        var result = WeekendPreparation.Assess(preset, 6, new(0, 2, 0), 73);
        Assert.Equal("Preference mismatch", result.Feedback.Drivers[0].Fit);
        Assert.Equal("Comfortable", result.Feedback.Drivers[1].Fit);
        Assert.Equal(82, result.Diagnostics.Effective.Cars[0].Source.Car.Ratings["mechanicalGrip"]);
    }
}
