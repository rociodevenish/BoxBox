using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class TyreTests
{
    [Fact]
    public void Mid_lap_pace_change_charges_only_distance_travelled_under_each_instruction()
    {
        var tyres = new TyreTuning(DegradationPerLap: .01, WarmupPenalty: 0, DegradationPenalty: 0,
            PushGain: 0, ConserveLoss: 0, PushWear: 2, ConserveWear: .5);
        var engine = new RaceEngine(new RaceConfiguration(12, new Track("Test", 100), Tyres: tyres),
            [new Driver("leader", "A"), new Driver("slow", "B", Pace: -100)], 1);
        var first = engine.ResolvePhase(new Dictionary<string, DriverDecision>
            { ["slow"] = new(PaceInstruction.Conserve) });
        Assert.Equal(.9875, first.Tyres.Single(t => t.DriverId == "slow").Condition, 6);
        var second = engine.ResolvePhase(new Dictionary<string, DriverDecision>
            { ["slow"] = new(PaceInstruction.Push) });
        Assert.Equal(.9375, second.Tyres.Single(t => t.DriverId == "slow").Condition, 6);
    }

    [Fact]
    public void Compounds_and_tyre_management_change_stint_history()
    {
        PhaseResult Run(TyreCompound compound, double management = 0) =>
            new RaceEngine(new RaceConfiguration(5, new Track("Test")),
                [new Driver("a", "A", StartingCompound: compound, TyreManagement: management)], 1).ResolvePhase();
        var soft = Run(TyreCompound.Soft);
        var hard = Run(TyreCompound.Hard);
        Assert.True(soft.Time < hard.Time);
        Assert.True(soft.Tyres[0].Condition < hard.Tyres[0].Condition);
        Assert.Equal(5, soft.Tyres[0].Age);
        Assert.True(Run(TyreCompound.Soft, 10).Tyres[0].Condition > soft.Tyres[0].Condition);
    }

    [Fact]
    public void Warmup_cost_is_observable_and_invalid_tuning_is_rejected()
    {
        double Run(double warmup) => new RaceEngine(new RaceConfiguration(1, new Track("Test", 100),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: warmup)), [new Driver("a", "A")], 1)
            .ResolvePhase().Time;
        Assert.Equal(2, Run(2) - Run(0), 6);
        Assert.Throws<ArgumentException>(() => Run(double.NaN));
    }
    [Fact]
    public void Push_gains_early_time_but_can_lose_after_tyre_degradation()
    {
        double Run(PaceInstruction pace, int laps)
        {
            var race = new RaceEngine(new RaceConfiguration(laps, new Track("Test", 100)),
                [new Driver("a", "A")], 1);
            PhaseResult result;
            do { result = race.ResolvePhase(new Dictionary<string, DriverDecision> { ["a"] = new(pace) }); }
            while (!result.Finished);
            return result.Classification[0].FinishTime!.Value;
        }
        Assert.True(Run(PaceInstruction.Push, 5) < Run(PaceInstruction.Standard, 5));
        Assert.True(Run(PaceInstruction.Push, 40) > Run(PaceInstruction.Standard, 40));
    }
}
