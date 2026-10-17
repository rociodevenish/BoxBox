using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;

public class InterruptionTests
{
    private static RaceEngine Race(params ForcedIncident[] events) => new(
        new(20, new("Lab", Segments: [new("first", 500, 45), new("last", 500, 45)]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0), Traffic: new(Enabled: false),
            StopVariation: 0, Incidents: new(ComponentWearPerSecond: 0, InterruptMajorIncidents: true)),
        [new("a", "A"), new("b", "B", Gap: 9)], 9, new(events));

    [Fact]
    public void Major_incident_stops_shared_clock_mid_segment_without_resolving_future_laps()
    {
        var race = Race(new ForcedIncident("a", IncidentKind.Spin, 36));
        var result = race.ResolvePhase();
        Assert.Equal(36, result.Time, 8);
        Assert.Equal(.4, result.Classification.Single(c => c.DriverId == "a").Progress, 8);
        Assert.Equal(.3, result.Classification.Single(c => c.DriverId == "b").Progress, 8);
        Assert.Single(result.Events, e => e.Kind == RaceEventKind.Spin);
        Assert.DoesNotContain(result.Events, e => e.Time > 36);
        Assert.Equal(PhaseCompletionReason.SafetyCar, result.CompletionReason);
        Assert.Equal(36, result.Interruption!.Time);
        Assert.Equal(5, result.NormalTargetDistance);
    }

    [Fact]
    public void Fresh_normal_window_targets_five_laps_from_fractional_active_leader()
    {
        var race = Race(new ForcedIncident("a", IncidentKind.Spin, 306));
        Assert.Equal(306, race.ResolvePhase().Time);
        var resumed = race.ResolvePhase();
        var leader = resumed.Classification.First();
        Assert.Equal(8.4, resumed.NormalTargetDistance!.Value, 8);
        Assert.Equal(8.4, leader.Laps + leader.Progress, 8);
        Assert.Equal(PhaseCompletionReason.NormalBoundary, resumed.CompletionReason);
        Assert.Null(resumed.Interruption);
        Assert.Single(resumed.Events, e => e.Kind == RaceEventKind.Spin);
    }

    [Theory]
    [InlineData(36, false)]
    [InlineData(99, true)]
    [InlineData(100, true)]
    public void Queued_and_ongoing_stops_remain_single_committed_stops(double trigger, bool entered)
    {
        var race = Race(new ForcedIncident("a", IncidentKind.Spin, trigger));
        var interrupted = race.ResolvePhase(new Dictionary<string, DriverDecision> { ["b"] = new(PitCompound: TyreCompound.Hard) });
        Assert.Equal(entered, interrupted.Tyres.Single(c => c.DriverId == "b").InPits);
        Assert.Throws<ArgumentException>(() => race.ResolvePhase(new Dictionary<string, DriverDecision> { ["b"] = new(PitCompound: TyreCompound.Soft) }));
        var resumed = race.ResolvePhase();
        Assert.Single(resumed.Events, e => e.DriverId == "b" && e.Kind == RaceEventKind.PitService);
        Assert.Single(resumed.Events, e => e.DriverId == "b" && e.Kind == RaceEventKind.TyreChanged);
        Assert.Equal(TyreCompound.Hard, resumed.Tyres.Single(c => c.DriverId == "b").Compound);
    }

    [Fact]
    public void Simultaneous_lap_pit_and_incidents_are_drained_once_with_stable_identity()
    {
        var race = Race(new("a", IncidentKind.Spin, 99), new("b", IncidentKind.Spin, 99));
        var result = race.ResolvePhase(new Dictionary<string, DriverDecision> { ["b"] = new(PitCompound: TyreCompound.Hard) });
        Assert.Equal(99, result.Time);
        Assert.Equal(2, result.Events.Count(e => e.Kind == RaceEventKind.Spin));
        Assert.Single(result.Events, e => e.DriverId == "b" && e.Kind == RaceEventKind.LapCompleted);
        Assert.Single(result.Events, e => e.DriverId == "b" && e.Kind == RaceEventKind.PitService);
        Assert.Equal("local-incidents-v1:a:forced:0", result.Interruption!.Identity);
        Assert.Equal(result.Interruption, race.GetState().Interruption);
    }

