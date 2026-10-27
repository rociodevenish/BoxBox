using Game.Domain;
namespace Game.Simulation;

/// <summary>Authored complete-weekend hypotheses; the profile is host configuration, never a player command.</summary>
public static class WeatherWeekendProfiles
{
    public const string Version="weather-weekend-presets-v1";
    public static string? GetProfile(WeatherTuning tuning)=>tuning.SourceVersion.StartsWith(Version+"/",StringComparison.Ordinal)
        ?tuning.SourceVersion.Split('/')[1]:null;
    public static WeatherTuning Apply(EffectiveWeekend weekend,string profile)
    {
        var mapped=WeekendPresetCatalog.WeatherInputs(weekend,new());
        var tune=profile switch
        {
            "all-dry"=>mapped with{MeanRain=0,Volatility=0,InitialWetness=0},
            "steady-wet"=>mapped with{MeanRain=.95,Volatility=mapped.Volatility*.1,InitialWetness=.85},
            // Rain begins immediately, but meaningful surface wetness arrives during the weekend.
            "rain-arrival"=>mapped with{MeanRain=.6,InitialWetness=0,WettingPerSecond=.0005,DryingPerSecond=.00015},
            "drying"=>mapped with{MeanRain=0,Volatility=0,InitialWetness=.95,DryingPerSecond=.0005},
            "changeable"=>mapped with{MeanRain=.35,InitialWetness=.45,WettingPerSecond=.001,DryingPerSecond=.00054,IntervalSeconds=180},
            _=>throw new ArgumentException("Unknown versioned weather weekend profile.")
        };
        return tune with{SourceVersion=Version+"/"+profile+"/"+mapped.SourceVersion};
    }
}
