using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public sealed class TimingObservationTests
{
    [Fact]
    public void Recorded_queue_contact_zero_is_distinct_from_missing_initial_timing()
    {
        var race = new RaceEngine(new(6, new("Restricted", Segments: [new("a", 1000, 100, 0, 0, PassingAllowed: false)]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)), [new("leader", "A"), new("follower", "B", Pace: 10, Gap: 5)], 1);
        Assert.Null(race.GetTimingObservation().Single(car => car.DriverId == "follower").ToAhead.Seconds);
        race.ResolvePhase();
        var contact = race.GetTimingObservation().Single(car => car.DriverId == "follower");
        Assert.Equal(0, contact.ToAhead.Seconds);
        Assert.Equal(0, contact.ToAhead.Laps);
    }

    [Fact]
    public void Ahead_reference_uses_its_own_lap_deficit_and_finish_timing()
    {
        var config = new RaceConfiguration(6, new("Open", Segments: [new("a", 1000, 100, 0, 0)]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0), Traffic: new(Enabled: false));
        var race = new RaceEngine(config, [new("leader", "A"), new("middle", "B", Pace: -100), new("last", "C", Pace: -110)], 1);
        race.ResolvePhase();
        var last = race.GetTimingObservation().Single(car => car.DriverId == "last");
        Assert.Equal(2, last.ToLeader.Laps);
        Assert.Null(last.ToLeader.Seconds);
        Assert.Equal(0, last.ToAhead.Laps);
        Assert.True(last.ToAhead.Seconds > 0);

        var finish = new RaceEngine(config, [new("a", "A"), new("b", "B", Gap: 3), new("c", "C", Gap: 8)], 1);
        while (!finish.ResolvePhase().Finished) { }
        Assert.Equal(5, finish.GetTimingObservation().Single(car => car.DriverId == "c").ToAhead.Seconds);
        Assert.Equal(8, finish.GetTimingObservation().Single(car => car.DriverId == "c").ToLeader.Seconds);
    }
}