    [Fact]
    public void Minor_errors_continue_and_retired_leader_cannot_satisfy_later_target()
    {
        var race = Race(new("a", IncidentKind.LockUp, 36), new("a", IncidentKind.MechanicalFailure, 100));
        var interrupted = race.ResolvePhase();
        Assert.Equal(100, interrupted.Time);
        Assert.Single(interrupted.Events, e => e.Kind == RaceEventKind.LockUp);
        var retired = interrupted.Classification.Single(c => c.DriverId == "a");
        Assert.NotNull(retired.RetirementTime);
        var resumed = race.ResolvePhase();
        var active = resumed.Classification.Single(c => c.DriverId == "b");
        Assert.Equal(6 + 1d / 90, resumed.NormalTargetDistance!.Value, 8);
        Assert.Equal(resumed.NormalTargetDistance.Value, active.Laps + active.Progress, 8);
        Assert.Equal(retired.Laps, resumed.Classification.Single(c => c.DriverId == "a").Laps);
        Assert.Equal(retired.Progress, resumed.Classification.Single(c => c.DriverId == "a").Progress);
    }

    [Fact]
    public void Finish_wins_over_simultaneous_major_incident_and_all_retired_has_no_special_window()
    {
        var config = new RaceConfiguration(1, new("Lab"), Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0),
            Traffic: new(Enabled: false), Incidents: new(ComponentWearPerSecond: 0, InterruptMajorIncidents: true));
        var race = new RaceEngine(config, [new("a", "A"), new("b", "B", Gap: 9)], 9,
            new IncidentScenario([new("b", IncidentKind.Spin, 90)]));
        var finished = race.ResolvePhase();
        Assert.True(finished.Finished);
        Assert.Equal(PhaseCompletionReason.Finish, finished.CompletionReason);
        Assert.Null(finished.Interruption);
        Assert.Single(finished.Events, e => e.Kind == RaceEventKind.Spin);

        var retired = Race(new("a", IncidentKind.MechanicalFailure, 36), new("b", IncidentKind.MechanicalFailure, 36)).ResolvePhase();
        Assert.True(retired.Finished);
        Assert.Equal(PhaseCompletionReason.Finish, retired.CompletionReason);
        Assert.Null(retired.Interruption);
    }

    [Fact]
    public void Configured_normal_span_is_frozen_from_active_progress_and_bounded_by_finish()
    {
        var config = new RaceConfiguration(3, new("Lab"), Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0),
            Traffic: new(Enabled: false), Incidents: new(ComponentWearPerSecond: 0, InterruptMajorIncidents: true), NormalPhaseLaps: 1.5);
        var race = new RaceEngine(config, [new("a", "A")], 9);
        var first = race.ResolvePhase();
        Assert.Equal(1.5, first.NormalTargetDistance);
        Assert.Equal(135, first.Time, 8);
        var final = race.ResolvePhase();
        Assert.Equal(3, final.NormalTargetDistance);
        Assert.True(final.Finished);
    }

    [Fact]
    public void Major_incident_at_normal_lap_boundary_drains_crossing_and_partial_wear_is_elapsed_only()
    {
        var race = Race(new ForcedIncident("a", IncidentKind.Spin, 450));
        var boundary = race.ResolvePhase();
        Assert.Equal(450, boundary.Time);
        Assert.Equal(PhaseCompletionReason.SafetyCar, boundary.CompletionReason);
        Assert.Equal(5, boundary.Classification.Single(c => c.DriverId == "a").Laps);
        Assert.Single(boundary.Events, e => e.DriverId == "a" && e.Kind == RaceEventKind.LapCompleted && e.Lap == 5);

        var wearRace = new RaceEngine(new(20, new("Lab"),
            Tyres: new(DegradationPerLap: .1, WarmupPenalty: 0, DegradationPenalty: 0), Traffic: new(Enabled: false),
            Incidents: new(ComponentWearPerSecond: 0, InterruptMajorIncidents: true)), [new("a", "A")], 9,
            new IncidentScenario([new("a", IncidentKind.Spin, 36)]));
        Assert.Equal(.88, Assert.Single(wearRace.ResolvePhase().Tyres).Condition, 8);
    }
}
