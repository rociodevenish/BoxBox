using Microsoft.AspNetCore.SignalR.Client;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;
public sealed class ControlTransferTests
{
    [Fact]
    public async Task Host_takeover_waits_for_next_window_and_preserves_accepted_human_lock()
    {
        var clock=new DeadlineTests.ControlledClock();await using var server=Server(clock);using var host=server.CreateClient();var(id,device)=await StartShared(host);
        var command=new TeamCommand(Guid.NewGuid().ToString(),0,1,[new("driver-01","Push","Stay Out",null,"Neutral"),new("driver-02","Conserve","Stay Out",null,"Neutral")]);
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",command)).EnsureSuccessStatusCode();
        var change=await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/control",new{commandId=Guid.NewGuid().ToString(),revision=0,mode="PermanentAi"});change.EnsureSuccessStatusCode();
        var status=(await change.Content.ReadFromJsonAsync<JsonElement>());Assert.Equal("Human",status.GetProperty("mode").GetString());Assert.Equal("PermanentAi",status.GetProperty("pendingMode").GetString());Assert.Equal(1,status.GetProperty("effectivePhase").GetInt32());
        var retained=await host.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{id}/teams/team-01?deviceId={device}");Assert.Equal("Push",retained!.ConfirmedPlan!.Cars[0].Pace);
        clock.Advance(TimeSpan.FromSeconds(120));(await host.PostAsJsonAsync($"/api/lobbies/{id}/advance-due",new{commandId=Guid.NewGuid().ToString(),phase=0,windowRevision=1})).EnsureSuccessStatusCode();
        await ContinueIfNeeded(host,id,device);
        var state=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(1,state.GetProperty("phase").GetInt32());
        var control=state.GetProperty("controlStatus").EnumerateArray().Single(team=>team.GetProperty("teamId").GetString()=="team-01");Assert.Equal("PermanentAi",control.GetProperty("mode").GetString());
        Assert.DoesNotContain(state.GetProperty("readiness").EnumerateArray(),team=>team.GetProperty("teamId").GetString()=="team-01");
        var rejected=await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",command with{CommandId=Guid.NewGuid().ToString(),Phase=1,Revision=state.GetProperty("revision").GetInt64()});Assert.Equal(HttpStatusCode.Conflict,rejected.StatusCode);
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",command)).EnsureSuccessStatusCode();
    }
    [Theory]
    [InlineData("TemporaryAi")]
    [InlineData("PermanentAi")]
    public async Task Takeover_and_authorised_reclaim_survive_restart_and_apply_only_at_boundaries(string mode)
    {
        var clock=new DeadlineTests.ControlledClock();string id,device,token;var request=new{commandId=Guid.NewGuid().ToString(),revision=0,mode};
        await using(var server=Server(clock,true))
        {
            using var host=server.CreateClient();(id,device)=await StartShared(host);token=host.DefaultRequestHeaders.Authorization!.Parameter!;
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/control",request)).EnsureSuccessStatusCode();await Expire(host,id,device,clock);
        }
        await using var restarted=Server(clock,true);using var resumed=restarted.CreateClient();resumed.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var own=await resumed.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{id}/teams/team-01?deviceId={device}");Assert.Equal(mode,own!.ControlStatus!.Mode);Assert.Equal(2,own.ControlStatus.Revision);Assert.Equal(1,own.Phase);
        Assert.Equal(HttpStatusCode.Forbidden,(await resumed.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/reclaim?deviceId=unassigned",new{commandId=Guid.NewGuid().ToString(),revision=2,mode="Human"})).StatusCode);
        if(mode=="PermanentAi")
        {
            var reclaim=await resumed.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/reclaim?deviceId={device}",new{commandId=Guid.NewGuid().ToString(),revision=2,mode="Human"});reclaim.EnsureSuccessStatusCode();
            var queued=await reclaim.Content.ReadFromJsonAsync<TeamControlStatus>();Assert.Equal("PermanentAi",queued!.Mode);Assert.Equal("Human",queued.PendingMode);
        }
        var retry=await resumed.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/control",request);retry.EnsureSuccessStatusCode();Assert.Equal(1,(await retry.Content.ReadFromJsonAsync<TeamControlStatus>())!.Revision);
        await Expire(resumed,id,device,clock);
        var completed=await resumed.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Contains(completed.GetProperty("decisionSources").EnumerateArray(),source=>source.GetProperty("teamId").GetString()=="team-01"&&source.GetProperty("source").GetString()==(mode=="PermanentAi"?"permanent-takeover-ai":"temporary-takeover-ai"));
        own=await resumed.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{id}/teams/team-01?deviceId={device}");Assert.Equal("Human",own!.ControlStatus!.Mode);Assert.Equal(2,own.Phase);
        var command=new TeamCommand(Guid.NewGuid().ToString(),own.Phase,own.CommandRevision,[new("driver-01","Push","Stay Out",null,"Neutral"),new("driver-02","Standard","Stay Out",null,"Neutral")]);
        (await resumed.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",command)).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task Concurrent_control_requests_commit_one_revision_and_revoked_device_cannot_reclaim_after_restart()
    {
        var clock=new DeadlineTests.ControlledClock();string id,device,token;
        await using(var server=Server(clock,true))
        {
            using var host=server.CreateClient();(id,device)=await StartShared(host);token=host.DefaultRequestHeaders.Authorization!.Parameter!;
            var requests=await Task.WhenAll(Enumerable.Range(0,8).Select(_=>host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/control",new{commandId=Guid.NewGuid().ToString(),revision=0,mode="PermanentAi"})));
            Assert.Single(requests,response=>response.StatusCode==HttpStatusCode.OK);Assert.Equal(7,requests.Count(response=>response.StatusCode==HttpStatusCode.Conflict));
            await Expire(host,id,device,clock);(await host.PostAsJsonAsync($"/api/lobbies/{id}/devices/{device}/revoke",new{})).EnsureSuccessStatusCode();
        }
        await using var restarted=Server(clock,true);using var revoked=restarted.CreateClient();revoked.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(HttpStatusCode.Forbidden,(await revoked.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/reclaim?deviceId={device}",new{commandId=Guid.NewGuid().ToString(),revision=2,mode="Human"})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await revoked.GetAsync($"/api/lobbies/{id}/teams/team-01?deviceId={device}")).StatusCode);
        var state=await revoked.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal("PermanentAi",state.GetProperty("controlStatus")[0].GetProperty("mode").GetString());
        await using var hub=new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>restarted.Server.CreateHandler();options.AccessTokenProvider=()=>Task.FromResult<string?>(token);}).Build();await hub.StartAsync();
        await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(()=>hub.InvokeAsync<TeamSnapshot>("SubscribeHouseholdTeam",id,device,"team-01"));
        var publicState=await hub.InvokeAsync<PublicSnapshot>("SubscribePublic",id);Assert.Equal("PermanentAi",publicState.ControlStatus!.Single(status=>status.TeamId=="team-01").Mode);
    }
    [Fact]
    public async Task Queued_takeover_survives_restart_without_changing_the_current_phase()
    {
        var clock=new DeadlineTests.ControlledClock();string id,device,token;
        await using(var server=Server(clock,true))
        {
            using var host=server.CreateClient();(id,device)=await StartShared(host);token=host.DefaultRequestHeaders.Authorization!.Parameter!;
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/control",new{commandId=Guid.NewGuid().ToString(),revision=0,mode="PermanentAi"})).EnsureSuccessStatusCode();
        }
        await using var restarted=Server(clock,true);using var hostAgain=restarted.CreateClient();hostAgain.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var own=await hostAgain.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{id}/teams/team-01?deviceId={device}");Assert.Equal(0,own!.Phase);Assert.Equal("Human",own.ControlStatus!.Mode);Assert.Equal("PermanentAi",own.ControlStatus.PendingMode);Assert.Equal(1,own.ControlStatus.Revision);
        await Expire(hostAgain,id,device,clock);own=await hostAgain.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{id}/teams/team-01?deviceId={device}");Assert.Equal(1,own!.Phase);Assert.Equal("PermanentAi",own.ControlStatus!.Mode);Assert.Equal(2,own.ControlStatus.Revision);
    }
    private static async Task Expire(HttpClient host,string id,string device,DeadlineTests.ControlledClock clock)
    {
        var own=await host.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{id}/teams/team-02?deviceId={device}");clock.Advance(TimeSpan.FromSeconds(121));
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/advance-due",new{commandId=Guid.NewGuid().ToString(),phase=own!.Phase,windowRevision=own.CommandRevision})).EnsureSuccessStatusCode();await ContinueIfNeeded(host,id,device);
    }
    private static async Task<(string Id,string Device)> StartShared(HttpClient host)
    {
        host.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(false));
        var created=await(await host.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=20,seed=123,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
        var id=created.GetProperty("lobby").GetProperty("sessionId").GetString()!;var device=created.GetProperty("membership").GetProperty("deviceId").GetString()!;
        foreach(var(name,team)in new[]{("Alex","team-01"),("Sam","team-02")})
        {
            var added=await(await host.PostAsJsonAsync($"/api/lobbies/{id}/participants",new{name,deviceId=device})).Content.ReadFromJsonAsync<JsonElement>();var player=added.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()==name).GetProperty("playerId").GetString();
            (await host.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId=team,playerId=player,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
        }
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();return(id,device);
    }
    private static WebApplicationFactory<Program> Server(DeadlineTests.ControlledClock clock,bool durable=false)=>DeadlineTests.Server(clock,durable).WithWebHostBuilder(builder=>builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>{["Persistence:EmulatorHost"]=Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")??"127.0.0.1:19100"})));
    private static async Task ContinueIfNeeded(HttpClient host,string id,string device)
    {
        var state=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");if(state.GetProperty("lifecycle").GetString()!="phase-result")return;
        var own=await host.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{id}/teams/team-01?deviceId={device}");
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/continue",new{commandId=Guid.NewGuid().ToString(),phase=state.GetProperty("phase").GetInt32(),windowRevision=own!.CommandRevision})).EnsureSuccessStatusCode();
    }
}
