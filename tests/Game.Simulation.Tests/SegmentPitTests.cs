using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class SegmentPitTests
{
    private static RaceConfiguration Config(int laps = 12) => new(laps,
        new("Track", 100, PitLaneLoss: 20, StationaryStop: 3, Segments:
            [new("a", 400, 40, 0, 0, PassingAllowed: false), new("b", 600, 60, 0, 0, PassingAllowed: false)]),
        Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0, HardLoss: 0), StopVariation: 0);

    [Fact]
    public void Next_lap_end_stop_rejoins_at_start_finish_and_preserves_push()
    {
        var race = new RaceEngine(Config(), [new("a", "A")], 1);
        race.ResolvePhase();
        var phase = race.ResolvePhase(new Dictionary<string, DriverDecision> { ["a"] = new(PaceInstruction.Push, TyreCompound.Hard) });
        var entry = Assert.Single(phase.Events, e => e.Kind == RaceEventKind.PitEntry);
        var exit = Assert.Single(phase.Events, e => e.Kind == RaceEventKind.PitExit);
        Assert.Equal(6, entry.Lap);
        Assert.Equal(599, entry.Time, 8);
        Assert.Equal(622, exit.Time, 8);
        Assert.Equal(6000, exit.DistanceMetres);
        Assert.Equal("a", exit.SegmentId);
        Assert.Equal(1018, phase.Time, 8);
        Assert.Equal(PaceInstruction.Push, phase.Tyres[0].Pace);
        Assert.True(phase.Eligibility["a"]);
    }
    [Fact]
    public void Pit_exit_rejoins_spatial_defender_and_can_lose_the_clear_air_gain()
    {
        PhaseResult Run(bool traffic)
        {
            var race = new RaceEngine(Config(8) with
            {
                Track = Config().Track with { PitLaneLoss = 37 },
                Tyres = new(DegradationPerLap: 0, WarmupPenalty: 0, HardLoss: 0, PushGain: 20),
                Traffic = new(Enabled: traffic)
            }, [new("a", "A"), new("b", "B", Gap: 5)], 1);
            race.ResolvePhase();
            return race.ResolvePhase(new Dictionary<string, DriverDecision> { ["a"] = new(PaceInstruction.Push, TyreCompound.Hard) });
        }
        var clear = Run(false);
        Assert.Equal("a", clear.Classification[0].DriverId);
        var traffic = Run(true);
        Assert.Equal("b", traffic.Classification[0].DriverId);
        Assert.InRange(traffic.Classification.Single(c => c.DriverId == "a").FinishTime!.Value - 805, .2, .4);
        Assert.Contains(traffic.Events, e => e.DriverId == "a" && e.Kind == RaceEventKind.TrafficLoss && e.Time > 620);
    }
    [Fact]
    public void Pit_exit_at_an_existing_cars_crossing_rejoins_behind_that_car()
    {
        var race = new RaceEngine(Config(2), [new("a", "A", Pace: 10), new("b", "B", Gap: 13)], 1);
        var result = race.ResolvePhase(new Dictionary<string, DriverDecision> { ["a"] = new(PitCompound: TyreCompound.Hard) });
        Assert.Equal("b", result.Classification[0].DriverId);
        Assert.InRange(result.Classification[1].FinishTime!.Value - result.Classification[0].FinishTime!.Value, .2, .4);
        var exit = Assert.Single(result.Events, e => e.Kind == RaceEventKind.PitExit);
        Assert.Equal(113, exit.Time, 8);
        Assert.Equal(1000, exit.DistanceMetres);
    }
    [Fact]
    public void Simultaneous_segment_exits_apply_both_new_trajectories_before_entry_opportunity()
    {
        var result = new RaceEngine(new(2, new("Track", PassingChance: 1, Segments:
            [new("corner", 500, 50, 0, 1, PassingAllowed: false),
             new("straight", 500, 50, 1, 0, PassingDifficulty: 0)]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new("z", "A", Speed: 30), new("y", "B", Speed: 20, Cornering: 20, Gap: 2)], 1).ResolvePhase();
        Assert.Equal("z", result.Classification[0].DriverId);
        Assert.DoesNotContain(result.Events, e => e.Kind is RaceEventKind.PassingAttempt or RaceEventKind.Overtake);
    }
    [Fact]
    public void Boundary_drains_simultaneous_crossings_before_accepting_next_lap_stop()
    {
        var race = new RaceEngine(Config(), [new("a", "A"), new("b", "B")], 1);
        var first = race.ResolvePhase();
        Assert.All(first.Classification, c => Assert.Equal(5, c.Laps));
        Assert.Equal(2, first.Events.Count(e => e.Kind == RaceEventKind.LapCompleted && e.Time == 500));
        var next = race.ResolvePhase(new Dictionary<string, DriverDecision> { ["b"] = new(PitCompound: TyreCompound.Hard) });
        var entry = Assert.Single(next.Events, e => e.Kind == RaceEventKind.PitEntry);
        Assert.Equal(6, entry.Lap);
        Assert.Equal(600, entry.Time, 8);
    }

    [Fact]
    public void Complete_mixed_22_car_race_laps_stops_and_finishes_reproducibly()
    {
        PhaseResult Run()
        {
            var config = Config() with { Track = Config().Track with
            { PassingChance = 1, Segments =
                [new("straight", 400, 40, 1, 0, PassingDifficulty: 0), new("corner", 600, 60, 0, 1, PassingDifficulty: 0)] } };
            var drivers = Enumerable.Range(0, 22).Select(i => new Driver($"d{i:00}", $"t{i / 2}",
                Speed: i % 3 + 1, Cornering: i % 4 + 1, Pace: i * 3 + 1, Gap: i * .2)).ToArray();
            var race = new RaceEngine(config, drivers, 17);
            var result = race.ResolvePhase(drivers.ToDictionary(d => d.Id, d => new DriverDecision(PitCompound: TyreCompound.Hard)));
            while (!result.Finished) result = race.ResolvePhase();
            return result;
        }
        var result = Run();
        Assert.Equal(22, result.Classification.Count);
        Assert.Equal(12, result.LeaderLap);
        Assert.Contains(result.Classification, c => c.Laps < 12);
        Assert.All(result.Classification, c => Assert.NotNull(c.FinishTime));
        Assert.All(result.Eligibility.Values, Assert.True);
        Assert.Equal(22, result.Events.Count(e => e.Kind == RaceEventKind.PitEntry));
        Assert.Equal(22, result.Events.Count(e => e.Kind == RaceEventKind.PitExit));
        Assert.Contains(result.Events, e => e.Kind == RaceEventKind.BlueFlag);
        Assert.True(result.Events.Zip(result.Events.Skip(1)).All(p => p.First.Time <= p.Second.Time + 1e-8));
        var replay = Run();
        Assert.Equal(result.Events, replay.Events);
        Assert.Equal(result.Classification, replay.Classification);
    }
    [Fact]
    public void Boundary_pit_exit_rejoins_behind_a_lapping_car_at_the_same_coordinate()
    {
        var race = new RaceEngine(Config() with { Track = Config().Track with { PitLaneLoss = 407 } },
            [new("a", "A", Pace: 10), new("z", "B")], 1);
        var first = race.ResolvePhase(new Dictionary<string, DriverDecision> { ["a"] = new(PitCompound: TyreCompound.Hard) });
        Assert.Equal(500, first.Time, 8);
        Assert.Contains(first.Events, e => e.Kind == RaceEventKind.PitExit && Math.Abs(e.Time - 500) < 1e-8);
        Assert.False(first.Tyres.Single(t => t.DriverId == "a").InPits);
        var next = race.ResolvePhase();
        var rejoined = next.Classification.Single(c => c.DriverId == "a");
        Assert.Equal(6, rejoined.Laps);
        Assert.Equal(0, rejoined.Progress, 8);
        Assert.Equal(4, rejoined.GapLaps);
    }
    [Fact]
    public void Coincident_chequered_crossings_finish_lapped_car_independently_of_id()
    {
        var race = new RaceEngine(Config() with { Traffic = new(Enabled: false) },
            [new("z", "A"), new("a", "B", Pace: -100)], 1);
        PhaseResult result;
        do { result = race.ResolvePhase(); } while (!result.Finished);
        Assert.Equal(12, result.Classification[0].Laps);
        Assert.Equal(6, result.Classification[1].Laps);
        Assert.All(result.Classification, c => Assert.Equal(1200, c.FinishTime!.Value, 8));
        Assert.All(result.Eligibility.Values, eligible => Assert.False(eligible));
    }

    [Fact]
    public void Invalid_phase_decision_does_not_mutate_segmented_state()
    {
        var race = new RaceEngine(Config(), [new("a", "A")], 1);
        Assert.Throws<ArgumentException>(() => race.ResolvePhase(new Dictionary<string, DriverDecision>
            { ["a"] = new(PitCompound: (TyreCompound)99) }));
        var state = race.GetState();
        Assert.Equal(0, state.Time);
        Assert.Empty(state.Events);
        Assert.Equal(1, state.Tyres[0].Condition);
        Assert.Equal(5, race.ResolvePhase().LeaderLap);
    }
}
