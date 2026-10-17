using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class NeutralisedRunningTests
{
    [Theory]
    [InlineData(36)]
    [InlineData(100)]
    public void Queued_or_ongoing_segmented_stop_survives_once_and_preserves_raw_eligibility(double trigger)
    {
        var race=new RaceEngine(new(20,new("Lab",Segments:[new("one",400,36,0,0),new("two",600,54,0,0)],PitRoute:new(.2,38)),
            Tyres:new(DegradationPerLap:.01,WarmupPenalty:0),StopVariation:0,
            Incidents:new(ComponentWearPerSecond:.0001,InterruptMajorIncidents:true)),
            [new("a","A"),new("b","B",Gap:9),new("c","C",Gap:12)],9,new([new("a",IncidentKind.MechanicalFailure,trigger)]));
        var before=race.ResolvePhase(new Dictionary<string,DriverDecision>{["b"]=new(PitCompound:TyreCompound.Hard)});
        Assert.Equal(trigger==36?"queued":"ongoing",race.GetSafetyCarOptions().Single(o=>o.DriverId=="b").Status);
        var after=race.ResolveSafetyCar(new Dictionary<string,SafetyCarDecision>{["c"]=new()});
        Assert.Single(after.Events,e=>e.DriverId=="b"&&e.Kind==RaceEventKind.TyreChanged);
        Assert.Single(after.Events,e=>e.DriverId=="b"&&e.Kind==RaceEventKind.PitExit);
        Assert.True(after.Eligibility["b"]);Assert.False(after.Eligibility["a"]);
        Assert.True(after.Tyres.Single(t=>t.DriverId=="b").Condition<1);
        Assert.True(race.GetDesignerIncidentDiagnostics().Single(d=>d.DriverId=="c").ComponentCondition<1);
    }
    [Fact]
    public void Lapped_field_keeps_its_physical_order_without_unlapping_or_racing_passes()
    {
        var race=new RaceEngine(new(30,new("Lab"),AttributeSeconds:1,Tyres:new(DegradationPerLap:0,WarmupPenalty:0),
            Traffic:new(Enabled:false),Incidents:new(ComponentWearPerSecond:0,InterruptMajorIncidents:true)),
            [new("a","A",Pace:25),new("b","B",Gap:2),new("c","C",Gap:5)],4,new([new("c",IncidentKind.Spin,400)]));
        var before=race.ResolvePhase();
        // First normal boundary precedes the event; continue to its actual timestamp.
        if(before.Interruption is null)before=race.ResolvePhase();
        var after=race.ResolveSafetyCar(race.GetSafetyCarOptions().Where(o=>o.Status=="available").ToDictionary(o=>o.DriverId,_=>new SafetyCarDecision()));
        Assert.Equal(before.Classification.Select(c=>c.DriverId),after.Classification.Select(c=>c.DriverId));
        Assert.True(after.Classification.Single(c=>c.DriverId=="b").GapLaps>0);
        Assert.DoesNotContain(after.Events.Where(e=>e.Time>before.Time),e=>e.Kind is RaceEventKind.Overtake or RaceEventKind.BlueFlag);
    }
    [Theory]
    [InlineData(1,false)]
    [InlineData(20,true)]
    public void Finish_or_continuing_failures_complete_under_safety_car(int laps,bool retire)
    {
        var events=new List<ForcedIncident>{new("a",IncidentKind.Spin,36)};
        if(retire){events.Add(new("a",IncidentKind.MechanicalFailure,50));events.Add(new("b",IncidentKind.MechanicalFailure,60));}
        var race=new RaceEngine(new(laps,new("Lab"),Incidents:new(ComponentWearPerSecond:0,InterruptMajorIncidents:true)),
            [new("a","A"),new("b","B",Gap:2)],9,new IncidentScenario(events));
        race.ResolvePhase();var result=race.ResolveSafetyCar(new Dictionary<string,SafetyCarDecision>{["a"]=new(),["b"]=new()});
        Assert.True(result.Finished);Assert.Equal(PhaseCompletionReason.Finish,result.CompletionReason);Assert.Null(result.Interruption);
        if(retire){Assert.Equal(60,result.Time);Assert.All(result.Classification,c=>Assert.NotNull(c.RetirementTime));}
        else{Assert.All(result.Classification,c=>Assert.NotNull(c.FinishTime));Assert.True(result.Classification[1].FinishTime>result.Classification[0].FinishTime);}
    }
    [Fact]
    public void Authored_pit_route_has_fixed_service_and_less_actual_loss_under_neutralisation()
    {
        RaceEngine Create(bool interrupted)=>new(new(20,new("Lab",PitRoute:new(.2,38)),
            Tyres:new(DegradationPerLap:0,WarmupPenalty:0),StopVariation:0,Traffic:new(Enabled:false),
            Incidents:new(ComponentWearPerSecond:0,InterruptMajorIncidents:interrupted)),
            [new("a","A"),new("b","B",Gap:2)],9,new([new("a",IncidentKind.Spin,36)]));
        var neutral=Create(true);neutral.ResolvePhase();
        var after=neutral.ResolveSafetyCar(new Dictionary<string,SafetyCarDecision>{["a"]=new(),["b"]=new(TyreCompound.Hard)});
        var normal=Create(false);var racing=normal.ResolvePhase(new Dictionary<string,DriverDecision>{["b"]=new(PitCompound:TyreCompound.Hard)});
        Assert.Single(after.Events,e=>e.DriverId=="b"&&e.Kind==RaceEventKind.TyreChanged);
        Assert.Equal(3,after.Events.Single(e=>e.DriverId=="b"&&e.Kind==RaceEventKind.PitService).Value);
        Assert.Equal(41,after.Events.Single(e=>e.DriverId=="b"&&e.Kind==RaceEventKind.PitEntry).Value);
        Assert.True(after.Classification.Single(c=>c.DriverId=="b").GapSeconds<racing.Classification.Single(c=>c.DriverId=="b").GapSeconds);
    }
    [Fact]
    public void Neutralised_running_compresses_real_gaps_without_passes_and_restart_uses_current_progress()
    {
        var race=new RaceEngine(new(20,new("Lab"),Tyres:new(DegradationPerLap:.01,WarmupPenalty:0),StopVariation:0,
            Incidents:new(ComponentWearPerSecond:.0001,InterruptMajorIncidents:true)),
            [new("a","A"),new("b","B",Gap:20)],9,new([new("a",IncidentKind.Spin,36)]));
        var before=race.ResolvePhase();
        var after=race.ResolveSafetyCar(new Dictionary<string,SafetyCarDecision>{["a"]=new(),["b"]=new()});
        Assert.InRange(after.Time-before.Time,179.999,180.001);
        Assert.Equal(new[]{"a","b"},after.Classification.Select(c=>c.DriverId));
        Assert.True(after.Classification[1].GapSeconds<before.Classification[1].GapSeconds);
        Assert.All(after.Classification,c=>Assert.True(c.Laps+c.Progress>before.Classification.Single(b=>b.DriverId==c.DriverId).Laps+before.Classification.Single(b=>b.DriverId==c.DriverId).Progress));
        Assert.DoesNotContain(after.Events,e=>e.Kind==RaceEventKind.Overtake);
        Assert.Null(after.Interruption);
        var leader=after.Classification[0];
        var restart=race.ResolvePhase();
        Assert.Equal(leader.Laps+leader.Progress+5,restart.NormalTargetDistance!.Value,7);
    }
}
