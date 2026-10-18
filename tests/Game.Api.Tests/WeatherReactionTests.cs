using System.Net.Http.Json;
using Game.Application;
using Game.TestFixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
namespace Game.Api.Tests;

public class WeatherReactionTests
{
    [Fact]
    public async Task Household_weather_deadline_and_partial_lock_survive_durable_restart_and_fill_only_missing_team()
    {
        var clock=new DeadlineTests.ControlledClock();
        WebApplicationFactory<Program> Durable()=>DeadlineTests.Server(clock,true).WithWebHostBuilder(builder=>
            builder.ConfigureServices(services=>services.AddSingleton<IRacePresets,WeatherReactionRacePresets>()));
        await using var server=Durable();using var client=server.CreateClient();
        var(id,device)=await DeadlineTests.StartShared(client);var route="/api/lobbies/"+id;
        foreach(var team in new[]{"team-01","team-02"})
        {
            var own=(await client.GetFromJsonAsync<TeamSnapshot>($"{route}/teams/{team}?deviceId={device}"))!;
            var normal=new TeamCommand(Guid.NewGuid().ToString(),own.Phase,own.CommandRevision,
                own.Cars.Select(car=>new CarCommand(car.DriverId,"Standard","Stay Out",null,"Neutral")).ToArray());
            (await client.PostAsJsonAsync($"{route}/teams/{team}/plans?deviceId={device}",normal)).EnsureSuccessStatusCode();
        }
        var before=(await client.GetFromJsonAsync<PublicSnapshot>("/api/sessions/"+id))!;
        Assert.Equal(22,before.RaceTime,8);Assert.Equal(120,before.TimingPolicy!.EffectiveSeconds);
        var first=(await client.GetFromJsonAsync<TeamSnapshot>($"{route}/teams/team-01?deviceId={device}"))!;
        var acceptedCommand=new TeamCommand(Guid.NewGuid().ToString(),first.Phase,first.CommandRevision,
            first.Cars.Select(car=>new CarCommand(car.DriverId,"Push","Stay Out",null,"Neutral")).ToArray(),"Weather");
        var accepted=await client.PostAsJsonAsync($"{route}/teams/team-01/plans?deviceId={device}",acceptedCommand);accepted.EnsureSuccessStatusCode();
        await using var recovered=Durable();using var restored=recovered.CreateClient();restored.DefaultRequestHeaders.Authorization=client.DefaultRequestHeaders.Authorization;
        var saved=(await restored.GetFromJsonAsync<PublicSnapshot>("/api/sessions/"+id))!;
        Assert.Equal(before.DecisionDeadlineUtc,saved.DecisionDeadlineUtc);Assert.Equal(before.WeatherReaction!.Identity,saved.WeatherReaction!.Identity);
        Assert.Contains(saved.Readiness!,team=>team.TeamId=="team-01"&&team.Locked);
        clock.Advance(TimeSpan.FromSeconds(120));
        var due=new DueTransitionInput(Guid.NewGuid().ToString(),saved.Phase,saved.WindowRevision);
        var closed=await restored.PostAsJsonAsync(route+"/advance-due",due);closed.EnsureSuccessStatusCode();
        var after=(await closed.Content.ReadFromJsonAsync<PublicSnapshot>())!;
        Assert.Equal(72,after.RaceTime,8);
        Assert.Contains(after.DecisionSources!,source=>source.TeamId=="team-01"&&source.Source=="human");
        Assert.Contains(after.DecisionSources!,source=>source.TeamId=="team-02"&&source.Source=="timeout-ai");
        var retry=await restored.PostAsJsonAsync($"{route}/teams/team-01/plans?deviceId={device}",acceptedCommand);retry.EnsureSuccessStatusCode();
        Assert.Equal(await accepted.Content.ReadAsStringAsync(),await retry.Content.ReadAsStringAsync());
        var repeated=(await(await restored.PostAsJsonAsync(route+"/advance-due",due)).Content.ReadFromJsonAsync<PublicSnapshot>())!;
        Assert.Equal(after.Revision,repeated.Revision);
    }

    private static WebApplicationFactory<Program> Server(ISessionStore store)=>new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder=>builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>{{"Designer:Key","weather-test"}}))
            .ConfigureServices(services=>{services.AddSingleton<IRacePresets,WeatherReactionRacePresets>();services.AddSingleton(store);}));
    [Fact]
    public async Task Complete_weather_window_retries_recovers_and_replays_without_reroll_or_duplicate_stop()
    {
        var store=new VolatileSessionStore();await using var server=Server(store);using var client=server.CreateClient();
        var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",10,17))).Content.ReadFromJsonAsync<SessionCreated>())!;
        var route="/api/sessions/"+created.SessionId;client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        var initial=(await client.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;
        TeamCommand Command(TeamSnapshot team,string? kind=null)=>new(Guid.NewGuid().ToString(),team.Phase,team.Revision,
            team.Cars.Select(car=>new CarCommand(car.DriverId,"Standard","Stay Out",null,"Neutral")).ToArray(),kind);
        (await client.PostAsJsonAsync(route+"/plans",Command(initial))).EnsureSuccessStatusCode();
        var observed=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;
        var own=(await client.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;
        Assert.Equal(22,observed.RaceTime,8);Assert.Equal("Weather",own.WindowKind);Assert.Equal("awaiting-decisions",own.Lifecycle);
        Assert.Equal(new[]{"Weather"},observed.WeatherReaction!.Causes);
        await using var hub=new HubConnectionBuilder().WithUrl(new Uri(server.Server.BaseAddress,"/hubs/race"),options=>{
            options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();}).Build();
        await hub.StartAsync();
        var broadcast=await hub.InvokeAsync<PublicSnapshot>("SubscribePublic",created.SessionId);
        var privateState=await hub.InvokeAsync<TeamSnapshot>("SubscribeTeam",created.SessionId,created.ControllerCapability);
        Assert.Equal(observed.WeatherReaction.Identity,broadcast.WeatherReaction!.Identity);Assert.Equal("Weather",privateState.WindowKind);
        await using var restored=Server(store);using var recovery=restored.CreateClient();recovery.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);
        var recovered=(await recovery.GetFromJsonAsync<PublicSnapshot>(route))!;
        Assert.Equal(observed.WeatherReaction!.Identity,recovered.WeatherReaction!.Identity);Assert.Equal(observed.Weather,recovered.Weather);
        var next=Command(own,"Weather");
        next=next with{Cars=next.Cars.Select(car=>car with{Pace="Push",Pit="Pit",Compound="Intermediate"}).ToArray()};
        var accepted=await client.PostAsJsonAsync(route+"/plans",next);accepted.EnsureSuccessStatusCode();
        var result=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;Assert.Equal(72,result.RaceTime,6);
        var again=await client.PostAsJsonAsync(route+"/plans",next);again.EnsureSuccessStatusCode();
        Assert.Equal(result.RaceTime,(await client.GetFromJsonAsync<PublicSnapshot>(route))!.RaceTime);
        client.DefaultRequestHeaders.Add("X-Designer-Key","weather-test");
        var export=await client.GetFromJsonAsync<JsonElement>($"/api/designer/sessions/{created.SessionId}/record");
        Assert.True(BrowserReplay.Verify(BrowserReplay.Read(export)).Matches);
    }
}
