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
