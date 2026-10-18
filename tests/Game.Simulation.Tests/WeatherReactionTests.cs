using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;

public class WeatherReactionTests
{
    private static RaceConfiguration Configuration(WeatherTuning weather, WeatherReactionTuning? reactions=null)=>new(12,
        new("Lab",BaseLapTime:90,PitRoute:new(.2,38)),Tyres:new(DegradationPerLap:0,WarmupPenalty:0,ConserveLoss:0,PushGain:0),
        StopVariation:0,Traffic:new(Enabled:false),Weather:weather,
        WetTyres:new(DryUnsuitabilitySeconds:0,IntermediateUnsuitabilitySeconds:0,WetUnsuitabilitySeconds:0,WetSkillSeconds:0),
        WeatherReactions:reactions??new(DryBoundary:.2,WetBoundary:.7,Hysteresis:.02,MinimumSeparationSeconds:30),
        Incidents:new(ComponentWearPerSecond:0,InterruptMajorIncidents:true));

    [Fact]
    public void Rain_noise_inside_hysteresis_band_does_not_interrupt_phase()
    {
        var config=Configuration(new(MeanRain:.5,Volatility:.5,InitialWetness:.2,WettingPerSecond:.00001,DryingPerSecond:.00001,IntervalSeconds:10));
        var result=new RaceEngine(config,[new("a","A")],17).ResolvePhase();
        Assert.Equal(5,result.LeaderLap);Assert.Null(result.WeatherReaction);
    }

