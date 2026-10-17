using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class IncidentTests
{
    [Fact]
    public void Attack_and_defend_raise_contact_risk_only_at_the_same_actual_encounter()
    {
        int Contacts(bool battle)
        {
            var count = 0;
            foreach (var seed in Enumerable.Range(1, 64))
            {
                var race = new RaceEngine(new(2, new("Lab", PassingChance: 1, Segments: [new("zone", 1000, 90, PassingDifficulty: 0)]),
                    Traffic: new(AttackMultiplier: 1, DefendMultiplier: 1, BattleWear: 0), PassingVariation: 0,
                    Incidents: new(ContactProbability: .2)), [new("a", "A"), new("b", "B", Speed: 10, Gap: 1)], seed);
                var state = race.ResolvePhase(new Dictionary<string, DriverDecision> {
                    ["a"] = new(Battle: battle ? BattleInstruction.Defend : BattleInstruction.Neutral),
                    ["b"] = new(Battle: battle ? BattleInstruction.Attack : BattleInstruction.Neutral) });
                if (state.Events.Any(e => e.Kind == RaceEventKind.Contact)) count++;
            }
            return count;
        }
        var neutral = Contacts(false);
        var battling = Contacts(true);
        Assert.InRange(neutral, 1, 63);
        Assert.True(battling > neutral);
    }

    [Fact]
    public void Worn_tyres_and_track_stress_increase_their_respective_exposure()
    {
        IncidentDiagnostic Run(TyreCompound compound, double stress)
        {
            var race = new RaceEngine(new(8, new("Lab"), Tyres: new(DegradationPerLap: .1, DegradationPenalty: 0, ExhaustionPenalty: 0),
                Incidents: new(ErrorRate: 1e-9, FailureRate: 1e-9, MechanicalStress: stress)),
                [new("a", "A", StartingCompound: compound)], 9);
            while (!race.GetState().Finished) race.ResolvePhase();
            return Assert.Single(race.GetDesignerIncidentDiagnostics());
        }
        Assert.True(Run(TyreCompound.Soft, 1).ErrorExposure > Run(TyreCompound.Hard, 1).ErrorExposure);
        Assert.True(Run(TyreCompound.Medium, 1.5).FailureExposure > Run(TyreCompound.Medium, .5).FailureExposure);
    }
    [Fact]
    public void Rejects_error_rates_that_can_sustain_unbounded_time_loss()
    {
        Assert.Throws<ArgumentException>(() => new RaceEngine(new(2, new("Lab"), Incidents: new(ErrorRate: .1)), [new("a", "A")], 9));
    }
    [Fact]
    public void Zero_channels_preserve_legacy_race_outcomes_and_detailed_source_inputs_are_snapshotted()
    {
        var weekend = WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"), 8);
        var inputs = WeekendPresetCatalog.IncidentInputs(weekend, new(ComponentWearPerSecond: 0));
        Assert.Equal(1.25, inputs.Configuration.Incidents!.MechanicalStress);
        for (var i = 0; i < inputs.Drivers.Length; i++)
        {
            Assert.Equal(weekend.Cars[i].Source.Driver.Ratings["errorControl"], inputs.Drivers[i].IncidentProfile!.ErrorControl);
            Assert.Equal(82, inputs.Drivers[i].IncidentProfile!.Reliability);
            Assert.Contains(WeekendPreparation.CardVersion, inputs.Drivers[i].IncidentProfile!.SourceVersion);
        }
        var legacy = StrategyComparison.Run(weekend.Configuration, weekend.Cars.Select(c => c.RaceDriver).ToArray(), new([], []), 9);
        var zero = StrategyComparison.Run(inputs.Configuration, inputs.Drivers, new([], []), 9);
        Assert.Equal(legacy.Result.Events, zero.Result.Events);
        Assert.Equal(legacy.Result.Classification, zero.Result.Classification);
        Assert.Equal(legacy.Result.Tyres, zero.Result.Tyres);
        Assert.DoesNotContain("Incidents", System.Text.Json.JsonSerializer.Serialize(weekend.Configuration));
        Assert.DoesNotContain("IncidentProfile", System.Text.Json.JsonSerializer.Serialize(weekend.Cars[0].RaceDriver));
    }

    [Fact]
    public void Error_control_reliability_and_actual_pace_change_exposure_without_extra_draws()
    {
        IncidentDiagnostic Run(PaceInstruction pace, IncidentProfile profile)
        {
            var race = new RaceEngine(new(8, new("Lab"), Incidents: new(ErrorRate: 1e-9, FailureRate: 1e-9)),
                [new("a", "A", IncidentProfile: profile)], 9);
            while (!race.GetState().Finished) race.ResolvePhase(new Dictionary<string, DriverDecision> { ["a"] = new(pace) });
            Assert.Single(race.GetDesignerDiagnostics(), d => d.Channel == "local-incidents-v1/error");
            Assert.Single(race.GetDesignerDiagnostics(), d => d.Channel == "local-incidents-v1/mechanical");
            return Assert.Single(race.GetDesignerIncidentDiagnostics());
        }
        var careful = Run(PaceInstruction.Conserve, new(100, 100));
        var push = Run(PaceInstruction.Push, new(100, 100));
        var weaker = Run(PaceInstruction.Push, new(1, 1));
        Assert.True(push.ErrorExposure > careful.ErrorExposure);
        Assert.True(push.FailureExposure > careful.FailureExposure);
        Assert.True(push.ComponentCondition < careful.ComponentCondition);
        Assert.True(weaker.ErrorExposure > push.ErrorExposure);
        Assert.True(weaker.FailureExposure > push.FailureExposure);
    }
    [Fact]
    public void Component_wear_counts_running_seconds_and_excludes_the_pit_stop()
    {
        var race = new RaceEngine(new(2, new("Lab"), Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0),
            StopVariation: 0, Incidents: new(ComponentWearPerSecond: .001)), [new("a", "A")], 9);
        var result = race.ResolvePhase(new Dictionary<string, DriverDecision> { ["a"] = new(PitCompound: TyreCompound.Hard) });
        Assert.Equal(203.8, result.Time, 8); // Hard compound costs 0.8 seconds on lap 2.
        Assert.Equal(.8192, Assert.Single(race.GetDesignerIncidentDiagnostics()).ComponentCondition, 8);
        Assert.DoesNotContain("ComponentCondition", System.Text.Json.JsonSerializer.Serialize(result));
        Assert.Equal("Good", Assert.Single(race.GetTeamObservation("A").Conditions!).ComponentCondition);
    }

    [Fact]
    public void Nonzero_hazards_and_wear_are_invariant_under_equivalent_subdivision_and_observation()
    {
        (PhaseResult Result, RaceEngine Engine) Run(IReadOnlyList<TrackSegment> segments, bool observe)
        {
            var race = new RaceEngine(new(12, new("Lab", Segments: segments),
                Tyres: new(DegradationPerLap: .02, WarmupPenalty: 0, DegradationPenalty: 0), Traffic: new(Enabled: false),
                Incidents: new(ErrorRate: .004, FailureRate: .0004, ComponentWearPerSecond: .0005)),
                [new("a", "A", IncidentProfile: new(60, 70))], 9);
            PhaseResult state;
            do
            {
                if (observe) for (var i = 0; i < 10; i++) { _ = race.GetState(); _ = race.GetTeamObservation("A"); }
                state = race.ResolvePhase();
            } while (!state.Finished);
            return (state, race);
        }
        var whole = Run([new("whole", 1000, 90)], false);
        var split = Run([new("one", 250, 22.5), new("two", 750, 67.5)], true);
        Assert.Equal(whole.Result.Time, split.Result.Time, 8);
        Assert.Equal(whole.Result.Tyres[0].Condition, split.Result.Tyres[0].Condition, 8);
        Assert.Equal(whole.Engine.GetDesignerIncidentDiagnostics()[0].ComponentCondition,
            split.Engine.GetDesignerIncidentDiagnostics()[0].ComponentCondition, 8);
        var expected = whole.Result.Events.Where(e => e.Kind >= RaceEventKind.LockUp).ToArray();
        var actual = split.Result.Events.Where(e => e.Kind >= RaceEventKind.LockUp).ToArray();
        Assert.NotEmpty(expected);
        Assert.Equal(expected.Select(e => e.Kind), actual.Select(e => e.Kind));
        for (var i = 0; i < expected.Length; i++) Assert.Equal(expected[i].Time, actual[i].Time, 8);
    }

    [Fact]
    public void Forced_contact_requires_an_actual_encounter_and_records_both_cars_once()
    {
        PhaseResult Run(double gap)
        {
            var race = new RaceEngine(new(2, new("Lab", Segments: [new("zone", 1000, 90)]), Incidents: new()),
                [new("a", "A"), new("b", "B", Speed: 10, Gap: gap)], 1,
                new IncidentScenario([new("b", IncidentKind.Contact, 0)]));
            return race.ResolvePhase();
        }
        Assert.DoesNotContain(Run(100).Events, e => e.Kind == RaceEventKind.Contact);
        var events = Run(1).Events.Where(e => e.Kind == RaceEventKind.Contact).ToArray();
        Assert.Equal(2, events.Length);
        Assert.Equal(events[0].Time, events[1].Time);
        Assert.Equal(events[0].EncounterId, events[1].EncounterId);
    }

    [Fact]
    public void Diagnostic_replay_round_trips_forced_consequences_without_a_player_command()
    {
        var replay = SimulationReplay.CaptureDiagnostic(new(8, new("Lab"), Incidents: new()), [new("a", "A")],
            new([], []), 9, new([new("a", IncidentKind.MechanicalFailure, 20)]));
        var saved = System.Text.Json.JsonSerializer.Serialize(replay);
        var restored = System.Text.Json.JsonSerializer.Deserialize<RaceReplay>(saved)!;
        var first = StrategyComparison.Replay(replay);
        var second = StrategyComparison.Replay(restored);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(first.Result), System.Text.Json.JsonSerializer.Serialize(second.Result));
        Assert.Equal(first.Result.Events, second.Result.Events);
        Assert.Equal(first.Diagnostics, second.Diagnostics);
    }
    [Fact]
    public void All_retired_cars_freeze_distance_rank_and_require_no_instructions()
    {
        var race = new RaceEngine(new(8, new("Lab"), Incidents: new()), [new("a", "A"), new("b", "B", Gap: 1)], 9,
            new IncidentScenario([new("a", IncidentKind.MechanicalFailure, 20), new("b", IncidentKind.MechanicalFailure, 40)]));
        var result = race.ResolvePhase();
        Assert.True(result.Finished);
        Assert.Equal(40, result.Time);
        Assert.Equal("b", result.Classification[0].DriverId);
        Assert.All(result.Classification, c => Assert.NotNull(c.RetirementTime));
        Assert.All(result.BattleOptions.Values, Assert.Empty);
        Assert.Throws<ArgumentException>(() => race.ResolvePhase(new Dictionary<string, DriverDecision> { ["a"] = new() }));
        Assert.Equal(result.Classification, race.ResolvePhase().Classification);
    }
    [Fact]
    public void Local_spin_is_applied_once_at_its_actual_time_and_pits_do_not_repair_damage()
    {
        var race = new RaceEngine(new(8, new("Lab"), Incidents: new()), [new("a", "A")], 9,
            new IncidentScenario([new("a", IncidentKind.Spin, 20)]));
        var first = race.ResolvePhase(new Dictionary<string, DriverDecision> { ["a"] = new(PitCompound: TyreCompound.Hard) });
        var incident = Assert.Single(first.Events, e => e.Kind == RaceEventKind.Spin);
        Assert.Equal(20, incident.Time);
        Assert.True(Assert.Single(first.CarConditions!).DamageSecondsPerLap > 0);
        var finish = race.ResolvePhase();
        Assert.Single(finish.Events, e => e.Kind == RaceEventKind.Spin);
        Assert.Equal(first.CarConditions![0].DamageSecondsPerLap, finish.CarConditions![0].DamageSecondsPerLap);
        Assert.Contains(finish.Events, e => e.Kind == RaceEventKind.TyreChanged);
    }
}

