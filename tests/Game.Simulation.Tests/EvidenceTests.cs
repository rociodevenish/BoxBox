using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class EvidenceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Full_field_finishes_legally_on_both_profiles(bool mixed, bool technical)
    {
        var drivers = LaboratoryFixtures.Field(mixed);
        var plan = new StrategyPlan(drivers.Select(d => new ScheduledDecision(4, d.Id,
            new(PitCompound: TyreCompound.Hard))).ToArray(), []);
        var result = StrategyComparison.Run(new(40, LaboratoryFixtures.Track(technical), LapVariation: .2), drivers, plan, 7).Result;
        Assert.Equal(22, result.Classification.Count);
        Assert.Equal(22, result.Classification.Select(c => c.DriverId).Distinct().Count());
        Assert.All(result.Eligibility.Values, eligible => Assert.True(eligible));
        Assert.All(result.Classification, c => Assert.True(double.IsFinite(c.FinishTime!.Value)));
        Assert.Equal(40, result.LeaderLap);
    }

    [Fact]
    public void Track_profile_changes_early_stop_position_advantage_over_paired_seeds()
    {
        var drivers = LaboratoryFixtures.Field(true);
        var baseline = new StrategyPlan(drivers.Select(d => new ScheduledDecision(4, d.Id,
            new(PitCompound: TyreCompound.Hard))).ToArray(), []);
        var early = new StrategyPlan([new ScheduledDecision(3, "driver-06", new(PitCompound: TyreCompound.Hard))], []);
        double Mean(bool technical) => StrategyComparison.Compare(new ComparisonRequest(
            new(40, LaboratoryFixtures.Track(technical), LapVariation: .2), drivers, "driver-06", baseline, early,
            Enumerable.Range(7, 20).ToArray())).EligiblePositionDifference.Mean!.Value;
        Assert.True(Mean(false) >= 0);
        Assert.True(Mean(true) < 0);
    }
}
