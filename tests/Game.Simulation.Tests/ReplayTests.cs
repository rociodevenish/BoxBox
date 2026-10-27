using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class ReplayTests
{
    [Theory]
    [InlineData(false, "attacker/opponent/lap")]
    [InlineData(true, "attacker/defender/zone visit")]
    public void Passing_metadata_describes_the_selected_models_actual_random_scope(bool segmented, string expected)
    {
        var configuration = new RaceConfiguration(2, new("Track", Segments: segmented ? [new("a", 1000, 90)] : null));
        var channel = Assert.Single(SimulationReplay.Metadata(configuration).Channels, c => c.Name == "passing");
        Assert.Equal(expected, channel.Scope);
    }
    [Fact]
    public void Segmented_run_publishes_self_contained_replay_with_exact_outcomes()
    {
        var segments = new TrackSegment[] { new("a", 400, 40), new("b", 600, 60) };
        var drivers = new Driver[] { new("a", "A") };
        var scheduled = new ScheduledDecision[] { new(0, "a", new(PitCompound: TyreCompound.Hard)) };
        var run = StrategyComparison.Run(new(7, new("Track", Segments: segments), RaceFormVariation: 2, LapVariation: .4),
            drivers, new(scheduled, []), 7);
        segments[0] = new("changed", 1, 1);
        drivers[0] = new("changed", "B");
        scheduled[0] = new(0, "a", new(PaceInstruction.Push));
        Assert.Equal(SimulationVersions.SegmentedModel, run.Replay!.ModelVersion);
        Assert.Equal(SimulationVersions.SegmentedRandomness, run.Replay.RandomnessVersion);
        Assert.NotNull(run.Replay.Configuration.Tyres);
        Assert.NotNull(run.Replay.Configuration.Traffic);
        Assert.Equal("a", run.Replay.Configuration.Track.Segments![0].Id);
        var replay = StrategyComparison.Replay(run.Replay);
        Assert.Equal(run.Result.Events, replay.Result.Events);
        Assert.Equal(run.Result.Classification, replay.Result.Classification);
    }
    [Fact]
    public void Incompatible_replay_contracts_are_rejected_before_resolution()
    {
        var run = StrategyComparison.Run(new(2, new("Track", Segments: [new("a", 1000, 90)])),
            [new("a", "A")], new([], []), 7);
        Assert.Throws<ArgumentException>(() => StrategyComparison.Replay(run.Replay! with { ModelVersion = "future-model" }));
        Assert.Throws<ArgumentException>(() => StrategyComparison.Replay(run.Replay! with { RandomnessVersion = "future-randomness" }));
        Assert.Throws<ArgumentException>(() => StrategyComparison.Replay(run.Replay! with { ModelVersion = SimulationVersions.AggregateModel }));
    }

    [Fact]
    public void Designer_diagnostics_record_resolved_channel_values_once_per_scope()
    {
        var run = StrategyComparison.Run(new(7, new("Track", Segments: [new("a", 1000, 90)]),
            RaceFormVariation: 2, LapVariation: .4), [new("a", "A")],
            new([new(0, "a", new(PitCompound: TyreCompound.Hard))], []), 7);
        Assert.Equal(5, run.Metadata!.Channels.Count);
        Assert.Single(run.Diagnostics!, d => d.Channel == "race-form");
        Assert.Equal(7, run.Diagnostics!.Count(d => d.Channel == "lap-variation"));
        Assert.Single(run.Diagnostics!, d => d.Channel == "pit-service" && d.Scope == "stop:1");
        Assert.Equal(run.Diagnostics!.Count, run.Diagnostics.Select(d => (d.Channel, d.DriverId, d.Scope)).Distinct().Count());
        var replay = StrategyComparison.Replay(run.Replay!);
        Assert.Equal(run.Diagnostics, replay.Diagnostics);
        Assert.Equal(run.Result.Decisions, replay.Result.Decisions);
    }

    [Fact]
    public void Comparison_snapshot_retains_layout_versions_and_separate_outcome_groups()
    {
        var segments = new TrackSegment[] { new("a", 1000, 100) };
        var report = StrategyComparison.Compare(new(new(7, new("Track", Segments: segments)),
            [new("a", "A")], "a", new([], []), new([], []), [7, 8]));
        segments[0] = new("changed", 1, 1);
        Assert.Equal("a", report.Replay.Configuration.Track.Segments![0].Id);
        Assert.Equal(SimulationVersions.SegmentedModel, report.Replay.ModelVersion);
        Assert.Equal(SimulationVersions.SegmentedRandomness, report.Metadata!.RandomnessVersion);
        Assert.Equal(0, report.EligibleEqualDistanceTimeDifference.Count);
        var group = Assert.Single(report.Groups);
        Assert.Equal(2, group.PositionDifference.Count);
        Assert.Equal(7, group.BaselineLaps);
        Assert.False(group.BaselineEligible);
        Assert.Equal(0, group.EqualDistanceTimeDifference.Mean);
        Assert.Throws<ArgumentException>(() => StrategyComparison.Compare(report.Replay with { RandomnessVersion = "future" }));
    }
}
