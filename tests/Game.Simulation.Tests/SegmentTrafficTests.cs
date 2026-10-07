using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class SegmentTrafficTests
{
    private static RaceConfiguration Config(params TrackSegment[] segments) => new(2,
        new("Track", Segments: segments), Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0));

    [Fact]
    public void Faster_car_follows_in_restricted_segments_without_crossing_defender()
    {
        var result = new RaceEngine(Config(new TrackSegment("restricted", 1000, 100, 0, 0, PassingAllowed: false)),
            [new("leader", "A"), new("follower", "B", Pace: 10, Gap: 5)], 1).ResolvePhase();
        Assert.Equal("leader", result.Classification[0].DriverId);
        Assert.Equal(200, result.Classification[1].FinishTime!.Value, 8);
        Assert.Contains(result.Events, e => e.DriverId == "follower" && e.Kind == RaceEventKind.TrafficLoss);
    }
    [Fact]
    public void Queue_of_three_keeps_order_across_segment_and_phase_boundaries()
    {
        var race = new RaceEngine(Config(new TrackSegment("a", 100, 10, 0, 0, PassingAllowed: false),
            new("b", 900, 90, 0, 0, PassingAllowed: false)) with { Laps = 6 },
            [new("z", "A"), new("y", "B", Pace: 10, Gap: 2), new("x", "C", Pace: 20, Gap: 4)], 1);
        var first = race.ResolvePhase();
        Assert.Equal(new[] { "z", "y", "x" }, first.Classification.Select(c => c.DriverId));
        var final = race.ResolvePhase();
        Assert.Equal(new[] { "z", "y", "x" }, final.Classification.Select(c => c.DriverId));
        Assert.All(final.Classification, c => Assert.Equal(600, c.FinishTime!.Value, 8));
    }
    [Fact]
    public void Colocated_start_has_stable_order_and_does_not_allow_overlap_to_become_a_pass()
    {
        var result = new RaceEngine(Config(new TrackSegment("a", 1000, 100, 0, 0, PassingAllowed: false)),
            [new("a", "A"), new("b", "B", Pace: 10)], 1).ResolvePhase();
        Assert.Equal("a", result.Classification[0].DriverId);
        Assert.Equal(200, result.Classification[1].FinishTime!.Value, 8);
    }
    [Fact]
    public void Corner_gain_is_followed_by_straight_loss_with_equal_lap_pace()
    {
        var corner = new TrackSegment("corner", 500, 50, 0, 1);
        var straight = new TrackSegment("straight", 500, 50, 1, 0);
        var cornerCar = new SegmentInputs(100, 0, 20, 0, 1);
        var straightCar = new SegmentInputs(100, 20, 0, 0, 1);
        Assert.Equal(40, SegmentSimulation.Project(corner, cornerCar).TravelSeconds);
        Assert.Equal(50, SegmentSimulation.Project(straight, cornerCar).TravelSeconds);
        Assert.Equal(50, SegmentSimulation.Project(corner, straightCar).TravelSeconds);
        Assert.Equal(40, SegmentSimulation.Project(straight, straightCar).TravelSeconds);
    }

    [Fact]
    public void Apparent_catch_beyond_corner_boundary_is_invalidated_by_straight_speed()
    {
        var result = new RaceEngine(Config(new TrackSegment("corner", 500, 50, 0, 1, PassingAllowed: false),
            new("straight", 500, 50, 1, 0, PassingAllowed: false)),
            [new("leader", "A", Speed: 20), new("follower", "B", Cornering: 20, Gap: 12)], 1).ResolvePhase();
        Assert.Equal(180, result.Classification[0].FinishTime!.Value, 8);
        Assert.Equal(192, result.Classification[1].FinishTime!.Value, 8);
        Assert.DoesNotContain(result.Events, e => e.Kind == RaceEventKind.TrafficLoss);
    }

    [Fact]
    public void Different_segments_preserve_normalized_progress_and_trajectory_gap()
    {
        var race = new RaceEngine(Config(new TrackSegment("corner", 100, 20, PassingAllowed: false),
            new("straight", 900, 80, PassingAllowed: false)) with { Laps = 6 },
            [new("leader", "A"), new("follower", "B", Gap: 85)], 1);
        var phase = race.ResolvePhase();
        Assert.Equal(500, phase.Time);
        Assert.Equal(4, phase.Classification[1].Laps);
        Assert.Equal(.075, phase.Classification[1].Progress, 8);
        Assert.Equal(85, phase.Classification[1].GapSeconds, 8);
        Assert.Equal(4000, phase.Events.Last(e => e.DriverId == "follower" && e.Kind == RaceEventKind.SegmentExited).DistanceMetres!.Value, 8);
    }
    [Fact]
    public void Complete_22_car_restricted_race_preserves_the_queue_and_replays()
    {
        PhaseResult Run()
        {
            var race = new RaceEngine(Config(new TrackSegment("a", 300, 30, PassingAllowed: false),
                new("b", 700, 70, PassingAllowed: false)) with { Laps = 12 },
                Enumerable.Range(0, 22).Select(i => new Driver($"d{i:00}", $"t{i / 2}", Pace: i, Gap: i)).ToArray(), 7);
            PhaseResult result;
            do { result = race.ResolvePhase(); } while (!result.Finished);
            return result;
        }
        var result = Run();
        Assert.Equal(Enumerable.Range(0, 22).Select(i => $"d{i:00}"), result.Classification.Select(c => c.DriverId));
        Assert.All(result.Classification, c => Assert.Equal(1200, c.FinishTime!.Value, 8));
        Assert.Equal(result.Events, Run().Events);
    }
}

