using Game.Application;
using Game.Domain;
using Game.Simulation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
namespace Game.Api.Tests;
public sealed class SafetyCarWindowTests
{
    [Fact]
    public async Task Own_condition_bands_are_private_and_match_authenticated_signalr_observations()
    {
        await using var server=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.AddSingleton<IRacePresets>(new Game.TestFixtures.SafetyCarWindowPresets())));
        using var client=server.CreateClient();var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",10,123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);var route="/api/sessions/"+created.SessionId;
        var own=await client.GetFromJsonAsync<JsonElement>(route+"/team");
        foreach(var car in own.GetProperty("cars").EnumerateArray())
        {
            var condition=car.GetProperty("condition");Assert.Equal("Good",condition.GetProperty("componentCondition").GetString());Assert.Equal("Low",condition.GetProperty("risk").GetString());
            Assert.Equal(car.GetProperty("driverId").GetString(),condition.GetProperty("driverId").GetString());
            Assert.Equal(new[]{"driverId","componentCondition","risk","observedDamageSecondsPerLap","retired"},condition.EnumerateObject().Select(property=>property.Name));
        }
        await using var hub=new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();}).Build();
        await hub.StartAsync();var subscribed=await Microsoft.AspNetCore.SignalR.Client.HubConnectionExtensions.InvokeAsync<JsonElement>(hub,"SubscribeTeam",created.SessionId,created.ControllerCapability);
        Assert.Equal(own.GetProperty("cars").GetRawText(),subscribed.GetProperty("cars").GetRawText());
        var display=await Microsoft.AspNetCore.SignalR.Client.HubConnectionExtensions.InvokeAsync<JsonElement>(hub,"SubscribePublic",created.SessionId);
        Assert.DoesNotContain("componentCondition",display.GetRawText());Assert.DoesNotContain("componentCondition",await client.GetStringAsync(route));
        Assert.DoesNotContain("exposure",own.GetRawText(),StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("threshold",own.GetRawText(),StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void Legacy_staging_designer_replay_retains_commit_only_safety_car_semantics()
    {
        var config=new RaceConfiguration(20,new("Lab"),Incidents:new(ComponentWearPerSecond:0,InterruptMajorIncidents:true));
        Driver[] drivers=[new("a","A"),new("b","B",Gap:9)];var scenario=new IncidentScenario([new("a",IncidentKind.Spin,36)]);
        var engine=new RaceEngine(config,drivers,9,scenario);var normal=engine.ResolvePhase();
        var staged=engine.CommitSafetyCarChoices(new Dictionary<string,SafetyCarDecision>{["a"]=new(TyreCompound.Hard),["b"]=new()});
        var plan=new StrategyPlan([new(1,"a",new(PitCompound:TyreCompound.Hard)),new(1,"b",new())],[]);
        var replay=SimulationReplay.CaptureDiagnostic(config,drivers,plan,9,scenario);
        var record=new BrowserReplayRecord(BrowserReplay.LegacyDisruptionFormatVersion,"legacy","lab","A","dry-observation-ai-v2",replay,
            [new(0,normal.LeaderLap,normal.Time,false,BrowserReplay.OutcomeHash(normal)),new(1,staged.LeaderLap,staged.Time,false,BrowserReplay.OutcomeHash(staged))],false,OperationKinds:["Normal","SafetyCar"]);
        Assert.True(BrowserReplay.Verify(record).Matches);
        Assert.False(BrowserReplay.Verify(record with{FormatVersion=BrowserReplay.DisruptionFormatVersion}).Matches);
    }
    [Fact]
    public async Task Team_with_two_committed_stops_returns_to_normal_human_barrier_after_neutralised_running()
    {
        await using var server=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.AddSingleton<IRacePresets>(new Game.TestFixtures.SafetyCarWindowPresets())));
        using var client=server.CreateClient();var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("open-equal",10,123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);var route="/api/sessions/"+created.SessionId;
        (await client.PostAsJsonAsync(route+"/plans",new TeamCommand(Guid.NewGuid().ToString(),0,1,[new("driver-01","Standard","Pit","Hard","Neutral"),new("driver-02","Standard","Pit","Hard","Neutral")]))).EnsureSuccessStatusCode();
        var state=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;var own=(await client.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;
        Assert.Equal("awaiting-decisions",state.Lifecycle);Assert.Null(own.WindowKind);Assert.Null(state.SafetyCar);
        Assert.Contains(state.Readiness!,team=>team.TeamId=="team-01"&&!team.Locked);
        Assert.All(own.Cars,car=>Assert.Equal("Hard",car.Compound));
        (await client.PostAsJsonAsync(route+"/plans",new TeamCommand(Guid.NewGuid().ToString(),own.Phase,own.Revision,
            own.Cars.Select(car=>new CarCommand(car.DriverId,"Standard","Stay Out",null,"Neutral")).ToArray()))).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task Team_without_live_cars_does_not_hold_the_special_barrier()
    {
        await using var server=new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.AddSingleton<IRacePresets>(new Game.TestFixtures.SafetyCarWindowPresets())));
        using var client=server.CreateClient();var created=(await(await client.PostAsJsonAsync("/api/sessions",new CreateSessionInput("restricted-equal",10,123))).Content.ReadFromJsonAsync<SessionCreated>())!;
        client.DefaultRequestHeaders.Authorization=new("Bearer",created.ControllerCapability);var route="/api/sessions/"+created.SessionId;
        (await client.PostAsJsonAsync(route+"/plans",new TeamCommand(Guid.NewGuid().ToString(),0,1,[new("driver-01","Standard","Stay Out",null,"Neutral"),new("driver-02","Standard","Stay Out",null,"Neutral")]))).EnsureSuccessStatusCode();
        var state=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;Assert.Equal("awaiting-decisions",state.Lifecycle);Assert.Empty(state.Readiness!);
        Assert.Equal(2,state.Classification.Count(car=>car.TeamId=="team-01"&&car.PitState=="retired"));Assert.Null(state.SafetyCar);
        var own=(await client.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;
        (await client.PostAsJsonAsync(route+"/plans",new TeamCommand(Guid.NewGuid().ToString(),own.Phase,own.Revision,[]))).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task Shared_window_freezes_allowance_and_deadline_fills_only_missing_team_preserving_recovered_lock()
    {
        var clock=new DeadlineTests.ControlledClock();var store=new VolatileSessionStore();
        WebApplicationFactory<Program> Server()=>DeadlineTests.Server(clock).WithWebHostBuilder(builder=>builder.ConfigureServices(services=>{
            services.AddSingleton<IRacePresets>(new Game.TestFixtures.SafetyCarWindowPresets());services.AddSingleton<ISessionStore>(store);
        }));
        await using var server=Server();using var client=server.CreateClient();var(id,device)=await DeadlineTests.StartShared(client);var route="/api/lobbies/"+id;
        var lobby=(await client.GetFromJsonAsync<LobbySnapshot>(route))!;
        foreach(var team in lobby.Teams.Where(team=>team.TeamId is "team-01" or "team-02"))
            (await client.PostAsJsonAsync($"{route}/players/{team.PlayerId}/assistance",new PlayerAssistanceInput("Assisted",true))).EnsureSuccessStatusCode();
        foreach(var team in new[]{"team-01","team-02"})
        {
            var own=(await client.GetFromJsonAsync<TeamSnapshot>($"{route}/teams/{team}?deviceId={device}"))!;
            (await client.PostAsJsonAsync($"{route}/teams/{team}/plans?deviceId={device}",new TeamCommand(Guid.NewGuid().ToString(),own.Phase,own.CommandRevision,
                own.Cars.Select(car=>new CarCommand(car.DriverId,"Standard","Stay Out",null,"Neutral")).ToArray()))).EnsureSuccessStatusCode();
        }
        var interrupted=(await client.GetFromJsonAsync<PublicSnapshot>("/api/sessions/"+id))!;
        Assert.NotNull(interrupted.SafetyCar);Assert.Equal(120,interrupted.TimingPolicy!.EffectiveSeconds);Assert.Equal(1,interrupted.TimingPolicy.AdditionalSharedTeams);
        var first=(await client.GetFromJsonAsync<TeamSnapshot>($"{route}/teams/team-01?deviceId={device}"))!;
        Assert.NotNull(first.Assistance!.Recommendation);Assert.Empty(first.Assistance.Recommendation.Cars);
        var adviceJson=JsonSerializer.Serialize(first.Assistance.Recommendation,BrowserReplay.JsonOptions);
        Assert.Equal(2,JsonDocument.Parse(adviceJson).RootElement.GetProperty("safetyCarCars").GetArrayLength());
        Assert.DoesNotContain("pace",adviceJson,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("battle",adviceJson,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("recommendation",await client.GetStringAsync("/api/sessions/"+id),StringComparison.OrdinalIgnoreCase);
        var command=new SafetyCarTeamCommand(Guid.NewGuid().ToString(),first.Phase,first.CommandRevision,[new("driver-01","Pit","Hard"),new("driver-02","Stay Out",null)]);
        var accepted=await client.PostAsJsonAsync($"{route}/teams/team-01/safety-car-plans?deviceId={device}",command);accepted.EnsureSuccessStatusCode();
        await using var recovered=Server();using var restored=recovered.CreateClient();restored.DefaultRequestHeaders.Authorization=client.DefaultRequestHeaders.Authorization;
        var saved=(await restored.GetFromJsonAsync<TeamSnapshot>($"{route}/teams/team-01?deviceId={device}"))!;Assert.NotNull(saved.ConfirmedPlan);
        Assert.Equal(adviceJson,JsonSerializer.Serialize(saved.Assistance!.Recommendation,BrowserReplay.JsonOptions));
        var before=(await restored.GetFromJsonAsync<PublicSnapshot>("/api/sessions/"+id))!;Assert.Equal(interrupted.DecisionDeadlineUtc,before.DecisionDeadlineUtc);
        Assert.Contains(before.Readiness!,team=>team.TeamId=="team-01"&&team.Locked);
        clock.Advance(TimeSpan.FromSeconds(120));var due=new DueTransitionInput(Guid.NewGuid().ToString(),before.Phase,before.WindowRevision);
        var closed=await restored.PostAsJsonAsync(route+"/advance-due",due);closed.EnsureSuccessStatusCode();var staged=(await closed.Content.ReadFromJsonAsync<PublicSnapshot>())!;
        Assert.Equal("phase-result",staged.Lifecycle);Assert.Equal(before.RaceTime+180,staged.RaceTime,7);
        Assert.Contains(staged.DecisionSources!,source=>source.TeamId=="team-01"&&source.Source=="human");Assert.Contains(staged.DecisionSources!,source=>source.TeamId=="team-02"&&source.Source=="recommendation-timeout");
        var ownAfter=(await restored.GetFromJsonAsync<TeamSnapshot>($"{route}/teams/team-01?deviceId={device}"))!;Assert.Equal("Hard",ownAfter.Cars[0].Compound);
        var retry=await restored.PostAsJsonAsync($"{route}/teams/team-01/safety-car-plans?deviceId={device}",command);retry.EnsureSuccessStatusCode();Assert.Equal(await accepted.Content.ReadAsStringAsync(),await retry.Content.ReadAsStringAsync());
        var repeated=(await(await restored.PostAsJsonAsync(route+"/advance-due",due)).Content.ReadFromJsonAsync<PublicSnapshot>())!;Assert.Equal(staged.Revision,repeated.Revision);
    }
    [Fact]
    public async Task Incident_window_resolves_actual_neutralised_running_once_and_recovery_preserves_restart()
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
        var staged=(await client.GetFromJsonAsync<PublicSnapshot>(route))!;Assert.Equal(interrupted.RaceTime+180,staged.RaceTime,7);Assert.Equal("awaiting-decisions",staged.Lifecycle);Assert.Null(staged.SafetyCar);
        await using var recovered=Server();using var restored=recovered.CreateClient();restored.DefaultRequestHeaders.Authorization=client.DefaultRequestHeaders.Authorization;
        var saved=(await restored.GetFromJsonAsync<TeamSnapshot>(route+"/team"))!;
        Assert.Equal("Hard",saved.Cars.Single(car=>car.DriverId=="driver-01").Compound);Assert.Null(saved.ConfirmedPlan);Assert.Null(saved.WindowKind);
        Assert.Null((await restored.GetFromJsonAsync<PublicSnapshot>(route))!.SafetyCar);
        var json=await restored.GetStringAsync(route);Assert.DoesNotContain("Exposure",json,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("ComponentCondition",json,StringComparison.OrdinalIgnoreCase);
        restored.DefaultRequestHeaders.Add("X-Designer-Key","s09-window-replay");var replay=await restored.GetFromJsonAsync<JsonElement>("/api/designer/sessions/"+created.SessionId+"/record");
        Assert.Equal(BrowserReplay.DisruptionFormatVersion,replay.GetProperty("formatVersion").GetString());
        var verified=await restored.PostAsJsonAsync("/api/designer/replay/verify",replay);verified.EnsureSuccessStatusCode();Assert.True((await verified.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("matches").GetBoolean());
    }
}
