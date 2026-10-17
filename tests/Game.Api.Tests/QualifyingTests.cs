using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Api;
using Game.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;

public sealed partial class QualifyingTests
{
    private sealed class Clock:TimeProvider
    {
        private DateTimeOffset now=DateTimeOffset.Parse("2026-10-17T00:00:00Z");
        public override DateTimeOffset GetUtcNow()=>now;
        public void Advance(int seconds)=>now=now.AddSeconds(seconds);
    }
    private sealed class FailingStore(ISessionStore inner,bool commitFirst):ISessionStore
    {
        public bool Armed{get;set;}
        public StoredSession? Read(string id)=>inner.Read(id);
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
        public string Write(string id,string? expected,string payload,string? code)=>Write(id,expected,payload,code,null);
        public string Write(string id,string? expected,string payload,string? code,ScheduledWakeup? wakeup)
        {
            if(!Armed)return inner.Write(id,expected,payload,code,wakeup);
            Armed=false;if(commitFirst)inner.Write(id,expected,payload,code,wakeup);throw new IOException("deliberate save failure");
        }
    }
    private sealed class CapturedWakeups:IScheduledWakeups
    {
        public List<ScheduledWakeup> Items{get;}=[];
        public void Enqueue(ScheduledWakeup wakeup)=>Items.Add(wakeup);
    }
    private static WebApplicationFactory<Program> Server(ISessionStore store,Clock? clock=null,CapturedWakeups? wakeups=null)=>LobbyTests.Server().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>
    {services.Replace(ServiceDescriptor.Singleton(store));if(clock is not null)services.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));if(wakeups is not null)services.Replace(ServiceDescriptor.Singleton<IScheduledWakeups>(wakeups));}));
    private sealed class TestSessionScope(ISessionStore inner):ISessionStore
    {
        private readonly HashSet<string> ids=[];
        public StoredSession? Read(string id)=>inner.Read(id);
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public IReadOnlyList<string> ListSessions()=>inner.ListSessions().Where(ids.Contains).ToArray();
        public string Write(string id,string? expected,string payload,string? code)=>Write(id,expected,payload,code,null);
        public string Write(string id,string? expected,string payload,string? code,ScheduledWakeup? wakeup){var version=inner.Write(id,expected,payload,code,wakeup);ids.Add(id);return version;}
    }
    private static ISessionStore Store()=>new TestSessionScope(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")));
    private static async Task<(string Id,string Device)> Ready(HttpClient client,string token,bool twoTeams=false)
    {
        client.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var joined=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=17,mode="Competitive",name="Host",decisionSeconds=30,handoffSeconds=10})).Content.ReadFromJsonAsync<JsonElement>();
        var id=joined.GetProperty("lobby").GetProperty("sessionId").GetString()!;var device=joined.GetProperty("membership").GetProperty("deviceId").GetString()!;
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
        if(twoTeams)
        {
            var people=await(await client.PostAsJsonAsync($"/api/lobbies/{id}/participants",new{name="Second",deviceId=device})).Content.ReadFromJsonAsync<JsonElement>();
            var player=people.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()=="Second").GetProperty("playerId").GetString();
            (await client.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-02",playerId=player,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
        }
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monza"})).EnsureSuccessStatusCode();
        for(var i=1;i<=(twoTeams?2:1);i++)
        {
            var uri=$"/api/lobbies/{id}/preparation/teams/team-{i:00}?deviceId={device}";
            foreach(var operation in new[]{"practice","confirm"})
            {
                var state=await client.GetFromJsonAsync<JsonElement>(uri);
                (await client.PostAsJsonAsync(uri.Replace("?","/"+operation+"?"),new{commandId=Guid.NewGuid().ToString(),revision=state.GetProperty("revision").GetInt64(),assignmentRevision=state.GetProperty("assignmentRevision").GetInt64()})).EnsureSuccessStatusCode();
            }
        }
        return(id,device);
    }
    private static async Task<StartQualifyingCommand> StartCommand(HttpClient client,string id)=>new(Guid.NewGuid().ToString(),(await client.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/preparation")).GetProperty("revision").GetInt64());
    private static QualifyingCommand Choice(JsonElement state,string first="Safe",string second="All-Out")=>new(Guid.NewGuid().ToString(),"Q1",state.GetProperty("revision").GetInt64(),state.GetProperty("assignmentRevision").GetInt64(),
        state.GetProperty("eligibleDrivers").EnumerateArray().Select((d,i)=>new Game.Domain.QualifyingChoice(d.GetProperty("entryId").GetString()!,i==0?first:second)).ToArray());
    private static HubConnection Hub(WebApplicationFactory<Program> server,string? token=null)=>new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>
    {options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();if(token is not null)options.AccessTokenProvider=()=>Task.FromResult<string?>(token);}).Build();
    private static void PublicOnly(JsonElement state)
    {
        var json=state.GetRawText();foreach(var hidden in new[]{"seed","choices","effective","ratings","randomness","compatibility","diagnostics"})Assert.DoesNotContain("\""+hidden+"\"",json,StringComparison.OrdinalIgnoreCase);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Three_recorded_sessions_recover_to_a_unique_grid_with_fixed_elimination_groups(bool commitFirst)
    {
        var store=new FailingStore(Store(),commitFirst);var clock=new Clock();var wakeups=new CapturedWakeups();var token=await LobbyTests.Identity(false);
        await using var server=Server(store,clock,wakeups);using var client=server.CreateClient();var(id,device)=await Ready(client,token);
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/qualifying",await StartCommand(client,id))).EnsureSuccessStatusCode();
        var root=$"/api/lobbies/{id}/qualifying";var results=new List<JsonElement>();
        for(var stage=1;stage<=3;stage++)
        {
            var own=await client.GetFromJsonAsync<JsonElement>(root+$"/teams/team-01?deviceId={device}");
            var choices=Choice(own) with{Session=$"Q{stage}"};
            if(choices.Choices!.Count>0)(await client.PostAsJsonAsync(root+$"/teams/team-01/choices?deviceId={device}",choices)).EnsureSuccessStatusCode();
            if(choices.Choices!.Count==0&&own.GetProperty("lifecycle").GetString()=="awaiting-decisions"){clock.Advance(60);await server.Services.GetRequiredService<SessionCoordinator>().SweepWakeups();}
            var result=await client.GetFromJsonAsync<JsonElement>(root);results.Add(result.GetProperty("result").Clone());
            Assert.Equal($"Q{stage}",result.GetProperty("session").GetString());
            Assert.Equal(stage==1?22:stage==2?16:10,result.GetProperty("result").GetProperty("ranking").GetArrayLength());
            if(stage<3)
            {
                var presentation=wakeups.Items.Last(w=>w.SessionId==id&&w.Kind=="qualifying-presentation");
                if(stage==2)
                {
                    (await client.PostAsJsonAsync($"/api/lobbies/{id}/pause",new{})).EnsureSuccessStatusCode();clock.Advance(20);
                    var coordinator=server.Services.GetRequiredService<SessionCoordinator>();
                    var stale=new WakeupInput(id,presentation.Generation,presentation.Phase,presentation.WindowRevision);
                    Assert.True((await coordinator.DeliverQualifyingWakeup(stale)).Paused);
                    (await client.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();
                    var resumed=wakeups.Items.Last(w=>w.SessionId==id);clock.Advance(8);
                    await coordinator.DeliverQualifyingWakeup(new(id,resumed.Generation,resumed.Phase,resumed.WindowRevision));
                    var savedStage=await client.GetStringAsync(root);await coordinator.DeliverQualifyingWakeup(stale);
                    await coordinator.DeliverQualifyingWakeup(new(id,resumed.Generation,resumed.Phase,resumed.WindowRevision));
                    Assert.Equal(savedStage,await client.GetStringAsync(root));continue;
                }
                var command=new{commandId=Guid.NewGuid().ToString(),phase=stage,windowRevision=result.GetProperty("windowRevision").GetInt64()};
                var before=await client.GetStringAsync(root);store.Armed=true;
                Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(root+"/continue",command)).StatusCode);
                if(!commitFirst)Assert.Equal(before,await client.GetStringAsync(root));
                var advanced=await client.PostAsJsonAsync(root+"/continue",command);advanced.EnsureSuccessStatusCode();
                var saved=await client.GetStringAsync(root);
                (await client.PostAsJsonAsync(root+"/continue",command)).EnsureSuccessStatusCode();Assert.Equal(saved,await client.GetStringAsync(root));
                Assert.False((await client.PostAsJsonAsync(root+$"/teams/team-01/choices?deviceId={device}",choices with{CommandId=Guid.NewGuid().ToString()})).IsSuccessStatusCode);
            }
        }
        var final=await client.GetFromJsonAsync<JsonElement>(root);Assert.Equal("grid-ready",final.GetProperty("lifecycle").GetString());
        var grid=final.GetProperty("grid").EnumerateArray().ToArray();Assert.Equal(22,grid.Length);Assert.Equal(22,grid.Select(r=>r.GetProperty("entryId").GetString()).Distinct().Count());
        Assert.Equal(Enumerable.Range(1,22),grid.Select(r=>r.GetProperty("position").GetInt32()));
        Assert.Equal(results[0].GetProperty("ranking").EnumerateArray().Skip(16).Select(r=>r.GetProperty("entryId").GetString()),grid.Skip(16).Select(r=>r.GetProperty("entryId").GetString()));
        Assert.Equal(results[1].GetProperty("ranking").EnumerateArray().Skip(10).Select(r=>r.GetProperty("entryId").GetString()),grid.Skip(10).Take(6).Select(r=>r.GetProperty("entryId").GetString()));
        Assert.Equal(results[2].GetProperty("ranking").EnumerateArray().Select(r=>r.GetProperty("entryId").GetString()),grid.Take(10).Select(r=>r.GetProperty("entryId").GetString()));
        await using var restarted=Server(store,clock);using var recovered=restarted.CreateClient();Assert.Equal(final.GetRawText(),await recovered.GetStringAsync(root));
        Assert.Equal(1,(await recovered.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{id}"))!.LeaderLap);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Start_freezes_preparation_and_creates_Q1_in_one_durable_commit(bool commitFirst)
    {
        var store=new FailingStore(Store(),commitFirst);var token=await LobbyTests.Identity(false);
        await using var server=Server(store);using var client=server.CreateClient();var(id,device)=await Ready(client,token);
        var command=await StartCommand(client,id);var before=await client.GetStringAsync($"/api/lobbies/{id}/preparation");
        store.Armed=true;Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync($"/api/lobbies/{id}/qualifying",command)).StatusCode);
        if(!commitFirst)
        {
            Assert.Equal(before,await client.GetStringAsync($"/api/lobbies/{id}/preparation"));
            Assert.Equal(HttpStatusCode.Conflict,(await client.GetAsync($"/api/lobbies/{id}/qualifying")).StatusCode);
        }
        var response=await client.PostAsJsonAsync($"/api/lobbies/{id}/qualifying",command);response.EnsureSuccessStatusCode();var receipt=await response.Content.ReadAsStringAsync();
        Assert.All((await client.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/preparation")).GetProperty("readiness").EnumerateArray(),r=>Assert.True(r.GetProperty("locked").GetBoolean()));
        var first=await client.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/qualifying");PublicOnly(first);
        Assert.Equal(22,first.GetProperty("drivers").GetArrayLength());Assert.Equal(16,first.GetProperty("retain").GetInt32());
        Assert.Equal(10,first.GetProperty("teams").EnumerateArray().Count(t=>t.GetProperty("locked").GetBoolean()));
        await using var restarted=Server(store);using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(first.GetRawText(),(await recovered.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/qualifying")).GetRawText());
        Assert.Equal(receipt,await(await recovered.PostAsJsonAsync($"/api/lobbies/{id}/qualifying",command with{CommandId=Guid.Parse(command.CommandId).ToString("B").ToUpperInvariant()})).Content.ReadAsStringAsync());
        var prep=await recovered.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/preparation/teams/team-01?deviceId={device}");
        Assert.Equal(HttpStatusCode.Conflict,(await recovered.PostAsJsonAsync($"/api/lobbies/{id}/preparation/teams/team-01/reopen?deviceId={device}",new{commandId=Guid.NewGuid().ToString(),revision=prep.GetProperty("revision").GetInt64(),assignmentRevision=prep.GetProperty("assignmentRevision").GetInt64()})).StatusCode);
    }
    [Fact]
    public async Task Complete_team_locks_are_private_atomic_retryable_and_resolve_the_full_field_once()
    {
        var store=Store();var token=await LobbyTests.Identity(false);await using var server=Server(store);using var client=server.CreateClient();var(id,device)=await Ready(client,token,true);
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/qualifying",await StartCommand(client,id))).EnsureSuccessStatusCode();
        await using var publicHub=Hub(server);await using var ownHub=Hub(server,token);await publicHub.StartAsync();await ownHub.StartAsync();
        var publicUpdates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();var privateUpdates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();
        publicHub.On<JsonElement>("qualifyingSnapshot",s=>publicUpdates.Writer.TryWrite(s));ownHub.On<JsonElement>("qualifyingTeamSnapshot",s=>privateUpdates.Writer.TryWrite(s));
        PublicOnly(await publicHub.InvokeAsync<JsonElement>("SubscribeQualifying",id));
        await Assert.ThrowsAsync<HubException>(()=>publicHub.InvokeAsync<JsonElement>("SubscribeOpenQualifying",id,"team-01"));
        var own=await ownHub.InvokeAsync<JsonElement>("SubscribeHouseholdQualifying",id,device,"team-01");var command=Choice(own);
        var uri=$"/api/lobbies/{id}/qualifying/teams/team-01/choices?deviceId={device}";var before=await client.GetStringAsync($"/api/lobbies/{id}/qualifying");
        foreach(var invalid in new[]{command with{Choices=[command.Choices![0]]},command with{Choices=[command.Choices![0],command.Choices[0]]},command with{Choices=[command.Choices![0] with{Approach="Unknown"},command.Choices[1]]}})
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(uri,invalid)).StatusCode);
        Assert.Equal(before,await client.GetStringAsync($"/api/lobbies/{id}/qualifying"));Assert.False(publicUpdates.Reader.TryRead(out _));
        var accepted=await client.PostAsJsonAsync(uri,command);accepted.EnsureSuccessStatusCode();var receipt=await accepted.Content.ReadAsStringAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));var publicLock=await publicUpdates.Reader.ReadAsync(timeout.Token);PublicOnly(publicLock);
        Assert.Equal(JsonValueKind.Null,publicLock.GetProperty("result").ValueKind);
        var privateLock=await privateUpdates.Reader.ReadAsync(timeout.Token);Assert.True(privateLock.GetProperty("locked").GetBoolean());Assert.Equal("All-Out",privateLock.GetProperty("choices")[1].GetProperty("approach").GetString());
        await using var restarted=Server(store);using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(receipt,await(await recovered.PostAsJsonAsync(uri,command with{CommandId=Guid.Parse(command.CommandId).ToString("N")})).Content.ReadAsStringAsync());
        var second=await recovered.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/qualifying/teams/team-02?deviceId={device}");
        var last=await recovered.PostAsJsonAsync($"/api/lobbies/{id}/qualifying/teams/team-02/choices?deviceId={device}",Choice(second,"Push","Safe"));last.EnsureSuccessStatusCode();
        var result=await recovered.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/qualifying");PublicOnly(result);Assert.Equal("session-result",result.GetProperty("lifecycle").GetString());
        var ranking=result.GetProperty("result").GetProperty("ranking").EnumerateArray().ToArray();Assert.Equal(22,ranking.Length);Assert.Equal(22,ranking.Select(r=>r.GetProperty("entryId").GetString()).Distinct().Count());Assert.Equal(16,ranking.Count(r=>r.GetProperty("advances").GetBoolean()));
        Assert.Equal(receipt,await(await recovered.PostAsJsonAsync(uri,command)).Content.ReadAsStringAsync());Assert.Equal(result.GetRawText(),(await recovered.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/qualifying")).GetRawText());
        (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/devices/{device}/revoke",new{})).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden,(await recovered.PostAsJsonAsync(uri,command)).StatusCode);
    }
    [Fact]
    public async Task Saved_deadline_pause_and_sweep_preserve_human_lock_and_fill_only_missing_teams()
    {
        var store=Store();var clock=new Clock();var token=await LobbyTests.Identity(false);await using var server=Server(store,clock);using var client=server.CreateClient();var(id,device)=await Ready(client,token,true);
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/qualifying",await StartCommand(client,id))).EnsureSuccessStatusCode();
        var ownUri=$"/api/lobbies/{id}/qualifying/teams/team-01?deviceId={device}";var command=Choice(await client.GetFromJsonAsync<JsonElement>(ownUri));
        (await client.PostAsJsonAsync(ownUri.Replace("?","/choices?"),command)).EnsureSuccessStatusCode();
        var initial=await client.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/qualifying");Assert.Equal(40,initial.GetProperty("timingPolicy").GetProperty("effectiveSeconds").GetInt32());
        clock.Advance(10);(await client.PostAsJsonAsync($"/api/lobbies/{id}/pause",new{})).EnsureSuccessStatusCode();clock.Advance(100);
        await using var restarted=Server(store,clock);using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(0,await restarted.Services.GetRequiredService<SessionCoordinator>().SweepWakeups());
        (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();
        var resumed=await recovered.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/qualifying");Assert.Equal(clock.GetUtcNow().AddSeconds(30),resumed.GetProperty("decisionDeadlineUtc").GetDateTimeOffset());
        clock.Advance(30);Assert.Equal(1,await restarted.Services.GetRequiredService<SessionCoordinator>().SweepWakeups());
        var result=await recovered.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/qualifying");Assert.Equal("session-result",result.GetProperty("lifecycle").GetString());
        Assert.Equal("human",result.GetProperty("teams")[0].GetProperty("source").GetString());Assert.Equal("timeout-baseline",result.GetProperty("teams")[1].GetProperty("source").GetString());
        Assert.Equal("All-Out",(await recovered.GetFromJsonAsync<JsonElement>(ownUri)).GetProperty("choices")[1].GetProperty("approach").GetString());
        Assert.Equal(0,await restarted.Services.GetRequiredService<SessionCoordinator>().SweepWakeups());Assert.Equal(result.GetRawText(),(await recovered.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/qualifying")).GetRawText());
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_lock_save_emits_no_success_and_same_receipt_recovers_without_resampling(bool commitFirst)
    {
        var store=new FailingStore(Store(),commitFirst);var token=await LobbyTests.Identity(false);await using var server=Server(store);using var client=server.CreateClient();var(id,device)=await Ready(client,token);
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/qualifying",await StartCommand(client,id))).EnsureSuccessStatusCode();
        await using var hub=Hub(server,token);await hub.StartAsync();var updates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();hub.On<JsonElement>("qualifyingTeamSnapshot",s=>updates.Writer.TryWrite(s));
        var own=await hub.InvokeAsync<JsonElement>("SubscribeHouseholdQualifying",id,device,"team-01");var command=Choice(own);var uri=$"/api/lobbies/{id}/qualifying/teams/team-01/choices?deviceId={device}";
        var before=await client.GetStringAsync($"/api/lobbies/{id}/qualifying");store.Armed=true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(uri,command)).StatusCode);Assert.False(updates.Reader.TryRead(out _));
        if(!commitFirst)Assert.Equal(before,await client.GetStringAsync($"/api/lobbies/{id}/qualifying"));
        var accepted=await client.PostAsJsonAsync(uri,command);accepted.EnsureSuccessStatusCode();var receipt=await accepted.Content.ReadAsStringAsync();
        var result=await client.GetStringAsync($"/api/lobbies/{id}/qualifying");
        await using var restarted=Server(store);using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(result,await recovered.GetStringAsync($"/api/lobbies/{id}/qualifying"));Assert.Equal(receipt,await(await recovered.PostAsJsonAsync(uri,command)).Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict,(await recovered.PostAsJsonAsync(uri,command with{Choices=command.Choices!.Reverse().Select(c=>c with{Approach="Push"}).ToArray()})).StatusCode);
        Assert.Equal(result,await recovered.GetStringAsync($"/api/lobbies/{id}/qualifying"));
    }}
