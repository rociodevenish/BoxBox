using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class ComparisonTests
{
    [Fact]
    public void Replay_metadata_is_independent_of_later_caller_edits()
    {
        var seeds = new[] { 1 };
        var drivers = new[] { new Driver("a", "A") };
        var scheduled = new[] { new ScheduledDecision(0, "a", new(PitCompound: TyreCompound.Hard)) };
        var report = StrategyComparison.Compare(new ComparisonRequest(new RaceConfiguration(5, new Track("Test")),
            drivers, "a", new(scheduled, []), new(scheduled, []), seeds));
        seeds[0] = 99;
        drivers[0] = new Driver("changed", "B");
        scheduled[0] = new ScheduledDecision(0, "a", new(PaceInstruction.Push));
        Assert.Equal(1, report.Replay.Seeds[0]);
        Assert.Equal("a", report.Replay.Drivers[0].Id);
        Assert.Equal(TyreCompound.Hard, report.Replay.Baseline.Scheduled[0].Decision.PitCompound);
    }
    [Fact]
    public void Unequal_distances_do_not_enter_elapsed_time_statistics()
    {
        var config = new RaceConfiguration(12, new Track("Test", 100),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0, PushGain: 50), Traffic: new(Enabled: false));
        var baseline = new StrategyPlan([new ScheduledDecision(0, "slow", new(PitCompound: TyreCompound.Hard))], []);
        var alternative = new StrategyPlan([new ScheduledDecision(0, "slow", new(PitCompound: TyreCompound.Hard))], [], PaceInstruction.Push);
        var report = StrategyComparison.Compare(new ComparisonRequest(config,
            [new Driver("leader", "A"), new Driver("slow", "B", Pace: -100)], "slow", baseline, alternative, [1]));
        Assert.NotEqual(report.Pairs[0].BaselineLaps, report.Pairs[0].AlternativeLaps);
        Assert.Null(report.Pairs[0].ComparableTimeDifference);
        Assert.Equal(0, report.EligibleEqualDistanceTimeDifference.Count);
    }

    [Fact]
    public void Ineligible_results_remain_diagnostic_and_opponent_changes_are_rejected()
    {
        var request = new ComparisonRequest(new RaceConfiguration(5, new Track("Test")),
            [new Driver("a", "A"), new Driver("b", "B", Gap: 10)], "a", new([], []), new([], []), [1, 2]);
        var report = StrategyComparison.Compare(request);
        Assert.Equal(0, report.EligiblePositionDifference.Count);
        Assert.Equal(2, report.DiagnosticPositionDifference.Count);
        Assert.Throws<ArgumentException>(() => StrategyComparison.Compare(request with
            { Alternative = new([new ScheduledDecision(0, "b", new())], []) }));
    }
    [Fact]
    public void Comparison_changes_only_target_and_preserves_paired_seeds()
    {
        var baseline = new StrategyPlan([new ScheduledDecision(1, "a", new(PitCompound: TyreCompound.Hard)),
            new ScheduledDecision(1, "b", new(PitCompound: TyreCompound.Hard))], []);
        var alternative = new StrategyPlan([new ScheduledDecision(1, "a", new(PitCompound: TyreCompound.Hard))], [],
            PaceInstruction.Push);
        var report = StrategyComparison.Compare(new ComparisonRequest(new RaceConfiguration(10, new Track("Test")),
            [new Driver("a", "A"), new Driver("b", "B", Gap: 10)], "a", baseline, alternative, [7, 8]));
        Assert.Equal(new[] { 7, 8 }, report.Pairs.Select(p => p.Seed));
        Assert.All(report.Pairs, p => Assert.All(p.Alternative.Result.Decisions.Where(d => d.DriverId == "b"),
            d => Assert.Equal(PaceInstruction.Standard, d.Decision.Pace)));
        Assert.Equal(2, report.EligiblePositionDifference.Count);
    }
}
