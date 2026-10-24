using Game.Application;
using Game.Domain;
using Game.Simulation;
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
// Guarded lab fixture: align an observed mechanical interruption and a weather edge.
// The probe uses the public simulation boundary and zero weather effects; no production path uses it.
public sealed class WeatherAdviceRacePresets:IRacePresets
{
    public RacePreset Get(string name,int laps)
    {
        if(name!="open-mixed")return new WeatherReactionRacePresets().Get(name,laps);
        var preset=new SafetyCarWindowPresets().Get("restricted-equal",laps);
        var configuration=preset.Configuration with{Incidents=new(FailureRate:.001,ComponentWearPerSecond:0,InterruptMajorIncidents:true)};
        var probe=new RaceEngine(configuration,preset.Drivers,123);
        var interruption=probe.ResolvePhase(preset.Drivers.ToDictionary(d=>d.Id,_=>new DriverDecision())).Interruption
            ??throw new InvalidOperationException("Mixed weather fixture must interrupt.");
        return preset with{Configuration=configuration with{
            Weather=new(MeanRain:1,Volatility:0,WettingPerSecond:.22/interruption.Time),
            WetTyres=new(DryUnsuitabilitySeconds:0,IntermediateUnsuitabilitySeconds:0,WetUnsuitabilitySeconds:0,WrongConditionWear:0,WetSkillSeconds:0,WetErrorMultiplier:0),
            WeatherReactions=new(DryBoundary:.2,Hysteresis:.02)}};
    }
}
