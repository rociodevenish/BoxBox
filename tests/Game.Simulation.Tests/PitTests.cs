using Game.Domain;
using Game.Simulation;
using Xunit;

namespace Game.Simulation.Tests;

public class PitTests
{
    [Fact]
    public void Queued_stop_survives_an_intervening_phase_boundary()
    {
        var race = new RaceEngine(new RaceConfiguration(20, new Track("Test", 100),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new Driver("leader", "A"), new Driver("slow", "B", Pace: -900)], 1);
        race.ResolvePhase(new Dictionary<string, DriverDecision> { ["slow"] = new(PitCompound: TyreCompound.Hard) });
        var result = race.ResolvePhase();
        // The leader reaches lap 10 at the same instant as slow completes lap 1.
        result = race.ResolvePhase();
        Assert.Single(result.Events, e => e.DriverId == "slow" && e.Kind == "PitEntry");
    }
    [Fact]
    public void Fresh_tyres_can_repay_stop_loss_in_clear_air()
    {
        double Run(bool pit)
        {
            var race = new RaceEngine(new RaceConfiguration(40, new Track("Test", 100)),
                [new Driver("a", "A")], 1);
            PhaseResult result;
            var phase = 0;
            do
            {
                result = race.ResolvePhase(new Dictionary<string, DriverDecision>
                    { ["a"] = new(PitCompound: pit && phase == 4 ? TyreCompound.Hard : null) });
                phase++;
            } while (!result.Finished);
            return result.Classification[0].FinishTime!.Value;
        }
        Assert.True(Run(true) < Run(false));
    }

    [Fact]
    public void Lapped_driver_pits_at_own_next_crossing()
    {
        var race = new RaceEngine(new RaceConfiguration(12, new Track("Test", 100),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)),
            [new Driver("leader", "A"), new Driver("slow", "B", Pace: -100)], 1);
        race.ResolvePhase();
        var result = race.ResolvePhase(new Dictionary<string, DriverDecision>
            { ["slow"] = new(PitCompound: TyreCompound.Hard) });
        var entry = Assert.Single(result.Events, e => e.Kind == "PitEntry");
        Assert.Equal(600, entry.Time);
        Assert.Equal(3, entry.Lap);
    }
    [Fact]
    public void No_stop_is_ineligible_without_losing_raw_result()
    {
        var result = new RaceEngine(new RaceConfiguration(3, new Track("Test")),
            [new Driver("a", "A")], 1).ResolvePhase();
        Assert.True(result.Finished);
        Assert.False(result.Eligibility["a"]);
        Assert.NotNull(result.Classification[0].FinishTime);
        Assert.DoesNotContain(result.Events, e => e.Kind == "PitEntry");
    }

    [Fact]
    public void Invalid_compound_or_driver_is_rejected_without_advancing_clock()
    {
        var race = new RaceEngine(new RaceConfiguration(10, new Track("Test")), [new Driver("a", "A")], 1);
        Assert.Throws<ArgumentException>(() => race.ResolvePhase(new Dictionary<string, DriverDecision>
            { ["a"] = new(PitCompound: (TyreCompound)99) }));
        Assert.Throws<ArgumentException>(() => race.ResolvePhase(new Dictionary<string, DriverDecision>
            { ["unknown"] = new() }));
        Assert.Equal(5, race.ResolvePhase().LeaderLap);
    }

    [Fact]
    public void Extra_pit_does_not_shift_another_drivers_random_laps()
    {
        RaceEvent[] Run(bool pit)
        {
            var race = new RaceEngine(new RaceConfiguration(12, new Track("Test"), LapVariation: .4),
                [new Driver("leader", "A", Pace: 4), new Driver("other", "B", Gap: 10)], 42);
            var first = race.ResolvePhase(new Dictionary<string, DriverDecision>
                { ["other"] = new(PitCompound: pit ? TyreCompound.Hard : null) });
            PhaseResult result = first;
            while (!result.Finished) result = race.ResolvePhase();
            return result.Events.Where(e => e.DriverId == "leader" && e.Kind == "LapCompleted").ToArray();
        }
        Assert.Equal(Run(false), Run(true));
    }
    [Fact]
    public void Pit_at_next_lap_end_changes_tyres_and_preserves_pace()
    {
        var race = new RaceEngine(new RaceConfiguration(12, new Track("Test", 100),
            Tyres: new(DegradationPerLap: 0, WarmupPenalty: 0)), [new Driver("a", "A")], 1);
        race.ResolvePhase();
        var result = race.ResolvePhase(new Dictionary<string, DriverDecision>
        { ["a"] = new(PaceInstruction.Push, TyreCompound.Hard) });
        var stop = Assert.Single(result.Events, e => e.Kind == "PitEntry");
        Assert.Equal(6, stop.Lap);
        Assert.Equal(TyreCompound.Hard, result.Tyres[0].Compound);
        Assert.Equal(4, result.Tyres[0].Age);
        Assert.Equal(PaceInstruction.Push, result.Tyres[0].Pace);
        Assert.True(result.Eligibility["a"]);
    }
}
