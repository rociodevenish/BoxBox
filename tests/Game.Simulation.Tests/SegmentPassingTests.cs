using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class SegmentPassingTests
{
    [Fact]
    public void Restricted_queue_passes_when_it_reaches_an_eligible_zone()
    {
        var race = new RaceEngine(new(2, new("Track", PassingChance: 1, Segments:
            [new("restricted", 500, 50, 0, 0, PassingAllowed: false),
             new("zone", 500, 50, 0, 0, PassingDifficulty: 0)]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new("leader", "A"), new("follower", "B", Pace: 20, Gap: 2)], 1);
        var result = race.ResolvePhase();
        var pass = Assert.Single(result.Events, e => e.Kind == RaceEventKind.Overtake);
        Assert.Equal("zone", pass.SegmentId);
        Assert.Equal(50, pass.Time, 8);
        Assert.Equal("follower", result.Classification[0].DriverId);
    }
    [Fact]
    public void Failed_pass_is_reused_within_visit_but_next_lap_is_a_new_opportunity()
    {
        var race = new RaceEngine(new(6, new("Track", PassingChance: 0, Segments:
            [new("a", 500, 50, 0, 0, PassingDifficulty: 0, PassingZoneId: "zone"),
             new("b", 500, 50, 0, 0, PassingDifficulty: 0, PassingZoneId: "zone")]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new("leader", "A"), new("follower", "B", Pace: 20, Gap: 2)], 1);
        var first = race.ResolvePhase();
        var last = race.ResolvePhase();
        var attempts = last.Events.Where(e => e.Kind == RaceEventKind.PassingAttempt).ToArray();
        Assert.Equal(6, attempts.Length);
        Assert.Equal(6, attempts.Select(e => e.EncounterId).Distinct().Count());
        Assert.All(attempts, e => Assert.Equal(0, e.Value));
        Assert.DoesNotContain(last.Events, e => e.Kind == RaceEventKind.Overtake);
        Assert.Equal(6, first.Events.Count(e => e.Kind == RaceEventKind.PassingAttempt));
    }
    [Fact]
    public void Eligible_entry_draw_is_cached_but_overtake_waits_for_physical_catch()
    {
        var result = new RaceEngine(new(1, new("Track", PassingChance: 1,
            Segments: [new("zone", 1000, 100, 0, 0, PassingDifficulty: 0)]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new("leader", "A"), new("follower", "B", Pace: 20, Gap: 2)], 1).ResolvePhase();
        var attempt = Assert.Single(result.Events, e => e.Kind == RaceEventKind.PassingAttempt);
        var pass = Assert.Single(result.Events, e => e.Kind == RaceEventKind.Overtake);
        Assert.Equal(2, attempt.Time, 8);
        Assert.Equal(10, pass.Time, 8);
        Assert.Equal(attempt.EncounterId, pass.EncounterId);
        Assert.Equal(0, attempt.Value);
    }

    [Fact]
    public void Equivalent_subdivision_preserves_passing_draws_and_outcomes()
    {
        PhaseResult Run(IReadOnlyList<TrackSegment> segments) => new RaceEngine(new(3,
            new("Track", PassingChance: .4, Segments: segments), Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new("leader", "A"), new("follower", "B", Pace: 20, Gap: 2)], 42).ResolvePhase();
        var whole = Run([new("whole", 1000, 100, 0, 0, PassingZoneId: "zone")]);
        var split = Run([new("a", 300, 30, 0, 0, PassingZoneId: "zone"), new("b", 700, 70, 0, 0, PassingZoneId: "zone")]);
        Assert.Equal(whole.Classification.Select(c => (c.DriverId, c.Laps)), split.Classification.Select(c => (c.DriverId, c.Laps)));
        Assert.Equal(whole.Events.Where(e => e.Kind is RaceEventKind.PassingAttempt or RaceEventKind.Overtake)
            .Select(e => (e.Kind, e.EncounterId, Math.Round(e.Time, 6))),
            split.Events.Where(e => e.Kind is RaceEventKind.PassingAttempt or RaceEventKind.Overtake)
            .Select(e => (e.Kind, e.EncounterId, Math.Round(e.Time, 6))));
    }

    [Fact]
    public void Lapping_uses_explicit_blue_flags_even_in_a_restricted_segment()
    {
        var result = new RaceEngine(new(4, new("Track", Segments:
            [new("restricted", 1000, 100, 0, 0, PassingAllowed: false)]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new("fast", "A", Pace: 50), new("slow", "B", Pace: -10)], 1).ResolvePhase();
        Assert.Contains(result.Events, e => e.Kind == RaceEventKind.BlueFlag && e.OtherDriverId == "slow"
            && e.EncounterId is not null);
        Assert.DoesNotContain(result.Events, e => e.Kind is RaceEventKind.Overtake or RaceEventKind.PassingAttempt or RaceEventKind.BattleWear);
    }
    [Fact]
    public void Battle_wear_is_charged_only_for_actual_eligible_opportunities()
    {
        PhaseResult Run(bool eligible) => new RaceEngine(new(1, new("Track", PassingChance: 0,
            Segments: [new("zone", 1000, 100, 0, 0, PassingAllowed: eligible)]),
            Tyres: new(DegradationPerLap: .01, WarmupPenalty: 0)),
            [new("leader", "A"), new("follower", "B", Pace: 20, Gap: 2)], 1)
            .ResolvePhase(new Dictionary<string, DriverDecision>
            { ["leader"] = new(Battle: BattleInstruction.Defend), ["follower"] = new(Battle: BattleInstruction.Attack) });
        Assert.DoesNotContain(Run(false).Events, e => e.Kind == RaceEventKind.BattleWear);
        var eligible = Run(true);
        Assert.Equal(2, eligible.Events.Count(e => e.Kind == RaceEventKind.BattleWear));
        Assert.Single(eligible.Events, e => e.Kind == RaceEventKind.PassingAttempt);
    }

    [Fact]
    public void Defend_changes_eligible_outcome_without_creating_extra_opportunities()
    {
        PhaseResult Run(BattleInstruction battle) => new RaceEngine(new(1, new("Track", PassingChance: 1,
            Segments: [new("zone", 1000, 100, 0, 0, PassingDifficulty: 0)]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0), Traffic: new(DefendMultiplier: 0)),
            [new("leader", "A"), new("follower", "B", Pace: 20, Gap: 2)], 1)
            .ResolvePhase(new Dictionary<string, DriverDecision> { ["leader"] = new(Battle: battle) });
        Assert.Single(Run(BattleInstruction.Neutral).Events, e => e.Kind == RaceEventKind.Overtake);
        var defended = Run(BattleInstruction.Defend);
        Assert.DoesNotContain(defended.Events, e => e.Kind == RaceEventKind.Overtake);
        Assert.Single(defended.Events, e => e.Kind == RaceEventKind.PassingAttempt);
    }

    [Fact]
    public void Wrapped_zone_retains_one_visit_identity_across_start_finish()
    {
        var result = new RaceEngine(new(2, new("Track", PassingChance: 0, Segments:
            [new("prefix", 200, 20, 0, 0, PassingZoneId: "wrap"),
             new("restricted", 600, 60, 0, 0, PassingAllowed: false),
             new("suffix", 200, 20, 0, 0, PassingZoneId: "wrap")]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new("leader", "A"), new("follower", "B", Pace: 20, Gap: 2)], 1).ResolvePhase();
        var attempts = result.Events.Where(e => e.Kind == RaceEventKind.PassingAttempt).ToArray();
        Assert.Equal(3, attempts.Length);
        Assert.Equal(new[] { 2d, 80d, 180d }, attempts.Select(e => e.Time));
        Assert.Equal(3, attempts.Select(e => e.EncounterId).Distinct().Count());
    }
    [Fact]
    public void Multiple_nearby_opponents_keep_distinct_replayable_opportunities()
    {
        PhaseResult Run() => new RaceEngine(new(2, new("Track", PassingChance: 1, Segments:
            [new("restricted", 500, 50, 0, 0, PassingAllowed: false),
             new("zone", 500, 50, 0, 0, PassingDifficulty: 0)]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new("z", "A"), new("y", "B", Pace: 10, Gap: 2), new("x", "C", Pace: 20, Gap: 4)], 3).ResolvePhase();
        var result = Run();
        Assert.Equal(new[] { "x", "y", "z" }, result.Classification.Select(c => c.DriverId));
        var passes = result.Events.Where(e => e.Kind == RaceEventKind.Overtake && e.DriverId == "x").ToArray();
        Assert.Equal(new[] { "y", "z" }, passes.Select(e => e.OtherDriverId).Order());
        Assert.Equal(passes.Length, passes.Select(e => e.EncounterId).Distinct().Count());
        Assert.Equal(result.Events, Run().Events);
    }

    [Fact]
    public void Shared_zone_rejects_disjoint_or_non_equivalent_subdivisions()
    {
        void Reject(IReadOnlyList<TrackSegment> segments) => Assert.Throws<ArgumentException>(() =>
            new RaceEngine(new(1, new("Track", Segments: segments)), [new("a", "A")], 1));
        Reject([new("a", 100, 10, PassingZoneId: "zone"), new("b", 100, 10),
            new("c", 100, 10, PassingZoneId: "zone"), new("d", 100, 10)]);
        Reject([new("a", 100, 10, PassingZoneId: "zone"), new("b", 100, 20, PassingZoneId: "zone")]);
        Reject([new("a", 100, 10, PassingZoneId: "zone"), new("b", 100, 10, PassingDifficulty: .9, PassingZoneId: "zone")]);
    }
}

