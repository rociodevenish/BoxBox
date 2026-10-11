using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
namespace Game.Api.Tests;
public sealed class AssistanceTests
{
    [Fact]
    public async Task Per_player_guided_advice_is_frozen_private_and_standard_has_no_recommendation()
    {
        var clock=new DeadlineTests.ControlledClock();await using var server=Server().WithWebHostBuilder(b=>b.ConfigureServices(services=>services.AddSingleton<TimeProvider>(clock)));using var client=server.CreateClient();
        var (id,device,player)=await Setup(client);
        var configured=await client.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Guided",autoAcceptRecommendation=true});
        configured.EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
        var route=$"/api/lobbies/{id}/teams/team-01?deviceId={device}";
        var first=await client.GetFromJsonAsync<JsonElement>(route);
        var help=first.GetProperty("assistance");Assert.Equal("Guided",help.GetProperty("helpLevel").GetString());
        Assert.Equal(2,help.GetProperty("recommendation").GetProperty("cars").GetArrayLength());
        Assert.Equal(help.GetRawText(),(await client.GetFromJsonAsync<JsonElement>(route)).GetProperty("assistance").GetRawText());
        Assert.DoesNotContain("recommendation",await client.GetStringAsync($"/api/sessions/{id}"),StringComparison.OrdinalIgnoreCase);
        var changed=await client.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Standard",autoAcceptRecommendation=false});changed.EnsureSuccessStatusCode();
        Assert.Equal(help.GetRawText(),(await client.GetFromJsonAsync<JsonElement>(route)).GetProperty("assistance").GetRawText());
        clock.Advance(TimeSpan.FromSeconds(91));
        var closed=await(await client.PostAsJsonAsync($"/api/lobbies/{id}/advance-due",new{commandId=Guid.NewGuid().ToString(),phase=0,windowRevision=1})).Content.ReadFromJsonAsync<JsonElement>();
        if(closed.GetProperty("lifecycle").GetString()=="phase-result")
            (await client.PostAsJsonAsync($"/api/lobbies/{id}/continue",new{commandId=Guid.NewGuid().ToString(),phase=0,windowRevision=1})).EnsureSuccessStatusCode();
        var nextHelp=(await client.GetFromJsonAsync<JsonElement>(route)).GetProperty("assistance");Assert.Equal("Standard",nextHelp.GetProperty("helpLevel").GetString());Assert.False(nextHelp.TryGetProperty("recommendation",out _));
    }
    [Fact]
    public async Task Standard_never_exposes_advice_and_only_host_can_change_player_help()
    {
        await using var server=Server();using var host=server.CreateClient();var(id,device,player)=await Setup(host);
        using var other=server.CreateClient();other.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));
        Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await other.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Assisted",autoAcceptRecommendation=false})).StatusCode);
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
        var own=await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/teams/team-01?deviceId={device}");
        var help=own.GetProperty("assistance");Assert.Equal("Standard",help.GetProperty("helpLevel").GetString());Assert.False(help.TryGetProperty("recommendation",out _));
        Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await other.GetAsync($"/api/lobbies/{id}/teams/team-01?deviceId={device}")).StatusCode);
    }
    [Fact]
    public async Task Recommendation_and_explicit_timeout_acceptance_survive_restart_without_rerolling()
    {
        var clock=new DeadlineTests.ControlledClock();
        WebApplicationFactory<Program> Durable()=>Server().WithWebHostBuilder(b=>{
            b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"Persistence:ProjectId","demo-f1game"},{"Persistence:EmulatorHost",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")??"127.0.0.1:19100"},{"Designer:Key","assistance-evidence"}}));
            b.ConfigureServices(s=>s.AddSingleton<TimeProvider>(clock));});
        string id,device,token,savedAdvice;
        await using(var initial=Durable())
        {
            using var host=initial.CreateClient();var setup=await Setup(host);(id,device)=(setup.Id,setup.Device);token=host.DefaultRequestHeaders.Authorization!.Parameter!;
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/players/{setup.Player}/assistance",new{helpLevel="Assisted",autoAcceptRecommendation=true})).EnsureSuccessStatusCode();
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
            savedAdvice=(await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/teams/team-01?deviceId={device}")).GetProperty("assistance").GetRawText();
        }
        await using var restart=Durable();using var reader=restart.CreateClient();reader.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(savedAdvice,(await reader.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/teams/team-01?deviceId={device}")).GetProperty("assistance").GetRawText());
        clock.Advance(TimeSpan.FromSeconds(91));
        var state=await(await reader.PostAsJsonAsync($"/api/lobbies/{id}/advance-due",new{commandId=Guid.NewGuid().ToString(),phase=0,windowRevision=1})).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(state.GetProperty("decisionSources").EnumerateArray(),entry=>entry.GetProperty("source").GetString()=="recommendation-timeout");
        reader.DefaultRequestHeaders.Add("X-Designer-Key","assistance-evidence");
        var replay=await reader.GetFromJsonAsync<JsonElement>($"/api/designer/sessions/{id}/record");
        using var advice=JsonDocument.Parse(savedAdvice);
        foreach(var car in advice.RootElement.GetProperty("recommendation").GetProperty("cars").EnumerateArray())
        {
            var decision=replay.GetProperty("replay").GetProperty("plan").GetProperty("scheduled").EnumerateArray().Single(d=>d.GetProperty("phase").GetInt32()==0&&d.GetProperty("driverId").GetString()==car.GetProperty("driverId").GetString()).GetProperty("decision");
            var suggested=car.GetProperty("suggested");Assert.Equal(suggested.GetProperty("pace").GetString(),decision.GetProperty("pace").GetString());Assert.Equal(suggested.GetProperty("compound").GetString(),decision.GetProperty("pitCompound").GetString());Assert.Equal(suggested.GetProperty("battle").GetString(),decision.GetProperty("battle").GetString());
        }
    }
    [Fact]
    public async Task Intentionally_open_guided_advice_respects_a_stop_queued_across_the_phase_boundary()
    {
        await using var server=Server().WithWebHostBuilder(b=>b.ConfigureServices(services=>services.AddSingleton<IRacePresets>(new Game.TestFixtures.BoundaryRacePresets(600))));
        using var host=server.CreateClient();var(id,device,player)=await Setup(host,"Family");
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Guided",autoAcceptRecommendation=false})).EnsureSuccessStatusCode();
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
        var route=$"/api/lobbies/{id}/teams/team-01?deviceId={device}";var first=(await host.GetFromJsonAsync<TeamSnapshot>(route))!;
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",new TeamCommand(Guid.NewGuid().ToString(),first.Phase,first.CommandRevision,first.Assistance!.Recommendation!.Cars.Select(car=>car.Suggested).ToArray()))).EnsureSuccessStatusCode();
        var state=(await host.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!;
        if(state.Lifecycle=="phase-result")
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/continue",new{commandId=Guid.NewGuid().ToString(),phase=state.Phase,windowRevision=first.CommandRevision})).EnsureSuccessStatusCode();
        using var spectator=server.CreateClient();var open=(await spectator.GetFromJsonAsync<OpenTeamSnapshot>($"/api/lobbies/{id}/open/team-01"))!;
        Assert.True(open.Cars.Single(car=>car.DriverId=="driver-02").StopQueued);
        var advice=open.Assistance!.Recommendation!.Cars.Single(car=>car.DriverId=="driver-02");Assert.Equal("Stay Out",advice.Suggested.Pit);Assert.Null(Assert.Single(advice.PitOptions));
        Assert.Contains(advice.Reasons,reason=>reason.Contains("queued",StringComparison.OrdinalIgnoreCase));Assert.Contains("estimate",advice.Uncertainty,StringComparison.OrdinalIgnoreCase);
    }
    internal static WebApplicationFactory<Program> Server()=>new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"Firebase:ProjectId","demo-f1game"}})));
    internal static async Task<(string Id,string Device,string Player)> Setup(HttpClient client,string mode="Competitive")
    {
        client.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(false));
        var created=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode,name="Advice host"})).Content.ReadFromJsonAsync<JsonElement>();
        var id=created.GetProperty("lobby").GetProperty("sessionId").GetString()!;var member=created.GetProperty("membership");
        var device=member.GetProperty("deviceId").GetString()!;var player=member.GetProperty("playerId").GetString()!;
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();return(id,device,player);
    }
}




