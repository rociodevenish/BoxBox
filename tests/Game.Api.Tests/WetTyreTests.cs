using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Game.TestFixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;
public class WetTyreTests
{
    [Fact]
    public async Task Both_wet_compounds_are_committed_retried_and_recovered_without_extra_stops_or_private_data()
    {
        var store=new VolatileSessionStore();
        WebApplicationFactory<Program> Server()=>new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>
            builder.ConfigureServices(services=>{services.AddSingleton<IRacePresets,WetTyreRacePresets>();services.AddSingleton<ISessionStore>(store);}));
        await using var server=Server();using var client=server.CreateClient();
        var created=(await (await client.PostAsJsonAsync("/api/sessions",new{preset="open-equal",laps=10,seed=17})).Content.ReadFromJsonAsync<SessionCreated>())!;
        var route=$"/api/sessions/{created.SessionId}";
        client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        var own=await client.GetFromJsonAsync<JsonElement>(route+"/team");
        Assert.True(own.GetProperty("wetTyresEnabled").GetBoolean());
        var command=new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars=new[]{
            new{driverId="driver-01",pace="Standard",pit="Pit",compound="Intermediate",battle="Neutral"},
            new{driverId="driver-02",pace="Standard",pit="Pit",compound="Wet",battle="Neutral"}}};
        (await client.PostAsJsonAsync(route+"/plans",command)).EnsureSuccessStatusCode();
        var after=await client.GetFromJsonAsync<JsonElement>(route);
        var rows=after.GetProperty("classification").EnumerateArray().ToArray();
        Assert.Equal("Intermediate",rows.Single(r=>r.GetProperty("driverId").GetString()=="driver-01").GetProperty("compound").GetString());
        Assert.Equal("Wet",rows.Single(r=>r.GetProperty("driverId").GetString()=="driver-02").GetProperty("compound").GetString());
        Assert.All(rows.Where(r=>r.GetProperty("teamId").GetString()=="team-01"),r=>Assert.Equal(1,r.GetProperty("completedPitStops").GetInt32()));
        (await client.PostAsJsonAsync(route+"/plans",command)).EnsureSuccessStatusCode();
        await using var recovered=Server();using var recoveredClient=recovered.CreateClient();
        var replay=await recoveredClient.GetFromJsonAsync<JsonElement>(route);
        Assert.Equal(after.GetProperty("classification").GetRawText(),replay.GetProperty("classification").GetRawText());
        Assert.False(replay.TryGetProperty("forecast",out _));
        Assert.DoesNotContain("wetWeatherSkill",replay.GetRawText());
        Assert.DoesNotContain("ErrorExposure",replay.GetRawText());
    }
}
