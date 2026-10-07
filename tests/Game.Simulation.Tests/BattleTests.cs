using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class BattleTests
{
    [Fact]
    public void Successful_early_attack_pays_only_actual_encounter_cost()
    {
        var result = new RaceEngine(new RaceConfiguration(5, new Track("Open", 100, PassingChance: 1)),
            [new Driver("leader", "A"), new Driver("fast", "B", Pace: 10, Gap: 1)], 1)
            .ResolvePhase(new Dictionary<string, DriverDecision> { ["fast"] = new(Battle: BattleInstruction.Attack) });
        var pass = Assert.Single(result.Events, e => e.DriverId == "fast" && e.Kind == RaceEventKind.Overtake);
        var wear = Assert.Single(result.Events, e => e.DriverId == "fast" && e.Kind == RaceEventKind.BattleWear);
        Assert.Equal(pass.Time, wear.Time);
        Assert.True(wear.Value > 0);
    }
    [Fact]
    public void Running_alone_has_only_neutral_and_invalid_attack_is_atomic()
    {
        var race = new RaceEngine(new RaceConfiguration(5, new Track("Test")), [new Driver("a", "A")], 1);
        Assert.Equal(new[] { BattleInstruction.Neutral }, race.GetState().BattleOptions["a"]);
        Assert.Throws<ArgumentException>(() => race.ResolvePhase(new Dictionary<string, DriverDecision>
            { ["a"] = new(Battle: BattleInstruction.Attack) }));
        Assert.Equal(0, race.GetState().Time);
        Assert.Empty(race.GetState().Events);
    }

    [Fact]
    public void Defend_reduces_opponent_pass_success_across_seeds()
    {
        int Wins(BattleInstruction instruction)
        {
            var wins = 0;
            for (var seed = 0; seed < 40; seed++)
            {
                var result = new RaceEngine(new RaceConfiguration(5, new Track("Test", 100, PassingChance: .4)),
                    [new Driver("leader", "A"), new Driver("fast", "B", Pace: 3, Gap: 1)], seed)
                    .ResolvePhase(new Dictionary<string, DriverDecision> { ["leader"] = new(Battle: instruction) });
                if (result.Classification[0].DriverId == "fast") wins++;
            }
            return wins;
        }
        Assert.True(Wins(BattleInstruction.Defend) < Wins(BattleInstruction.Neutral));
    }
    [Fact]
    public void Attack_improves_passing_over_matching_seeds_at_a_tyre_cost()
    {
        (int Wins, double Condition) Run(BattleInstruction battle)
        {
            var wins = 0;
            var condition = 0.0;
            for (var seed = 0; seed < 40; seed++)
            {
                var result = new RaceEngine(new RaceConfiguration(5, new Track("Test", 100, PassingChance: .25)),
                    [new Driver("leader", "A"), new Driver("fast", "B", Pace: 3, Gap: 1)], seed)
                    .ResolvePhase(new Dictionary<string, DriverDecision> { ["fast"] = new(Battle: battle) });
                if (result.Classification[0].DriverId == "fast") wins++;
                condition += result.Tyres.Single(t => t.DriverId == "fast").Condition;
            }
            return (wins, condition);
        }
        var neutral = Run(BattleInstruction.Neutral);
        var attack = Run(BattleInstruction.Attack);
        Assert.True(attack.Wins > neutral.Wins);
        Assert.True(attack.Condition < neutral.Condition);
    }
}
