using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class SegmentEvidenceTests
{
    [Fact]
    public void Straight_and_corner_strengths_have_equal_clear_air_lap_pace_but_distinct_local_gains()
    {
        var drivers = LaboratoryFixtures.SegmentedField(true).Take(2).Select(d => d with { Gap = 0 }).ToArray();
        var result = StrategyComparison.Run(new(2, LaboratoryFixtures.SegmentedTrack(false),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0), Traffic: new(Enabled: false)), drivers, new([], []), 7).Result;
        Assert.All(result.Classification, c => Assert.Equal(176, c.FinishTime!.Value, 8));
        var exits = result.Events.Where(e => e.Kind == RaceEventKind.SegmentExited && e.SegmentId == "straight-1" && e.Lap == 1).ToArray();
        Assert.Equal(21.5, exits.Single(e => e.DriverId == "driver-01").Time, 8);
        Assert.Equal(22.5, exits.Single(e => e.DriverId == "driver-02").Time, 8);
    }
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Both_segment_layouts_complete_legal_22_car_races(bool mixed, bool restricted)
    {
        var drivers = LaboratoryFixtures.SegmentedField(mixed);
        var plan = new StrategyPlan(drivers.Select(d => new ScheduledDecision(4, d.Id,
            new(PitCompound: TyreCompound.Hard))).ToArray(), []);
        var result = StrategyComparison.Run(new(40, LaboratoryFixtures.SegmentedTrack(restricted), LapVariation: .2), drivers, plan, 7).Result;
        Assert.Equal(22, result.Classification.Count);
        Assert.Equal(40, result.LeaderLap);
        Assert.All(result.Classification, car => Assert.True(double.IsFinite(car.FinishTime!.Value)));
        Assert.All(result.Eligibility.Values, eligible => Assert.True(eligible));
    }
}
