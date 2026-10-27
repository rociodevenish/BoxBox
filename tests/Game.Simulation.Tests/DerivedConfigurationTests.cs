using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class DerivedConfigurationTests
{
    [Fact]
    public void Later_segment_overflow_is_rejected_before_a_race_can_mutate()
    {
        var configuration = new RaceConfiguration(2, new("Track", Segments:
            [new("initial", 500, 50, 0, 0), new("later", 500, 50, 1, 0)]), AttributeSeconds: 2);
        Assert.Throws<ArgumentException>(() => new RaceEngine(configuration, [new("a", "A", Speed: -1e308)], 7));
    }
    [Fact]
    public void Finite_segment_demand_cannot_overflow_the_derived_wear()
    {
        var configuration = new RaceConfiguration(2, new("Track", Segments: [new("a", 1000, 100, TyreDemand: 1e308)]),
            Tyres: new(DegradationPerLap: 10));
        Assert.Throws<ArgumentException>(() => new RaceEngine(configuration, [new("a", "A")], 7));
    }
    [Theory]
    [InlineData(false, "compound")]
    [InlineData(true, "compound")]
    [InlineData(false, "pace")]
    [InlineData(true, "pace")]
    [InlineData(false, "variability")]
    [InlineData(true, "variability")]
    public void Supported_future_controls_are_validated_before_accepting_configuration(bool segmented, string control)
    {
        var tyres = control switch {
            "compound" => new TyreTuning(WarmupPenalty: 1e308, HardWarmup: 2),
            "pace" => new TyreTuning(DegradationPerLap: 1e308, PushWear: 2),
            _ => new TyreTuning(DegradationPenalty: 1e308) };
        var configuration = new RaceConfiguration(2, new("Track", Segments: segmented ? [new("a", 1000, 100)] : null),
            Tyres: tyres, RaceFormVariation: control == "variability" ? 1e308 : 0,
            LapVariation: control == "variability" ? 1e308 : 0);
        Assert.Throws<ArgumentException>(() => new RaceEngine(configuration, [new("a", "A")], 7));
    }
}
