using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public class WeatherWeekendProfileTests
{
    [Fact]
    public void Complete_weekend_profiles_freeze_actual_track_and_keep_exogenous_identity_independent_of_quality()
    {
        var monza=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),10);
        var monaco=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monaco"),10);
        foreach(var profile in new[]{"all-dry","steady-wet","rain-arrival","drying","changeable"})
        {
            var first=WeatherWeekendProfiles.Apply(monza,profile);
            var second=WeatherWeekendProfiles.Apply(monaco,profile);
            Assert.Contains("track-monza",first.SourceVersion);
            Assert.Contains("track-monaco",second.SourceVersion);
            Assert.Equal(profile,WeatherWeekendProfiles.GetProfile(first));
            Assert.Equal(CircuitWeather.Observe(first,101,900),CircuitWeather.Observe(first with{InformationQuality=1},101,900));
        }
        var dry=WeatherWeekendProfiles.Apply(monza,"all-dry");
        Assert.Equal(0,CircuitWeather.Observe(dry,101,5000).Wetness);
        var drying=WeatherWeekendProfiles.Apply(monza,"drying");
        Assert.True(CircuitWeather.Observe(drying,101,1500).Wetness<CircuitWeather.Observe(drying,101,540).Wetness);
        Assert.Throws<ArgumentException>(()=>WeatherWeekendProfiles.Apply(monza,"unknown"));
    }
}
