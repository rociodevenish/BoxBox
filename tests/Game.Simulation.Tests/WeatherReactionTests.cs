using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;

public class WeatherReactionTests
{
    [Fact]
    public void Fractional_weather_window_and_outcome_hash_are_identical_across_cultures()
    {
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            PhaseResult Run(string culture)
            {
                System.Globalization.CultureInfo.CurrentCulture = new(culture);
                return new RaceEngine(Configuration(new(MeanRain:1,Volatility:0,WettingPerSecond:.003)),
                    [new("a","A")],2).ResolvePhase();
            }
            var english = Run("en-US");
            var german = Run("de-DE");
            Assert.Equal("weather-reactions-v1:73.33333333333333:1", english.WeatherReaction!.Identity);
            Assert.Equal(english.WeatherReaction.Identity, german.WeatherReaction!.Identity);
            Assert.Equal(AutomaticRaceStart.OutcomeHash(english), AutomaticRaceStart.OutcomeHash(german));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = original; }
    }
    private static RaceConfiguration Configuration(WeatherTuning weather, WeatherReactionTuning? reactions=null)=>new(12,
        new("Lab",BaseLapTime:90,PitRoute:new(.2,38)),Tyres:new(DegradationPerLap:0,WarmupPenalty:0,ConserveLoss:0,PushGain:0),
        StopVariation:0,Traffic:new(Enabled:false),Weather:weather,
        WetTyres:new(DryUnsuitabilitySeconds:0,IntermediateUnsuitabilitySeconds:0,WetUnsuitabilitySeconds:0,WetSkillSeconds:0),
        WeatherReactions:reactions??new(DryBoundary:.2,WetBoundary:.7,Hysteresis:.02,MinimumSeparationSeconds:30),
        Incidents:new(ComponentWearPerSecond:0,InterruptMajorIncidents:true));

    [Theory]
    [InlineData(0)]
    [InlineData(.001)]
    public void Deferred_outward_crossing_opens_when_separation_ends_despite_flat_or_reversing_rain(double drying)
    {
        // Seed 14 has full rain until 60s, then no rain until 180s. The track is
        // saturated at the knot and still above the Wet edge when separation expires.
        var race = new RaceEngine(Configuration(new(MeanRain:.5,Volatility:1,WettingPerSecond:.02,
            DryingPerSecond:drying,IntervalSeconds:60),new(MinimumSeparationSeconds:70)),[new("a","A")],14);
        var first = race.ResolvePhase();
        Assert.Equal(14,first.Time,8);
        Assert.Equal("Intermediate",first.WeatherReaction!.Band);
        var second = race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new()});
        Assert.Equal(84,second.Time,8);
        Assert.Equal("Wet",second.WeatherReaction!.Band);
    }

    [Fact]
    public void Reversed_weather_below_deferred_upper_edge_waits_for_actual_lower_crossing()
    {
        var race = new RaceEngine(Configuration(new(MeanRain:.5,Volatility:1,WettingPerSecond:.02,
            DryingPerSecond:.02),new(MinimumSeparationSeconds:70)),[new("a","A")],14);
        Assert.Equal(14,race.ResolvePhase().Time,8);
        var next = race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new()});
        Assert.Equal(99,next.Time,8);
        Assert.Equal("Dry",next.WeatherReaction!.Band);
    }

    [Fact]
    public void Deferred_crossing_at_safety_car_deadline_opens_after_control_ends()
    {
        var config = Configuration(new(MeanRain:.5,Volatility:1,WettingPerSecond:.02,DryingPerSecond:0),
            new(MinimumSeparationSeconds:70)) with { SafetyCar = new(DurationSeconds:74) };
        var race = new RaceEngine(config,[new("a","A")],14,new([new("a",IncidentKind.Spin,10)]));
        Assert.Equal(10,race.ResolvePhase().Time,8);
        var first = race.ResolveSafetyCar(new Dictionary<string,SafetyCarDecision>{["a"]=new()});
        Assert.Equal(14,first.Time,8);
        Assert.Equal(84,first.WeatherReaction!.SafetyCarUntil);
        var next = race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new(PaceInstruction.Conserve)});
        Assert.Equal(84,next.Time,8);
        Assert.Equal("Wet",next.WeatherReaction!.Band);
        Assert.False(next.WeatherReaction.SafetyCarActive);
        Assert.Null(next.WeatherReaction.SafetyCarUntil);
    }

    [Fact]
    public void Finish_at_deferred_crossing_suppresses_the_window()
    {
        var config = Configuration(new(MeanRain:.5,Volatility:1,WettingPerSecond:.02,DryingPerSecond:0),
            new(MinimumSeparationSeconds:70));
        var race = new RaceEngine(config with { Laps=1,Track=config.Track with {BaseLapTime=84} },[new("a","A")],14);
        Assert.Equal(14,race.ResolvePhase().Time,8);
        var finish = race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new()});
        Assert.Equal(84,finish.Time,8);
        Assert.True(finish.Finished);
        Assert.Null(finish.WeatherReaction);
    }

    [Fact]
    public void Deferred_crossing_at_ordinary_phase_boundary_opens_one_weather_window()
    {
        var config = Configuration(new(MeanRain:.5,Volatility:1,WettingPerSecond:.02,DryingPerSecond:0),
            new(MinimumSeparationSeconds:70));
        var race = new RaceEngine(config with {Track=config.Track with {BaseLapTime=16.8}},[new("a","A")],14);
        Assert.Equal(14,race.ResolvePhase().Time,8);
        var boundary = race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new()});
        Assert.Equal(84,boundary.Time,8);
        Assert.Equal(5,boundary.LeaderLap);
        Assert.Equal(new[]{"Weather"},boundary.WeatherReaction!.Causes);
        var resumed = race.ResolveWeatherReaction(new Dictionary<string,DriverDecision>{["a"]=new()});
        Assert.True(resumed.Time>84);
        Assert.Null(resumed.WeatherReaction);
    }

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
