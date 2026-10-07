using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class RandomChannelTests
{
    private static RaceConfiguration Control(int laps = 10) => new(laps,
        new("Track", Segments: [new("a", 400, 40), new("b", 600, 60)]),
        Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0, HardLoss: 0), Traffic: new(Enabled: false), StopVariation: 0, EstimateError: 0);
    private static PhaseResult Run(RaceConfiguration configuration, Driver driver, int seed = 7)
    {
        var engine = new RaceEngine(configuration, [driver], seed);
        PhaseResult result;
        do { result = engine.ResolvePhase(); } while (!result.Finished);
        return result;
    }
    [Fact]
    public void Race_form_is_bounded_persistent_and_leaves_base_attributes_immutable()
    {
        var driver = new Driver("a", "A", Pace: 2, Speed: 2, Cornering: 2);
        var original = driver with { };
        var control = Run(Control(), driver);
        var form = Run(Control() with { RaceFormVariation = 4 }, driver);
        var controlLaps = control.Events.Where(e => e.Kind == RaceEventKind.LapCompleted).ToArray();
        var formLaps = form.Events.Where(e => e.Kind == RaceEventKind.LapCompleted).ToArray();
        var modifier = formLaps[0].Time - controlLaps[0].Time;
        Assert.InRange(modifier, -2, 2);
        Assert.NotEqual(0, modifier);
        for (var lap = 0; lap < formLaps.Length; lap++)
            Assert.Equal(modifier * (lap + 1), formLaps[lap].Time - controlLaps[lap].Time, 8);
        Assert.Equal(original, driver);
    }
    [Fact]
    public void Corresponding_first_stop_keeps_service_draw_when_strategy_changes_its_lap()
    {
        var configuration = Control(12) with { StopVariation = .6, RaceFormVariation = 2, LapVariation = 1 };
        var baseline = new StrategyPlan([new(0, "a", new(PitCompound: TyreCompound.Hard))], []);
        var alternative = new StrategyPlan([new(1, "a", new(PitCompound: TyreCompound.Hard))], []);
        var report = StrategyComparison.Compare(new(configuration, [new("a", "A")], "a", baseline, alternative, [7]));
        var pair = Assert.Single(report.Pairs);
        var first = Assert.Single(pair.Baseline.Result.Events, e => e.Kind == RaceEventKind.PitService);
        var second = Assert.Single(pair.Alternative.Result.Events, e => e.Kind == RaceEventKind.PitService);
        Assert.NotEqual(first.Lap, second.Lap);
        Assert.Equal(first.Value, second.Value);
        Assert.Equal(0, pair.ComparableTimeDifference!.Value, 8);
    }
    [Fact]
    public void Lap_noise_is_bounded_changes_by_lap_and_is_invariant_under_subdivision()
    {
        var config = Control() with { LapVariation = 2, RaceFormVariation = 0 };
        var whole = Run(config with { Track = config.Track with { Segments = [new("whole", 1000, 100)] } }, new("a", "A"));
        var split = Run(config, new("a", "A"));
        var laps = split.Events.Where(e => e.Kind == RaceEventKind.LapCompleted).Select(e => e.Time).ToArray();
        var durations = laps.Zip(new[] { 0d }.Concat(laps), (end, start) => end - start).ToArray();
        Assert.All(durations, duration => Assert.InRange(duration, 99, 101));
        Assert.True(durations.Distinct().Count() > 1);
        var wholeLaps = whole.Events.Where(e => e.Kind == RaceEventKind.LapCompleted).Select(e => e.Time).ToArray();
        for (var lap = 0; lap < laps.Length; lap++) Assert.Equal(wholeLaps[lap], laps[lap], 8);
    }

    [Theory]
    [InlineData(.4, false)]
    [InlineData(.6, true)]
    public void Zero_passing_variation_is_seed_independent_with_a_documented_half_threshold(double chance, bool passes)
    {
        PhaseResult RunPassing(int seed) => new RaceEngine(Control(2) with
        {
            Traffic = new(), PassingVariation = 0,
            Track = new("Track", PassingChance: chance, Segments: [new("zone", 1000, 100, 0, 0, PassingDifficulty: 0)])
        }, [new("leader", "A"), new("follower", "B", Pace: 20, Gap: 2)], seed).ResolvePhase();
        var first = RunPassing(1);
        Assert.Equal(passes, first.Events.Any(e => e.Kind == RaceEventKind.Overtake));
        Assert.Equal(first.Events, RunPassing(2).Events);
        Assert.Equal(first.Classification, RunPassing(2).Classification);
    }

    [Fact]
    public void Observation_error_is_phase_stable_bounded_clamped_and_excludes_hidden_inputs()
    {
        var race = new RaceEngine(Control() with { RaceFormVariation = 4, LapVariation = 2, EstimateError = .04,
            Tyres = new(DegradationPerLap: .05, WarmupPenalty: 0) }, [new("a", "A")], 7);
        var phase = race.ResolvePhase();
        var first = race.GetTeamObservation("A");
        var second = race.GetTeamObservation("A");
        Assert.Equal(first.TeamTyres, second.TeamTyres);
        var estimate = first.TeamTyres[0].EstimatedCondition;
        Assert.InRange(estimate, Math.Max(0, phase.Tyres[0].Condition - .04), Math.Min(1, phase.Tyres[0].Condition + .04));
        var json = System.Text.Json.JsonSerializer.Serialize(first);
        Assert.DoesNotContain("RaceForm", json);
        Assert.DoesNotContain("LapVariation", json);
        Assert.DoesNotContain("Seed", json);
        Assert.DoesNotContain("Opportunities", json);
        Assert.DoesNotContain("\"Condition\"", json);
        var exact = new RaceEngine(Control() with { EstimateError = 0 }, [new("a", "A")], 3);
        Assert.Equal(exact.GetState().Tyres[0].Condition, exact.GetTeamObservation("A").TeamTyres[0].EstimatedCondition);
    }

    [Theory]
    [InlineData(double.NaN, 1)]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    [InlineData(0, 1.1)]
    public void Invalid_new_channel_magnitudes_are_rejected(double form, double passing)
    {
        Assert.Throws<ArgumentException>(() => new RaceEngine(Control() with
            { RaceFormVariation = form, PassingVariation = passing }, [new("a", "A")], 1));
    }
    [Fact]
    public void Paired_strategy_changes_preserve_other_drivers_form_and_lap_noise_during_mid_lap_phase_changes()
    {
        var report = StrategyComparison.Compare(new(Control(12) with { RaceFormVariation = 4, LapVariation = 2,
            Tyres = new(DegradationPerLap: 0, WarmupPenalty: 0, PushGain: 20) },
            [new("leader", "A", Pace: 5), new("target", "B", Gap: 5)], "target",
            new([], []), new([], [], DefaultPace: PaceInstruction.Push), [7, 9]));
        foreach (var pair in report.Pairs)
        {
            var first = pair.Baseline.Result.Events.Where(e => e.DriverId == "leader" && e.Kind == RaceEventKind.LapCompleted).ToArray();
            var second = pair.Alternative.Result.Events.Where(e => e.DriverId == "leader" && e.Kind == RaceEventKind.LapCompleted).ToArray();
            var commonLaps = Math.Min(first.Length, second.Length);
            Assert.True(commonLaps >= 8);
            for (var lap = 0; lap < commonLaps; lap++) Assert.Equal(first[lap].Time, second[lap].Time, 8);
        }
    }

    [Fact]
    public void Extra_observation_reads_do_not_change_any_random_channel_or_replay()
    {
        PhaseResult RunObserved(bool read)
        {
            var race = new RaceEngine(Control(12) with { RaceFormVariation = 2, LapVariation = 1,
                StopVariation = .6, EstimateError = .04, Traffic = new(), Tyres = new(DegradationPerLap: .01) },
                [new("a", "A"), new("b", "B", Pace: 10, Gap: 2)], 31);
            PhaseResult result;
            var phase = 0;
            do
            {
                if (read) for (var repeat = 0; repeat < 20; repeat++) _ = race.GetTeamObservation("A");
                result = race.ResolvePhase(new Dictionary<string, DriverDecision>
                    { ["a"] = new(PitCompound: phase++ == 1 ? TyreCompound.Hard : null) });
            } while (!result.Finished);
            return result;
        }
        var first = RunObserved(false);
        var observed = RunObserved(true);
        Assert.Equal(first.Events, observed.Events);
        Assert.Equal(first.Classification, observed.Classification);
    }
}

