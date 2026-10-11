using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;
public sealed class DeadlineTests
{
    [Fact]
    public async Task Shared_device_deadline_is_frozen_before_choices_and_refresh_does_not_extend_it()
    {
        var clock=new ControlledClock();await using var server=Server(clock);using var client=server.CreateClient();
        var (id,device)=await StartShared(client);
        var first=await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");
        Assert.Equal("2026-10-11T01:02:00+00:00",first.GetProperty("decisionDeadlineUtc").GetString());
        Assert.Equal(120,first.GetProperty("timingPolicy").GetProperty("effectiveSeconds").GetInt32());
        Assert.Contains("Temporary AI",first.GetProperty("timingPolicy").GetProperty("timeoutPolicy").GetString());
        clock.Advance(TimeSpan.FromSeconds(40));
        var refreshed=await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");
        Assert.Equal(first.GetProperty("decisionDeadlineUtc").GetString(),refreshed.GetProperty("decisionDeadlineUtc").GetString());
    }
    [Theory]
    [InlineData(119,true)]
    [InlineData(120,false)]
    [InlineData(121,false)]
    public async Task Only_complete_locks_processed_before_deadline_are_accepted_and_expiry_is_idempotent(int seconds,bool accepted)
    {
        var clock=new ControlledClock();await using var server=Server(clock);using var client=server.CreateClient();var (id,device)=await StartShared(client);
        var command=new TeamCommand(Guid.NewGuid().ToString(),0,1,[new("driver-01","Push","Stay Out",null,"Neutral"),new("driver-02","Conserve","Pit","Hard","Neutral")]);
        clock.Advance(TimeSpan.FromSeconds(seconds));
        var submitted=await client.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",command);
        Assert.Equal(accepted?System.Net.HttpStatusCode.OK:System.Net.HttpStatusCode.Conflict,submitted.StatusCode);
        if(!accepted){var rejected=await submitted.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(5,rejected.GetProperty("currentState").GetProperty("leaderLap").GetInt32());}
        if(accepted)clock.Advance(TimeSpan.FromSeconds(2));
        var due=new{commandId=Guid.NewGuid().ToString(),phase=0,windowRevision=1};
        var closed=await client.PostAsJsonAsync($"/api/lobbies/{id}/advance-due",due);closed.EnsureSuccessStatusCode();
        var state=(await closed.Content.ReadFromJsonAsync<JsonElement>());
        Assert.Equal(5,state.GetProperty("leaderLap").GetInt32());Assert.Equal(0,state.GetProperty("phase").GetInt32());Assert.Equal("phase-result",state.GetProperty("lifecycle").GetString());
        var repeated=await client.PostAsJsonAsync($"/api/lobbies/{id}/advance-due",due);repeated.EnsureSuccessStatusCode();
        Assert.Equal(state.GetProperty("revision").GetInt64(),(await repeated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("revision").GetInt64());
        if(accepted)
        {
            var retry=await client.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",command);retry.EnsureSuccessStatusCode();
            Assert.Equal(await submitted.Content.ReadAsStringAsync(),await retry.Content.ReadAsStringAsync());
        }
        client.DefaultRequestHeaders.Add("X-Designer-Key","deadline-test-diagnostics");
        var record=await client.GetFromJsonAsync<JsonElement>($"/api/designer/sessions/{id}/record");
        var scheduled=record.GetProperty("replay").GetProperty("plan").GetProperty("scheduled").EnumerateArray().ToArray();
        var driver1=scheduled.Single(p=>p.GetProperty("driverId").GetString()=="driver-01");
        if(accepted)Assert.Equal("Push",driver1.GetProperty("decision").GetProperty("pace").GetString());
        Assert.Contains(state.GetProperty("decisionSources").EnumerateArray(),source=>source.GetProperty("teamId").GetString()=="team-02"&&source.GetProperty("source").GetString()=="timeout-ai");
    }
    [Fact]
    public async Task Pause_survives_restart_and_resume_uses_saved_remaining_duration()
    {
        var clock=new ControlledClock();string id,device,token;
        await using(var server=Server(clock,true))
        {
            using var client=server.CreateClient();(id,device)=await StartShared(client);token=client.DefaultRequestHeaders.Authorization!.Parameter!;
            clock.Advance(TimeSpan.FromSeconds(40));(await client.PostAsJsonAsync($"/api/lobbies/{id}/pause",new{})).EnsureSuccessStatusCode();
            var paused=await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");
            Assert.Equal(JsonValueKind.Null,paused.GetProperty("decisionDeadlineUtc").ValueKind);Assert.Equal(80,paused.GetProperty("pausedRemainingSeconds").GetDouble());
        }
        clock.Advance(TimeSpan.FromSeconds(1000));
        await using var restarted=Server(clock,true);using var host=restarted.CreateClient();host.DefaultRequestHeaders.Authorization=new("Bearer",token);
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();
        var resumed=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(clock.GetUtcNow().AddSeconds(80),resumed.GetProperty("decisionDeadlineUtc").GetDateTimeOffset());
    }
    [Fact]
    public async Task Overdue_restart_closes_one_window_and_pauses_before_another_window()
    {
        var clock=new ControlledClock();string id,device,token;
        await using(var server=Server(clock,true))
        {
            using var client=server.CreateClient();(id,device)=await StartShared(client);token=client.DefaultRequestHeaders.Authorization!.Parameter!;
            var command=new TeamCommand(Guid.NewGuid().ToString(),0,1,[new("driver-01","Push","Stay Out",null,"Neutral"),new("driver-02","Conserve","Pit","Hard","Neutral")]);
            (await client.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",command)).EnsureSuccessStatusCode();
        }
        clock.Advance(TimeSpan.FromDays(3));
        await using var restarted=Server(clock,true);using var host=restarted.CreateClient();host.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var recovered=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(5,recovered.GetProperty("leaderLap").GetInt32());Assert.Equal(0,recovered.GetProperty("phase").GetInt32());Assert.True(recovered.GetProperty("paused").GetBoolean());
        var due=new{commandId=Guid.NewGuid().ToString(),phase=0,windowRevision=1};
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/advance-due",due)).EnsureSuccessStatusCode();
        Assert.Equal(recovered.GetProperty("revision").GetInt64(),(await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}")).GetProperty("revision").GetInt64());
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();
        var next=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(1,next.GetProperty("phase").GetInt32());Assert.Equal(clock.GetUtcNow().AddSeconds(120),next.GetProperty("decisionDeadlineUtc").GetDateTimeOffset());
        var own=await host.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{id}/teams/team-02?deviceId={device}");
        var human=new TeamCommand(Guid.NewGuid().ToString(),own!.Phase,own.CommandRevision,[new("driver-03","Standard","Stay Out",null,"Neutral"),new("driver-04","Standard","Stay Out",null,"Neutral")]);
        (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-02/plans?deviceId={device}",human)).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task Failed_timeout_outcome_write_restarts_from_saved_complete_plan_without_human_acknowledgement()
    {
        var clock=new ControlledClock();var store=new TimeoutFailureStore(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19100"));string id;
        await using(var server=Server(clock,true).WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.AddSingleton<ISessionStore>(store))))
        {
            using var host=server.CreateClient();(id,_)=await StartShared(host);clock.Advance(TimeSpan.FromSeconds(120));store.FailOnSecondWrite=true;
            var failed=await host.PostAsJsonAsync($"/api/lobbies/{id}/advance-due",new{commandId=Guid.NewGuid().ToString(),phase=0,windowRevision=1});
            Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable,failed.StatusCode);
            var saved=JsonSerializer.Deserialize<JsonElement>(store.Read(id)!.Payload);
            Assert.Equal(JsonValueKind.Object,saved.GetProperty("lobby").GetProperty("race").GetProperty("pendingPlan").ValueKind);
            Assert.Equal(JsonValueKind.Null,saved.GetProperty("lobby").GetProperty("race").GetProperty("pendingCommand").ValueKind);
        }
        await using var restarted=Server(clock,true);using var client=restarted.CreateClient();
        var state=await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(5,state.GetProperty("leaderLap").GetInt32());Assert.True(state.GetProperty("paused").GetBoolean());
        var repeated=await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(state.GetProperty("revision").GetInt64(),repeated.GetProperty("revision").GetInt64());
        Assert.All(state.GetProperty("decisionSources").EnumerateArray(),source=>Assert.Equal("timeout-ai",source.GetProperty("source").GetString()));
    }
    private sealed class TimeoutFailureStore(ISessionStore inner):ISessionStore
    {
        public bool FailOnSecondWrite {get;set;}
        private int writes;
        public StoredSession? Read(string id)=>inner.Read(id);
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public string Write(string id,string? version,string payload,string? code){if(FailOnSecondWrite&&++writes==2)throw new IOException("Outcome save interrupted");return inner.Write(id,version,payload,code);}
    }
    internal static WebApplicationFactory<Program> Server(ControlledClock clock,bool durable=false)=>new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>{
        builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>{["Wakeups:Mode"]="manual",["Persistence:ProjectId"]=durable?"demo-f1game":null,["Persistence:EmulatorHost"]=Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19100",["Firebase:ProjectId"]="demo-f1game",["Designer:Key"]="deadline-test-diagnostics"}));
        builder.ConfigureServices(services=>services.AddSingleton<TimeProvider>(clock));
    });
    internal static async Task<(string Id,string Device)> StartShared(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(false));
        var created=(await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>());
        var id=created.GetProperty("lobby").GetProperty("sessionId").GetString()!;var device=created.GetProperty("membership").GetProperty("deviceId").GetString()!;
        foreach(var (name,team) in new[]{("Alex","team-01"),("Sam","team-02")})
        {
            var added=(await(await client.PostAsJsonAsync($"/api/lobbies/{id}/participants",new{name,deviceId=device})).Content.ReadFromJsonAsync<JsonElement>());
            var player=added.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()==name).GetProperty("playerId").GetString();
            (await client.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId=team,playerId=player,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
        }
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();return(id,device);
    }
    internal sealed class ControlledClock:TimeProvider
    {
        private DateTimeOffset now=DateTimeOffset.Parse("2026-10-11T01:00:00Z");
        public override DateTimeOffset GetUtcNow()=>now;
        public void Advance(TimeSpan elapsed)=>now+=elapsed;
    }
}
