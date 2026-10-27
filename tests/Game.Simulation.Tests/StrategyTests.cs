using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class StrategyTests
{
    [Fact]
    public void Bound_plan_rejects_rule_replacement_without_advancing()
    {
        var plan = new StrategyPlan([], [new ConditionalDecision("a", ConditionField.LeaderLap, 0,
            new(PaceInstruction.Conserve), ConditionComparison.GreaterThanOrEqual)]);
        var race = new RaceEngine(new RaceConfiguration(15, new Track("Test")), [new Driver("a", "A")], 1);
        var first = race.ResolvePhase(plan);
        plan.Conditional[0] = plan.Conditional[0] with { Decision = new(PaceInstruction.Push) };
        Assert.Throws<ArgumentException>(() => race.ResolvePhase(plan));
        Assert.Equal(first.Time, race.GetState().Time);
    }

    [Fact]
    public void Conditional_plan_replays_events_and_decision_history()
    {
        var plan = new StrategyPlan([], [new ConditionalDecision("a", ConditionField.EstimatedTyreCondition,
            .8, new(PitCompound: TyreCompound.Hard))]);
        PhaseResult Run()
        {
            var race = new RaceEngine(new RaceConfiguration(20, new Track("Test")), [new Driver("a", "A")], 19);
            PhaseResult result;
            do { result = race.ResolvePhase(plan); } while (!result.Finished);
            return result;
        }
        var first = Run();
        var second = Run();
        Assert.Equal(first.Events, second.Events);
        Assert.Equal(first.Decisions, second.Decisions);
    }
    [Fact]
    public void Scheduled_override_wins_and_does_not_consume_the_conditional_rule()
    {
        var plan = new StrategyPlan([new ScheduledDecision(0, "a", new(PaceInstruction.Conserve))],
            [new ConditionalDecision("a", ConditionField.LeaderLap, 0, new(PitCompound: TyreCompound.Hard),
                ConditionComparison.GreaterThanOrEqual)]);
        var race = new RaceEngine(new RaceConfiguration(15, new Track("Test")), [new Driver("a", "A")], 1);
        var first = race.ResolvePhase(plan);
        Assert.Equal("scheduled", first.Decisions[0].Source);
        Assert.DoesNotContain(first.Events, e => e.Kind == RaceEventKind.PitEntry);
        var second = race.ResolvePhase(plan);
        Assert.Single(second.Events, e => e.Kind == RaceEventKind.PitEntry);
    }

    [Fact]
    public void Team_observation_contains_only_own_estimates_and_is_repeatable()
    {
        var race = new RaceEngine(new RaceConfiguration(10, new Track("Test")),
            [new Driver("a", "A"), new Driver("b", "B", Gap: 2)], 1);
        race.ResolvePhase();
        var observation = race.GetTeamObservation("A");
        Assert.Equal("a", Assert.Single(observation.TeamTyres).DriverId);
        Assert.Equal(2, observation.Classification.Count);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(observation.TeamTyres), System.Text.Json.JsonSerializer.Serialize(race.GetTeamObservation("A").TeamTyres));
        Assert.NotEqual(race.GetState().Tyres[0].Condition, observation.TeamTyres[0].EstimatedCondition);
        Assert.Equal(new[] { "a" }, observation.BattleOptions.Keys);
    }

    [Fact]
    public void Invalid_future_rules_fail_before_clock_advances()
    {
        var race = new RaceEngine(new RaceConfiguration(10, new Track("Test")), [new Driver("a", "A")], 1);
        Assert.Throws<ArgumentException>(() => race.ResolvePhase(new StrategyPlan(
            [new ScheduledDecision(99, "a", new())], [])));
        Assert.Equal(0, race.GetState().Time);
    }
    [Fact]
    public void Conditional_stop_uses_team_estimate_and_fires_only_once()
    {
        var plan = new StrategyPlan([], [new ConditionalDecision("a", ConditionField.EstimatedTyreCondition,
            .9, new(PitCompound: TyreCompound.Hard))]);
        var race = new RaceEngine(new RaceConfiguration(20, new Track("Test")), [new Driver("a", "A")], 1);
        PhaseResult result;
        do { result = race.ResolvePhase(plan); } while (!result.Finished);
        Assert.Single(result.Events, e => e.Kind == RaceEventKind.PitEntry);
        Assert.True(result.Eligibility["a"]);
        Assert.NotEmpty(result.Decisions);
    }
}