    [Fact]
    public void Drying_crosses_hysteresis_edge_at_actual_time_and_separation_delays_next_window()
    {
        var race=new RaceEngine(Configuration(new(MeanRain:0,Volatility:0,InitialWetness:.8,DryingPerSecond:.01),
            new(DryBoundary:.2,WetBoundary:.7,Hysteresis:.02,MinimumSeparationSeconds:80)),[new("a","A")],2);
        var first=race.ResolvePhase();Assert.Equal(12,first.Time,8);Assert.Equal("Intermediate",first.WeatherReaction!.Band);
        var second=race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new()});
        Assert.Equal(92,second.Time,8);Assert.Equal("Dry",second.WeatherReaction!.Band);
    }

    [Fact]
    public void Coincident_incident_and_weather_share_one_window_then_keep_original_safety_car_deadline()
    {
        var race=new RaceEngine(Configuration(new(MeanRain:1,Volatility:0,WettingPerSecond:.01)),
            [new("a","A"),new("b","B",Gap:3)],2,new([new("b",IncidentKind.Spin,22)]));
        var first=race.ResolvePhase();Assert.Equal(22,first.Time,8);
        Assert.Equal(new[]{"Weather","SafetyCar"},first.WeatherReaction!.Causes);
        var second=race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new(PaceInstruction.Conserve),["b"]=new(PaceInstruction.Conserve)});
        Assert.Equal(72,second.Time,8);Assert.True(second.WeatherReaction!.SafetyCarActive);Assert.Equal(202,second.WeatherReaction.SafetyCarUntil);
        var final=race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new(PaceInstruction.Conserve),["b"]=new(PaceInstruction.Conserve)});
        Assert.Equal(202,final.Time,8);Assert.Null(final.WeatherReaction);
    }

    [Fact]
    public void Weather_at_safety_car_deadline_opens_after_control_ends()
    {
        var race=new RaceEngine(Configuration(new(MeanRain:1,Volatility:0,WettingPerSecond:.001)),
            [new("a","A")],2,new([new("a",IncidentKind.Spin,40)]));
        race.ResolvePhase();var state=race.ResolveSafetyCar(new Dictionary<string,SafetyCarDecision>{["a"]=new()});
        Assert.Equal(220,state.Time,8);Assert.NotNull(state.WeatherReaction);Assert.False(state.WeatherReaction!.SafetyCarActive);
        Assert.Null(state.WeatherReaction.SafetyCarUntil);
        Assert.True(race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new(PaceInstruction.Push)}).Time>220);
    }

    [Fact]
    public void Existing_safety_car_period_survives_weather_and_queued_stop_occurs_once()
    {
        var race=new RaceEngine(Configuration(new(MeanRain:1,Volatility:0,WettingPerSecond:.01)),
            [new("a","A"),new("b","B",Gap:3)],2,new([new("b",IncidentKind.Spin,10)]));
        race.ResolvePhase(new Dictionary<string,DriverDecision>{["a"]=new(PitCompound:TyreCompound.Intermediate)});
        var weather=race.ResolveSafetyCar(new Dictionary<string,SafetyCarDecision>{["b"]=new()});
        Assert.Equal(22,weather.Time,8);Assert.Equal(190,weather.WeatherReaction!.SafetyCarUntil);
        Assert.Throws<ArgumentException>(()=>race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new(PaceInstruction.Push),["b"]=new(PaceInstruction.Conserve)}));
        var next=race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new(PaceInstruction.Conserve),["b"]=new(PaceInstruction.Conserve)});
        Assert.Equal(190,next.WeatherReaction!.SafetyCarUntil);
        var end=race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new(PaceInstruction.Conserve),["b"]=new(PaceInstruction.Conserve)});
        Assert.Equal(190,end.Time,8);Assert.Single(end.Events,e=>e.DriverId=="a"&&e.Kind==RaceEventKind.PitEntry);
        Assert.Single(end.Events,e=>e.DriverId=="a"&&e.Kind==RaceEventKind.TyreChanged);
    }

    [Fact]
    public void Ongoing_stop_is_not_cancelled_or_duplicated_by_complete_reaction()
    {
        var race=new RaceEngine(Configuration(new(MeanRain:1,Volatility:0,WettingPerSecond:.0022)),[new("a","A")],2);
        var at=race.ResolvePhase(new Dictionary<string,DriverDecision>{["a"]=new(PitCompound:TyreCompound.Intermediate)});
        Assert.Equal(100,at.Time,8);Assert.Equal("ongoing",race.GetSafetyCarOptions().Single().Status);
        Assert.Throws<ArgumentException>(()=>race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new(PitCompound:TyreCompound.Wet)}));
        var after=race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new(PaceInstruction.Push)});
        Assert.Single(after.Events,e=>e.Kind==RaceEventKind.PitEntry);Assert.Single(after.Events,e=>e.Kind==RaceEventKind.PitExit);
    }

    [Fact]
    public void Chequered_flag_at_same_timestamp_suppresses_weather_window()
    {
        var config=Configuration(new(MeanRain:1,Volatility:0,WettingPerSecond:.01),new(DryBoundary:.88,WetBoundary:.95,Hysteresis:.02));
        var race=new RaceEngine(config with{Laps=1},[new("a","A")],2);
        var finish=race.ResolvePhase();Assert.Equal(90,finish.Time,8);Assert.True(finish.Finished);Assert.Null(finish.WeatherReaction);
    }
    [Fact]
    public void Rain_onset_stops_at_exact_crossing_and_complete_reaction_queues_next_lap_stop()
    {
        var race = new RaceEngine(new(8,new("Lab",Segments:[new("first",500,45),new("last",500,45)]),StopVariation:0,Traffic:new(Enabled:false),
            Weather:new(MeanRain:1,Volatility:0,WettingPerSecond:.01), WetTyres:new(),
            WeatherReactions:new(DryBoundary:.2,Hysteresis:.02)),[new("a","A"),new("b","A")],1);
        var onset=race.ResolvePhase();
        Assert.Equal(22,onset.Time,8);
        Assert.Equal("Intermediate",onset.WeatherReaction!.Band);
        Assert.Null(onset.Interruption);
        var resumed=race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{
            ["a"]=new(PaceInstruction.Push,TyreCompound.Intermediate),["b"]=new(PaceInstruction.Conserve)});
        Assert.True(resumed.Time>onset.Time);
        Assert.Equal(0,resumed.Events.Count(e=>e.Kind==RaceEventKind.PitEntry));
    }
}
