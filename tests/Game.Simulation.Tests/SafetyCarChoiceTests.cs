using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class SafetyCarChoiceTests
{
    private static RaceEngine Race()=>new(new(20,new("Lab"),Tyres:new(DegradationPerLap:0,WarmupPenalty:0),Traffic:new(Enabled:false),Incidents:new(ComponentWearPerSecond:0,InterruptMajorIncidents:true)),
        [new("a","A"),new("b","B",Gap:9)],9,new([new("a",IncidentKind.Spin,36)]));
    [Fact]
    public void Special_choices_queue_next_lap_stops_without_advancing_the_incident_clock()
    {
        var race=Race();var interrupted=race.ResolvePhase();
        var accepted=race.CommitSafetyCarChoices(new Dictionary<string,SafetyCarDecision>{["a"]=new(TyreCompound.Hard),["b"]=new()});
        Assert.Equal(interrupted.Time,accepted.Time);Assert.Equal(interrupted.Classification,accepted.Classification);
        Assert.Equal("queued",race.GetSafetyCarOptions().Single(c=>c.DriverId=="a").Status);
        Assert.DoesNotContain(accepted.Events,e=>e.Kind==RaceEventKind.PitEntry);
        var continued=race.ResolvePhase();
        Assert.Single(continued.Events,e=>e.DriverId=="a"&&e.Kind==RaceEventKind.TyreChanged);
        Assert.Equal(1,continued.Events.Single(e=>e.DriverId=="a"&&e.Kind==RaceEventKind.PitEntry).Lap);
    }
    [Fact]
    public void Committed_stop_cannot_be_replaced_or_duplicated_and_bad_batch_applies_nothing()
    {
        var race=Race();race.ResolvePhase(new Dictionary<string,DriverDecision>{["a"]=new(PitCompound:TyreCompound.Hard)});
        Assert.Throws<ArgumentException>(()=>race.CommitSafetyCarChoices(new Dictionary<string,SafetyCarDecision>{["a"]=new(TyreCompound.Soft),["b"]=new(TyreCompound.Soft)}));
        Assert.Equal("available",race.GetSafetyCarOptions().Single(c=>c.DriverId=="b").Status);
        Assert.Equal(TyreCompound.Hard,race.GetSafetyCarOptions().Single(c=>c.DriverId=="a").CommittedCompound);
    }
    [Fact]
    public void Retired_and_ongoing_cars_need_no_choice_and_stop_compound_stays_committed()
    {
        var race=new RaceEngine(new(20,new("Lab"),Tyres:new(DegradationPerLap:0,WarmupPenalty:0),Traffic:new(Enabled:false),
            Incidents:new(ComponentWearPerSecond:0,InterruptMajorIncidents:true)),[new("a","A"),new("b","B",Gap:9),new("c","C",Gap:12)],9,
            new IncidentScenario([new("a",IncidentKind.MechanicalFailure,100)]));
        race.ResolvePhase(new Dictionary<string,DriverDecision>{["b"]=new(PitCompound:TyreCompound.Hard)});
        Assert.Equal("retired",race.GetSafetyCarOptions().Single(c=>c.DriverId=="a").Status);
        var ongoing=race.GetSafetyCarOptions().Single(c=>c.DriverId=="b");Assert.Equal("ongoing",ongoing.Status);Assert.Equal(TyreCompound.Hard,ongoing.CommittedCompound);
        Assert.Throws<ArgumentException>(()=>race.CommitSafetyCarChoices(new Dictionary<string,SafetyCarDecision>{["a"]=new(),["c"]=new()}));
        var staged=race.CommitSafetyCarChoices(new Dictionary<string,SafetyCarDecision>{["c"]=new(TyreCompound.Soft)});
        Assert.Equal(100,staged.Time);Assert.Single(staged.Events,e=>e.DriverId=="b"&&e.Kind==RaceEventKind.TyreChanged);
    }
}
