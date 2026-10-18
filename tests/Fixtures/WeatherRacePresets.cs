using Game.Application;
using Game.Domain;
namespace Game.TestFixtures;
public sealed class WeatherRacePresets : IRacePresets
{
    public RacePreset Get(string name, int laps)
    {
        var dry = new DryRacePresets().Get(name, laps);
        return dry with { Configuration = dry.Configuration with {
            Weather = new(MeanRain: .6, InitialWetness: .25, SourceVersion: "weather-browser-v1") } };
    }
}
public sealed class WetTyreRacePresets : IRacePresets
{
    public RacePreset Get(string name, int laps)
    {
        var weather = new WeatherRacePresets().Get(name, laps);
        return weather with { Configuration = weather.Configuration with { WetTyres = new() } };
    }
}
public sealed class WeatherReactionRacePresets : IRacePresets
{
    public RacePreset Get(string name,int laps)
    {
        var preset=new WetTyreRacePresets().Get(name,laps);
        return preset with{Configuration=preset.Configuration with{
            Weather=name=="restricted-equal"?new(MeanRain:0,Volatility:0,InitialWetness:.8,DryingPerSecond:.01,SourceVersion:"weather-reaction-drying-browser-v1")
                :new(MeanRain:1,Volatility:0,WettingPerSecond:.01,SourceVersion:"weather-reaction-browser-v1"),
            WeatherReactions=new(DryBoundary:.2,Hysteresis:.02)}};
    }
}
