using Game.Application;
using Game.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
using Xunit;
namespace Game.Api.Tests;
public sealed class SafetyCarWindowTests
{
    [Fact]
    public async Task Team_without_live_cars_does_not_hold_the_special_barrier()
    {
        await using var server=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.AddSingleton<IRacePresets>(new Game.TestFixtures.SafetyCarWindowPresets())));
        using var client=server.CreateClient();var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("restricted-equal",10,123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);var route="/api/sessions/"+created.SessionId;
        (await client.PostAsJsonAsync(route+"/plans",new TeamCommand(Guid.NewGuid().ToString(),0,1,[new("driver-01","Standard","Stay Out",null,"Neutral"),new("driver-02","Standard","Stay Out",null,"Neutral")]))).EnsureSuccessStatusCode();
        var state=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;Assert.Equal("safety-car-ready",state.Lifecycle);Assert.Empty(state.Readiness!);
        Assert.Equal(2,state.Classification.Count(car=>car.TeamId=="team-01"&&car.PitState=="retired"));Assert.Equal(state.SafetyCar!.Time,state.RaceTime);
    }
    [Fact]
    public async Task Shared_window_freezes_allowance_and_deadline_fills_only_missing_team_preserving_recovered_lock()
    {
        var clock=new DeadlineTests.ControlledClock();var store=new VolatileSessionStore();
        WebApplicationFactory<Program> Server()=>DeadlineTests.Server(clock).WithWebHostBuilder(builder=>builder.ConfigureServices(services=>{
            services.AddSingleton<IRacePresets>(new Game.TestFixtures.SafetyCarWindowPresets());services.AddSingleton<ISessionStore>(store);
        }));
        await using var server=Server();using var client=server.CreateClient();var(id,device)=await DeadlineTests.StartShared(client);var route="/api/lobbies/"+id;
        foreach(var team in new[]{"team-01","team-02"})
        {
            var own=(await client.GetFromJsonAsync<TeamSnapshot>($"{route}/teams/{team}?deviceId={device}"))!;
            (await client.PostAsJsonAsync($"{route}/teams/{team}/plans?deviceId={device}",new TeamCommand(Guid.NewGuid().ToString(),own.Phase,own.CommandRevision,
                own.Cars.Select(car=>new CarCommand(car.DriverId,"Standard","Stay Out",null,"Neutral")).ToArray()))).EnsureSuccessStatusCode();
        }
        var interrupted=(await client.GetFromJsonAsync<PublicSnapshot>("/api/sessions/"+id))!;
        Assert.NotNull(interrupted.SafetyCar);Assert.Equal(120,interrupted.TimingPolicy!.EffectiveSeconds);Assert.Equal(1,interrupted.TimingPolicy.AdditionalSharedTeams);
        var first=(await client.GetFromJsonAsync<TeamSnapshot>($"{route}/teams/team-01?deviceId={device}"))!;
        var command=new SafetyCarTeamCommand(Guid.NewGuid().ToString(),first.Phase,first.CommandRevision,[new("driver-01","Pit","Hard"),new("driver-02","Stay Out",null)]);
        var accepted=await client.PostAsJsonAsync($"{route}/teams/team-01/safety-car-plans?deviceId={device}",command);accepted.EnsureSuccessStatusCode();
        await using var recovered=Server();using var restored=recovered.CreateClient();restored.DefaultRequestHeaders.Authorization=client.DefaultRequestHeaders.Authorization;
        var saved=(await restored.GetFromJsonAsync<TeamSnapshot>($"{route}/teams/team-01?deviceId={device}"))!;Assert.NotNull(saved.ConfirmedPlan);
        var before=(await restored.GetFromJsonAsync<PublicSnapshot>("/api/sessions/"+id))!;Assert.Equal(interrupted.DecisionDeadlineUtc,before.DecisionDeadlineUtc);
        Assert.Contains(before.Readiness!,team=>team.TeamId=="team-01"&&team.Locked);
        clock.Advance(TimeSpan.FromSeconds(120));var due=new DueTransitionInput(Guid.NewGuid().ToString(),before.Phase,before.WindowRevision);
        var closed=await restored.PostAsJsonAsync(route+"/advance-due",due);closed.EnsureSuccessStatusCode();var staged=(await closed.Content.ReadFromJsonAsync<PublicSnapshot>())!;
        Assert.Equal("safety-car-ready",staged.Lifecycle);Assert.Equal(before.RaceTime,staged.RaceTime);
        Assert.Contains(staged.DecisionSources!,source=>source.TeamId=="team-01"&&source.Source=="human");Assert.Contains(staged.DecisionSources!,source=>source.TeamId=="team-02"&&source.Source=="timeout-ai");
        var ownAfter=(await restored.GetFromJsonAsync<TeamSnapshot>($"{route}/teams/team-01?deviceId={device}"))!;Assert.Equal(TyreCompound.Hard,ownAfter.Cars[0].SafetyCar!.CommittedCompound);
        var retry=await restored.PostAsJsonAsync($"{route}/teams/team-01/safety-car-plans?deviceId={device}",command);retry.EnsureSuccessStatusCode();Assert.Equal(await accepted.Content.ReadAsStringAsync(),await retry.Content.ReadAsStringAsync());
        var repeated=(await(await restored.PostAsJsonAsync(route+"/advance-due",due)).Content.ReadFromJsonAsync<PublicSnapshot>())!;Assert.Equal(staged.Revision,repeated.Revision);
    }
    [Fact]
    public async Task Incident_opens_one_narrow_window_and_retry_recovery_preserves_committed_choices_without_running_time()
    {
        var store=new VolatileSessionStore();
        WebApplicationFactory<Program> Server()=>new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>{builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>{["Designer:Key"]="s09-window-replay"}));builder.ConfigureServices(services=>{
            services.AddSingleton<IRacePresets>(new Game.TestFixtures.SafetyCarWindowPresets());services.AddSingleton<ISessionStore>(store);
        });});
        await using var server=Server();using var client=server.CreateClient();
        var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",10,123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);var route="/api/sessions/"+created.SessionId;
        var normal=new TeamCommand(Guid.NewGuid().ToString(),0,1,[new("driver-01","Standard","Stay Out",null,"Neutral"),new("driver-02","Standard","Stay Out",null,"Neutral")]);
        (await client.PostAsJsonAsync(route+"/plans",normal)).EnsureSuccessStatusCode();
        var interrupted=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;var team=(await client.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;
        Assert.NotNull(interrupted.SafetyCar);Assert.Equal("SafetyCar",team.WindowKind);Assert.Equal("awaiting-decisions",team.Lifecycle);
        Assert.True(interrupted.RaceTime<450);Assert.All(team.Cars,car=>Assert.NotNull(car.SafetyCar));
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(route+"/plans",normal with{CommandId=Guid.NewGuid().ToString(),Phase=team.Phase,Revision=team.Revision})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(route+"/safety-car-plans",new{commandId=Guid.NewGuid().ToString(),phase=team.Phase,revision=team.Revision,cars=new[]{new{driverId="driver-01",pit="Stay Out",compound=(string?)null,pace="Push"}}})).StatusCode);
        var command=new SafetyCarTeamCommand(Guid.NewGuid().ToString(),team.Phase,team.Revision,[new("driver-01","Pit","Hard"),new("driver-02","Stay Out",null)]);
        var first=await client.PostAsJsonAsync(route+"/safety-car-plans",command);first.EnsureSuccessStatusCode();
        var retry=await client.PostAsJsonAsync(route+"/safety-car-plans",command);retry.EnsureSuccessStatusCode();Assert.Equal(await first.Content.ReadAsStringAsync(),await retry.Content.ReadAsStringAsync());
        var staged=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;Assert.Equal(interrupted.RaceTime,staged.RaceTime);Assert.Equal("safety-car-ready",staged.Lifecycle);
        await using var recovered=Server();using var restored=recovered.CreateClient();restored.DefaultRequestHeaders.Authorization=client.DefaultRequestHeaders.Authorization;
        var saved=(await restored.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;
        Assert.Equal("queued",saved.Cars.Single(car=>car.DriverId=="driver-01").SafetyCar!.Status);Assert.NotNull(saved.ConfirmedPlan);
        Assert.Equal(interrupted.SafetyCar,(await restored.GetFromJsonAsync<PublicSnapshot>(route))!.SafetyCar);
        var json=await restored.GetStringAsync(route);Assert.DoesNotContain("Exposure",json,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("ComponentCondition",json,StringComparison.OrdinalIgnoreCase);
        restored.DefaultRequestHeaders.Add("X-Designer-Key","s09-window-replay");var replay=await restored.GetFromJsonAsync<JsonElement>("/api/designer/sessions/"+created.SessionId+"/record");
        Assert.Equal(BrowserReplay.DisruptionFormatVersion,replay.GetProperty("formatVersion").GetString());
        var verified=await restored.PostAsJsonAsync("/api/designer/replay/verify",replay);verified.EnsureSuccessStatusCode();Assert.True((await verified.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("matches").GetBoolean());
    }
}

