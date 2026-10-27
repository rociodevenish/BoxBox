using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class SegmentTests
{
    [Fact]
    public void Rejects_empty_segment_layout()
    {
        Assert.Throws<ArgumentException>(() => new RaceEngine(new(2, new Track("Track", Segments: [])), [new("a", "A")], 1));
    }
    [Fact]
    public void Public_projection_stops_at_segment_exit_and_is_pure()
    {
        var segment = new TrackSegment("straight", 1000, 20, 1, 0);
        var inputs = new SegmentInputs(20, 2, 0, 0, 1);
        var first = SegmentSimulation.Project(segment, inputs, 100, 4);
        Assert.Equal(18, first.TravelSeconds, 8);
        Assert.Equal(1000, SegmentSimulation.Project(segment, inputs, 100, 100).PositionMetres);
        Assert.Equal(first, SegmentSimulation.Project(segment, inputs, 100, 4));
    }
    [Fact]
    public void Solo_segmented_race_distributes_pace_warmup_and_distance_wear()
    {
        var track = new Track("Track", Segments: [new("a", 1000, 20, 1, 0), new("b", 3000, 60, 0, 1)]);
        var engine = new RaceEngine(new(2, track, Tyres: new(DegradationPerLap: .1, WarmupPenalty: 4, DegradationPenalty: 0)),
            [new("a", "A", Speed: 4, Pace: 2)], 1);
        var result = engine.ResolvePhase();
        Assert.True(result.Finished);
        Assert.Equal(158, result.Time, 8);
        Assert.Equal(.8, result.Tyres[0].Condition, 8);
        Assert.Equal(4, result.Events.Count(e => e.Kind == RaceEventKind.SegmentExited));
    }
    [Fact]
    public void Catch_prediction_expires_at_either_cars_next_exit()
    {
        var attacker = SegmentSimulation.Project(new("a", 100, 10), new(10, 0, 0, 0, 1));
        var defender = SegmentSimulation.Project(new("b", 100, 20), new(20, 0, 0, 0, 1), 95);
        Assert.Null(SegmentSimulation.PredictCatch(attacker, defender, 10));
        Assert.Equal(.4, SegmentSimulation.PredictCatch(attacker, defender, 2));
    }
    [Fact]
    public void Phase_change_inside_segment_preserves_position_and_applies_new_pace()
    {
        var race = new RaceEngine(new(6, new Track("Track", Segments: [new("a", 500, 40), new("b", 500, 60)]),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0), Traffic: new(Enabled: false)),
            [new("leader", "A"), new("follower", "B", Gap: 10)], 1);
        var phase = race.ResolvePhase();
        Assert.Equal(500, phase.Time);
        Assert.Equal(5, phase.LeaderLap);
        Assert.Equal(4, phase.Classification[1].Laps);
        Assert.Equal(11d / 12, phase.Classification[1].Progress, 8);
        var finish = race.ResolvePhase(new Dictionary<string, DriverDecision> { ["follower"] = new(PaceInstruction.Push) });
        Assert.True(finish.Finished);
        Assert.Equal(608.9, finish.Classification[1].FinishTime!.Value, 8);
    }
    [Fact]
    public void Equivalent_subdivision_preserves_finish_and_tyre_condition()
    {
        PhaseResult Run(IReadOnlyList<TrackSegment> segments)
        {
            var engine = new RaceEngine(new(12, new Track("Track", Segments: segments),
                Tyres: new(), Traffic: new(Enabled: false)), [new("a", "A", Speed: 2, Pace: 1)], 4);
            PhaseResult result;
            do { result = engine.ResolvePhase(); } while (!result.Finished);
            return result;
        }
        var whole = Run([new("a", 4000, 90)]);
        var split = Run([new("a1", 1000, 22.5), new("a2", 3000, 67.5)]);
        Assert.Equal(whole.Time, split.Time, 8);
        Assert.Equal(whole.Tyres[0].Condition, split.Tyres[0].Condition, 8);
    }
    [Fact]
    public void One_segment_matches_aggregate_clear_air_with_variability_disabled()
    {
        PhaseResult Run(Track track)
        {
            var race = new RaceEngine(new(8, track, StopVariation: 0, LapVariation: 0, Traffic: new(Enabled: false)),
                [new("a", "A", Speed: 3, Cornering: 1, Pace: 2)], 1);
            PhaseResult result;
            do { result = race.ResolvePhase(new Dictionary<string, DriverDecision>
                { ["a"] = new(PaceInstruction.Push) }); } while (!result.Finished);
            return result;
        }
        var old = Run(new("Track", 90, .6));
        var segmented = Run(new("Track", Segments: [new("lap", 5000, 90, .6, .4)]));
        Assert.Equal(old.Time, segmented.Time, 8);
        Assert.Equal(old.Tyres, segmented.Tyres);
    }

    [Fact]
    public void Track_configuration_is_snapshotted_before_resolution()
    {
        var segments = new TrackSegment[] { new("a", 1000, 90) };
        var race = new RaceEngine(new(1, new("Track", Segments: segments),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)), [new("a", "A")], 1);
        segments[0] = new("a", 1000, 1);
        Assert.Equal(90, race.ResolvePhase().Time);
    }

    [Fact]
    public void Invalid_segment_values_and_identities_are_rejected()
    {
        void Reject(params TrackSegment[] segments) => Assert.Throws<ArgumentException>(() =>
            new RaceEngine(new(1, new("Track", Segments: segments)), [new("a", "A")], 1));
        Reject(new TrackSegment("a", 0, 90));
        Reject(new TrackSegment("a", double.PositiveInfinity, 90));
        Reject(new TrackSegment("a", 100, double.NaN));
        Reject(new TrackSegment("a", 100, 90, SpeedDemand: 1.1));
        Reject(new TrackSegment("a", 100, 90, CorneringDemand: -.1));
        Reject(new TrackSegment("a", 100, 90, TyreDemand: -1));
        Reject(new TrackSegment("a", 100, 90, PassingDifficulty: 2));
        Reject(new TrackSegment("a", 100, 90), new("a", 100, 90));
    }
}

