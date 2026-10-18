using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Game.TestFixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;
public class WeatherTests
{
    private static WebApplicationFactory<Program> Server(ISessionStore store) => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.ConfigureServices(services => {
            services.AddSingleton<IRacePresets, WeatherRacePresets>(); services.AddSingleton(store);
        }));
    [Fact]
    public async Task Weather_and_private_forecast_survive_reads_signalr_retries_and_restart()
    {
        var store = new VolatileSessionStore();
        await using var server = Server(store);
        using var client = server.CreateClient();
        var created = (await (await client.PostAsJsonAsync("/api/sessions", new { preset="open-equal", laps=10, seed=17 }))
            .Content.ReadFromJsonAsync<SessionCreated>())!;
        var route = $"/api/sessions/{created.SessionId}";
        var initial = await client.GetFromJsonAsync<JsonElement>(route);
        Assert.Equal(.25, initial.GetProperty("weather").GetProperty("wetness").GetDouble());
        Assert.False(initial.TryGetProperty("forecast", out _));
        client.DefaultRequestHeaders.Authorization = new("Bearer", created.ControllerCapability);
        var own = await client.GetFromJsonAsync<JsonElement>(route+"/team");
        var forecast = own.GetProperty("forecast").GetRawText();
        Assert.Equal(forecast, (await client.GetFromJsonAsync<JsonElement>(route+"/team")).GetProperty("forecast").GetRawText());
        await using var hub = new HubConnectionBuilder().WithUrl(new Uri(server.Server.BaseAddress,"/hubs/race"), options => {
            options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler(); }).Build();
        await hub.StartAsync();
        var subscribed = await hub.InvokeAsync<JsonElement>("SubscribeTeam", created.SessionId, created.ControllerCapability);
        Assert.Equal(forecast, subscribed.GetProperty("forecast").GetRawText());
        var command = new { commandId=Guid.NewGuid().ToString(), phase=0, revision=1,
            cars=new[]{new {driverId="driver-01",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"},
                new {driverId="driver-02",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"}} };
        var accepted = await client.PostAsJsonAsync(route+"/plans",command); accepted.EnsureSuccessStatusCode();
        var after = await client.GetFromJsonAsync<JsonElement>(route);
        var expected = after.GetProperty("weather").GetRawText();
        Assert.True(after.GetProperty("weather").GetProperty("time").GetDouble()>0);
        (await client.PostAsJsonAsync(route+"/plans",command)).EnsureSuccessStatusCode();
        Assert.Equal(expected,(await client.GetFromJsonAsync<JsonElement>(route)).GetProperty("weather").GetRawText());
        var afterForecast=(await client.GetFromJsonAsync<JsonElement>(route+"/team")).GetProperty("forecast").GetRawText();
        await using var recovered=Server(store); using var recoveredClient=recovered.CreateClient();
        Assert.Equal(expected,(await recoveredClient.GetFromJsonAsync<JsonElement>(route)).GetProperty("weather").GetRawText());
        recoveredClient.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        Assert.Equal(afterForecast,(await recoveredClient.GetFromJsonAsync<JsonElement>(route+"/team")).GetProperty("forecast").GetRawText());
    }
}
