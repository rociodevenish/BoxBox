using Game.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;
public sealed partial class WeatherActivationTests
{
    [Theory]
    [InlineData("all-dry")]
    [InlineData("steady-wet")]
    [InlineData("rain-arrival")]
    [InlineData("drying")]
    [InlineData("changeable")]
    public void Versioned_host_policy_enables_weather_for_existing_public_presets(string profile)
    {
        using var host=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder
            .UseSetting("Weather:Policy","weather-weekend-presets-v1").UseSetting("Weather:Profile",profile));
        using var client=host.CreateClient();
        var presets=host.Services.GetRequiredService<IRacePresets>();
        foreach(var name in new[]{"open-equal","restricted-equal","open-mixed","restricted-mixed"})
        {
            var race=presets.Get(name,10);
            Assert.NotNull(race.Configuration.Weather);
            Assert.StartsWith($"weather-weekend-presets-v1/{profile}/weather-track-",race.Configuration.Weather.SourceVersion);
            Assert.NotNull(race.Configuration.WetTyres);
            Assert.NotNull(race.Configuration.WeatherReactions);
            Assert.Equal(22,race.Drivers.Count);
        }
        Assert.Throws<ArgumentException>(()=>presets.Get("rain-arrival",10));
    }
}

public sealed partial class WeatherActivationTests
{
    [Fact]
    public void Unconfigured_host_preserves_dry_race_presets()
    {
        using var host=new WebApplicationFactory<Program>();using var client=host.CreateClient();
        var presets=host.Services.GetRequiredService<IRacePresets>();
        foreach(var name in new[]{"open-equal","restricted-equal","open-mixed","restricted-mixed"})
        {
            var configuration=presets.Get(name,10).Configuration;
            Assert.Null(configuration.Weather);Assert.Null(configuration.WetTyres);Assert.Null(configuration.WeatherReactions);
        }
    }
    [Theory]
    [InlineData("weather-weekend-presets-v2","all-dry")]
    [InlineData("weather-weekend-presets-v1","")]
    [InlineData("weather-weekend-presets-v1","unknown")]
    [InlineData("","steady-wet")]
    public void Invalid_or_incomplete_weather_configuration_fails_before_serving(string policy,string profile)
    {
        using var host=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder
            .UseSetting("Weather:Policy",policy).UseSetting("Weather:Profile",profile));
        Assert.ThrowsAny<Exception>(()=>host.CreateClient());
    }
}
