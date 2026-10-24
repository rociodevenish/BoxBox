using Game.Simulation;
namespace Game.Application;

/// <summary>Host-owned weather activation retaining the four public race preset names.</summary>
public sealed class WeatherWeekendPresets : IRacePresets
{
    public const string ContentVersion=WeatherWeekendProfiles.Version;
    private readonly string profile;
    public WeatherWeekendPresets(string profile)
    {
        // Validate at host startup, before accepting sessions.
        WeatherWeekendProfiles.Apply(WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),10),profile);
        this.profile=profile;
    }
    public RacePreset Get(string name,int laps)
    {
        var dry=new DryRacePresets().Get(name,laps);
        var effective=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),laps);
        return dry with{Configuration=dry.Configuration with{
            Weather=WeatherWeekendProfiles.Apply(effective,profile),WetTyres=new(),WeatherReactions=new()}};
    }
}
