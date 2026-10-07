using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class RaceTests
{
    [Fact]
    public void Equal_time_lapped_finish_is_independent_of_driver_id()
    {
        var race = new RaceEngine(new RaceConfiguration(12, new Track("Test", 100)),
            [new Driver("z-leader", "A"), new Driver("a-lapped", "B", Pace: -100)], 7);
        PhaseResult result;
        do { result = race.ResolvePhase(); } while (!result.Finished);
        Assert.Equal(6, result.Classification[1].Laps);
        Assert.Equal(1200, result.Classification[1].FinishTime);
    }

    [Fact]
    public void Equal_speed_follower_retains_exact_gap_at_phase_boundary()
    {
        var result = new RaceEngine(new RaceConfiguration(12, new Track("Test", 100)),
            [new Driver("a", "A"), new Driver("b", "B", Gap: 2)], 1).ResolvePhase();
        Assert.Equal(2, result.Classification[1].GapSeconds, 6);
        Assert.Equal(.98, result.Classification[1].Progress, 6);
    }
    [Fact]
    public void Same_seed_replays_a_complete_22_car_race()
    {
        var drivers = Enumerable.Range(1, 22).Select(i => new Driver($"d{i}", $"t{(i + 1) / 2}", Gap: i)).ToArray();
        PhaseResult Run()
        {
            var engine = new RaceEngine(new RaceConfiguration(7, new Track("Fast"), LapVariation: .4), drivers, 42);
            PhaseResult result;
            do { result = engine.ResolvePhase(); } while (!result.Finished);
            return result;
        }
        var first = Run();
        var second = Run();
        Assert.Equal(first.Classification, second.Classification);
        Assert.Equal(first.Events, second.Events);
        Assert.Equal(22, first.Classification.Select(c => c.DriverId).Distinct().Count());
        Assert.All(first.Classification, c => Assert.NotNull(c.FinishTime));
    }

    [Fact]
    public void Relevant_speed_attribute_improves_a_clear_air_race()
    {
        var race = new RaceEngine(new RaceConfiguration(3, new Track("Fast", 100, 1)),
            [new Driver("slow", "A"), new Driver("fast", "B", Speed: 2)], 1);
        var result = race.ResolvePhase();
        Assert.Equal("fast", result.Classification[0].DriverId);
        Assert.Equal(294, result.Classification[0].FinishTime);
    }

    [Fact]
    public void Invalid_starting_field_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new RaceEngine(new RaceConfiguration(0, new Track("Test")),
            [new Driver("a", "A")], 1));
        Assert.Throws<ArgumentException>(() => new RaceEngine(new RaceConfiguration(5, new Track("Test")),
            [new Driver("a", "A"), new Driver("a", "A")], 1));
    }

    [Fact]
    public void Race_phases_follow_leader_and_lapped_cars_finish_at_next_crossing()
    {
        var race = new RaceEngine(new RaceConfiguration(12, new Track("Test", 100)),
            [new Driver("fast", "A"), new Driver("slow", "B", Pace: -100)], 7);
        var first = race.ResolvePhase();
        Assert.Equal(5, first.LeaderLap);
        Assert.False(first.Finished);
        var second = race.ResolvePhase();
        Assert.Equal(10, second.LeaderLap);
        var finish = race.ResolvePhase();
        Assert.True(finish.Finished);
        Assert.Equal(12, finish.Classification[0].Laps);
        Assert.Equal(6, finish.Classification[1].Laps);
        Assert.Equal(1200, finish.Classification[0].FinishTime);
        Assert.Equal(1200, finish.Classification[1].FinishTime);
    }
}
