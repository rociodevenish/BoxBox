using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;

public class WeatherTests
{
    [Fact]
    public void Authoritative_conditions_advance_with_race_time_and_reads_do_not_advance_them()
    {
        var race = new RaceEngine(new(10, new("Weather lab"), Weather: new(InitialWetness: .2, MeanRain: .8)),
            [new("a", "A")], 17);
        var initial = race.GetState().Weather!;
        Assert.Equal(0, initial.Time);
        Assert.Equal(.2, initial.Wetness);
        Assert.Equal(initial, race.GetState().Weather);
        var result = race.ResolvePhase();
        Assert.Equal(result.Time, result.Weather!.Time);
        Assert.True(result.Weather.Wetness > initial.Wetness);
        Assert.Equal(result.Weather, race.GetState().Weather);
    }
    [Fact]
    public void Shared_preparation_clock_has_exact_drying_and_seeded_interval_identity()
    {
        var tuning = new WeatherTuning(MeanRain: 0, Volatility: 0, InitialWetness: .8);
        Assert.Equal(.2, CircuitWeather.Observe(tuning, 5, 600).Wetness, 10);
        Assert.Equal(0, CircuitWeather.Observe(tuning, 5, 1800).Wetness);
        var rainy = tuning with { MeanRain = .5, Volatility = .4 };
        Assert.Equal(CircuitWeather.Observe(rainy, 5, 600), CircuitWeather.Observe(rainy, 5, 600));
        Assert.NotEqual(CircuitWeather.Observe(rainy, 5, 600).RainIntensity,
            CircuitWeather.Observe(rainy, 6, 600).RainIntensity);
        Assert.NotEqual(CircuitWeather.Observe(rainy, 5, 599).IntervalIdentity,
            CircuitWeather.Observe(rainy, 5, 600).IntervalIdentity);
    }

    [Fact]
    public void Forecast_reads_are_frozen_by_time_and_only_own_team_forecast_is_observed()
    {
        var config = new RaceConfiguration(10, new("Weather lab"), Weather: new());
        var race = new RaceEngine(config, [new("a", "A"), new("b", "B")], 17);
        var forecast = race.GetTeamObservation("A").Forecast!;
        Assert.Equal(0, forecast.ObservedAt);
        Assert.Equal(120, forecast.RefreshAt);
        Assert.Equal(new[] { 300d, 900d }, forecast.Horizons.Select(h => h.ToSeconds));
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(forecast),
            System.Text.Json.JsonSerializer.Serialize(race.GetTeamObservation("A").Forecast));
        Assert.NotEqual(forecast.ObservationIdentity, race.GetTeamObservation("B").Forecast!.ObservationIdentity);
        Assert.Null(new RaceEngine(config with { Weather = null }, [new("a", "A")], 17).GetTeamObservation("A").Forecast);
    }
    [Fact]
    public void Weather_at_same_interruption_time_is_independent_of_strategy_geometry_and_observation_count()
    {
        RaceEngine Race(bool divided) => new(new(10, new("Lab", Segments: divided
                ? [new("first",500,45),new("last",500,45)] : [new("whole",1000,90)]),
            StopVariation:0, Traffic:new(Enabled:false), Weather:new(),
            Incidents:new(ComponentWearPerSecond:0,InterruptMajorIncidents:true)),
            [new("a","A")],17,new([new("a",IncidentKind.Spin,36)]));
        var first=Race(false); var second=Race(true);
        for(var i=0;i<10;i++){second.GetState();second.GetTeamObservation("A");}
        var a=first.ResolvePhase(new Dictionary<string,DriverDecision>{{"a",new(PaceInstruction.Push)}});
        var b=second.ResolvePhase(new Dictionary<string,DriverDecision>{{"a",new(PaceInstruction.Conserve,TyreCompound.Hard)}});
        Assert.Equal(36,a.Time);Assert.Equal(36,b.Time);
        Assert.Equal(a.Weather,b.Weather);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(first.GetWeatherForecast("A")),
            System.Text.Json.JsonSerializer.Serialize(second.GetWeatherForecast("A")));
    }

    [Fact]
    public void Forecast_quality_only_narrows_bounded_uncertainty_and_does_not_consult_future_weather()
    {
        var observed=new CurrentWeather(WeatherTuning.Version,120,"fixed-observation",.4,.3,.001);
        var low=CircuitWeather.Forecast(observed,"A",0);
        var high=CircuitWeather.Forecast(observed,"A",1);
        Assert.Equal(low.ObservationIdentity,high.ObservationIdentity);
        for(var i=0;i<low.Horizons.Count;i++){
            Assert.InRange(low.Horizons[i].RainProbability,.05,.95);
            Assert.True(high.Horizons[i].RainUpper-high.Horizons[i].RainLower
                <low.Horizons[i].RainUpper-low.Horizons[i].RainLower);
        }
        // Generate two different diagnostic future paths after the frozen observation.
        Assert.NotEqual(CircuitWeather.Observe(new(),1,900),CircuitWeather.Observe(new(),2,900));
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(low),
            System.Text.Json.JsonSerializer.Serialize(CircuitWeather.Forecast(observed,"A",0)));
    }

    [Fact]
    public void Disabled_weather_omits_extensions_and_supplied_track_ordinal_is_versioned()
    {
        var race=new RaceEngine(new(10,new("Dry")),[new("a","A")],17);
        Assert.DoesNotContain("Weather",System.Text.Json.JsonSerializer.Serialize(race.GetState()));
        Assert.DoesNotContain("Forecast",System.Text.Json.JsonSerializer.Serialize(race.GetTeamObservation("A")));
        var monza=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),10);
        var tuning=WeekendPresetCatalog.WeatherInputs(monza,new());
        Assert.Equal(.24,tuning.Volatility);
        Assert.Contains("1.0.0/2026.10.08-initial-balance/track-monza",tuning.SourceVersion);
        var weatherConfig=monza.Configuration with {Weather=tuning};
        Assert.EndsWith("+circuit-weather-v1",SimulationReplay.Model(weatherConfig));
        Assert.EndsWith("+weather-sha256-v1",SimulationReplay.Randomness(weatherConfig));
        Assert.Throws<ArgumentException>(()=>new RaceEngine(weatherConfig with {Weather=tuning with {ModelVersion="unknown"}},[new("a","A")],17));
    }
}

