using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class TeamStopObservationTests
{
    [Fact]
    public void Team_can_distinguish_queued_compound_from_completed_compound_use()
    {
        var race = new RaceEngine(new(20,new("Test",100),Tyres:new(DegradationPerLap:0, WarmupPenalty:0)),
            [new("leader","A"),new("slow","B",Pace:-900)],1);
        race.ResolvePhase(new Dictionary<string,DriverDecision>{{"slow",new(PitCompound:TyreCompound.Hard)}});
        var queued=Assert.Single(race.GetTeamObservation("B").TeamTyres);
        Assert.Equal(TyreCompound.Hard,queued.PendingCompound);
        Assert.Equal(new[]{TyreCompound.Medium},queued.UsedCompounds);
        race.ResolvePhase();race.ResolvePhase();
        var serviced=Assert.Single(race.GetTeamObservation("B").TeamTyres);
        Assert.Null(serviced.PendingCompound);
        Assert.Contains(TyreCompound.Hard,serviced.UsedCompounds!);
    }
}

