using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class TrafficTests
{
    [Fact]
    public void Pit_reentry_into_traffic_loses_time_after_exit()
    {
        var config = new RaceConfiguration(15, new Track("Narrow", 100, PassingChance: 0),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0));
        var race = new RaceEngine(config,
            [new Driver("fast", "A", Pace: 4), new Driver("train", "B", Gap: 5)], 1);
        var first = race.ResolvePhase(new Dictionary<string, DriverDecision>
            { ["fast"] = new(PitCompound: TyreCompound.Hard) });
        PhaseResult result = first;
        while (!result.Finished) result = race.ResolvePhase();
        var exit = Assert.Single(result.Events, e => e.DriverId == "fast" && e.Kind == RaceEventKind.PitExit);
        Assert.Contains(result.Events, e => e.DriverId == "fast" && e.Kind == RaceEventKind.TrafficLoss && e.Time > exit.Time);
        var clear = new RaceEngine(config, [new Driver("fast", "A", Pace: 4)], 1);
        PhaseResult clearResult = clear.ResolvePhase(new Dictionary<string, DriverDecision>
            { ["fast"] = new(PitCompound: TyreCompound.Hard) });
        while (!clearResult.Finished) clearResult = clear.ResolvePhase();
        Assert.DoesNotContain(clearResult.Events, e => e.Kind == RaceEventKind.TrafficLoss);
        Assert.True(result.Classification.Single(c => c.DriverId == "fast").FinishTime
            > clearResult.Classification[0].FinishTime);
    }
    [Fact]
    public void Successive_opponents_require_separate_actual_passes()
    {
        var result = new RaceEngine(new RaceConfiguration(5, new Track("Open", 100, PassingChance: 1),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new Driver("front", "A"), new Driver("middle", "B", Gap: 1),
             new Driver("fast", "C", Pace: 10, Gap: 2)], 1).ResolvePhase();
        Assert.Equal("fast", result.Classification[0].DriverId);
        Assert.Equal(2, result.Events.Count(e => e.DriverId == "fast" && e.Kind == RaceEventKind.Overtake));
        Assert.All(result.Events.Where(e => e.Kind == RaceEventKind.Overtake), e => Assert.True(e.Time > 0));
    }

    [Fact]
    public void Lapping_produces_blue_flag_loss_without_a_competitive_pass()
    {
        var result = new RaceEngine(new RaceConfiguration(5, new Track("Narrow", 100, PassingChance: 0),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new Driver("leader", "A"), new Driver("backmarker", "B", Pace: -100, Gap: 2)], 1).ResolvePhase();
        Assert.Contains(result.Events, e => e.DriverId == "leader" && e.Kind == RaceEventKind.BlueFlag && e.Value > 0);
        Assert.DoesNotContain(result.Events, e => e.Kind == RaceEventKind.Overtake);
        Assert.True(result.Classification[1].GapLaps > 0);
    }
    [Fact]
    public void Faster_car_is_held_up_when_passing_is_impossible()
    {
        var race = new RaceEngine(new RaceConfiguration(5, new Track("Narrow", 100, PassingChance: 0),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new Driver("leader", "A"), new Driver("fast", "B", Pace: 3, Gap: 1)], 1);
        var result = race.ResolvePhase();
        Assert.Equal("leader", result.Classification[0].DriverId);
        Assert.Contains(result.Events, e => e.DriverId == "fast" && e.Kind == RaceEventKind.TrafficLoss);
    }

    [Fact]
    public void Faster_car_can_pass_at_an_open_track()
    {
        var race = new RaceEngine(new RaceConfiguration(5, new Track("Open", 100, PassingChance: 1),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new Driver("leader", "A"), new Driver("fast", "B", Pace: 3, Gap: 1)], 1);
        var result = race.ResolvePhase();
        Assert.Equal("fast", result.Classification[0].DriverId);
        Assert.Contains(result.Events, e => e.DriverId == "fast" && e.Kind == RaceEventKind.Overtake);
    }
}
