using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR;
using Xunit;
using Game.Application;
using Game.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace Game.Api.Tests;

public sealed class PreparationTests
{
    [Theory]
    [InlineData("confirm",false,false)]
    [InlineData("confirm",false,true)]
    [InlineData("confirm",true,false)]
    [InlineData("reopen",false,false)]
    [InlineData("reopen",true,false)]
    public async Task Readiness_save_failures_publish_no_success_and_recover_the_same_receipt(string operation,bool commitBeforeFailure,bool failRead)
    {
        var store=new PreparationFailureStore(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")),commitBeforeFailure,failRead);
        var token=await LobbyTests.Identity(false);
        await using var server=LobbyTests.Server().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.Replace(ServiceDescriptor.Singleton<ISessionStore>(store))));
        using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var joined=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=19,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
        var id=joined.GetProperty("lobby").GetProperty("sessionId").GetString();var device=joined.GetProperty("membership").GetProperty("deviceId").GetString();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monza"})).EnsureSuccessStatusCode();
        var uri=$"/api/lobbies/{id}/preparation/teams/team-01?deviceId={device}";
        async Task<JsonElement> Current()=>await client.GetFromJsonAsync<JsonElement>(uri);
        object Command(JsonElement state)=>new{commandId=Guid.NewGuid().ToString(),revision=state.GetProperty("revision").GetInt64(),assignmentRevision=state.GetProperty("assignmentRevision").GetInt64()};
        (await client.PostAsJsonAsync(uri.Replace("?","/practice?"),Command(await Current()))).EnsureSuccessStatusCode();
        if(operation=="reopen")
            (await client.PostAsJsonAsync(uri.Replace("?","/confirm?"),Command(await Current()))).EnsureSuccessStatusCode();
        var before=await client.GetStringAsync(uri);var feedback=(await Current()).GetProperty("assessment").GetRawText();
        var command=Command(await Current());
        await using var hub=new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();options.AccessTokenProvider=()=>Task.FromResult<string?>(token);}).Build();
        var updates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();hub.On<JsonElement>("preparationTeamSnapshot",snapshot=>updates.Writer.TryWrite(snapshot));await hub.StartAsync();
        await hub.InvokeAsync<JsonElement>("SubscribeHouseholdPreparation",id,device,"team-01");
        store.Armed=true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(uri.Replace("?","/"+operation+"?"),command)).StatusCode);
        Assert.False(updates.Reader.TryRead(out _));
        if(!commitBeforeFailure)Assert.Equal(before,await client.GetStringAsync(uri));
        var accepted=await client.PostAsJsonAsync(uri.Replace("?","/"+operation+"?"),command);accepted.EnsureSuccessStatusCode();var ack=await accepted.Content.ReadAsStringAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));var notified=await updates.Reader.ReadAsync(timeout.Token);
        Assert.Equal(operation=="confirm"?"final-ready":"assessed",notified.GetProperty("lifecycle").GetString());
        Assert.Equal(feedback,notified.GetProperty("assessment").GetRawText());
        await using var restarted=LobbyTests.Server().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.Replace(ServiceDescriptor.Singleton<ISessionStore>(store))));
        using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(await client.GetStringAsync(uri),await recovered.GetStringAsync(uri));
        Assert.Equal(ack,await(await recovered.PostAsJsonAsync(uri.Replace("?","/"+operation+"?"),command)).Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(uri.Replace("?","/"+(operation=="confirm"?"reopen":"confirm")+"?"),command)).StatusCode);
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/devices/{device}/revoke",new{})).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(uri.Replace("?","/"+operation+"?"),command)).StatusCode);
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(false,true)]
    [InlineData(true,false)]
    public async Task Full_field_freeze_is_atomic_durable_and_retryable_without_starting_qualifying(bool commitBeforeFailure,bool failRead)
    {
        var store=new PreparationFailureStore(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")),commitBeforeFailure,failRead);
        var token=await LobbyTests.Identity(false);
        await using var server=LobbyTests.Server().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.Replace(ServiceDescriptor.Singleton<ISessionStore>(store))));
        using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var joined=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=17,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
        var id=joined.GetProperty("lobby").GetProperty("sessionId").GetString();var device=joined.GetProperty("membership").GetProperty("deviceId").GetString();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
        for(var i=2;i<=11;i++)
        {
            var name="Participant "+i;
            var people=await(await client.PostAsJsonAsync($"/api/lobbies/{id}/participants",new{name,deviceId=device})).Content.ReadFromJsonAsync<JsonElement>();
            var player=people.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()==name).GetProperty("playerId").GetString();
            (await client.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId=$"team-{i:00}",playerId=player,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
        }
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monaco"})).EnsureSuccessStatusCode();
        var publicUri=$"/api/lobbies/{id}/preparation";
        var before=await client.GetStringAsync(publicUri);
        var lobby=await client.GetFromJsonAsync<JsonElement>(publicUri);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(publicUri+"/freeze",new{commandId=Guid.NewGuid().ToString(),revision=lobby.GetProperty("revision").GetInt64()})).StatusCode);
        Assert.Equal(before,await client.GetStringAsync(publicUri));
        var confirmations=new List<(string Uri,object Command,string Ack)>();
        for(var i=1;i<=11;i++)
        {
            var uri=$"{publicUri}/teams/team-{i:00}?deviceId={device}";
            var team=await client.GetFromJsonAsync<JsonElement>(uri);var assignmentRevision=team.GetProperty("assignmentRevision").GetInt64();
            (await client.PostAsJsonAsync(uri.Replace("?","/practice?"),new{commandId=Guid.NewGuid().ToString(),revision=team.GetProperty("revision").GetInt64(),assignmentRevision})).EnsureSuccessStatusCode();
            team=await client.GetFromJsonAsync<JsonElement>(uri);
            var command=new{commandId=Guid.NewGuid().ToString(),revision=team.GetProperty("revision").GetInt64(),assignmentRevision};
            var accepted=await client.PostAsJsonAsync(uri.Replace("?","/confirm?"),command);accepted.EnsureSuccessStatusCode();
            confirmations.Add((uri,command,await accepted.Content.ReadAsStringAsync()));
            if(i==1)
            {
                var partial=await client.GetStringAsync(publicUri);var partialState=await client.GetFromJsonAsync<JsonElement>(publicUri);
                Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(publicUri+"/freeze",new{commandId=Guid.NewGuid().ToString(),revision=partialState.GetProperty("revision").GetInt64()})).StatusCode);
                Assert.Equal(partial,await client.GetStringAsync(publicUri));
                Assert.Equal("final-ready",(await client.GetFromJsonAsync<JsonElement>(uri)).GetProperty("lifecycle").GetString());
            }
        }
        var ready=await client.GetStringAsync(publicUri);lobby=await client.GetFromJsonAsync<JsonElement>(publicUri);
        Assert.All(lobby.GetProperty("readiness").EnumerateArray(),t=>Assert.True(t.GetProperty("finalReady").GetBoolean()));
        var freeze=new{commandId=Guid.NewGuid().ToString(),revision=lobby.GetProperty("revision").GetInt64()};
        using(var outsider=server.CreateClient())
        {
            outsider.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));
            Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync(publicUri+"/freeze",freeze)).StatusCode);
        }
        await using var hub=new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();options.AccessTokenProvider=()=>Task.FromResult<string?>(token);}).Build();
        var updates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();hub.On<JsonElement>("preparationTeamSnapshot",snapshot=>updates.Writer.TryWrite(snapshot));await hub.StartAsync();
        await hub.InvokeAsync<JsonElement>("SubscribeHouseholdPreparation",id,device,"team-01");
        store.Armed=true;
        Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(publicUri+"/freeze",freeze)).StatusCode);
        Assert.False(updates.Reader.TryRead(out _));
        if(!commitBeforeFailure)Assert.Equal(ready,await client.GetStringAsync(publicUri));
        var response=await client.PostAsJsonAsync(publicUri+"/freeze",freeze);response.EnsureSuccessStatusCode();var receipt=await response.Content.ReadAsStringAsync();
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.Equal("locked",(await updates.Reader.ReadAsync(deadline.Token)).GetProperty("lifecycle").GetString());
        var frozen=await client.GetStringAsync(publicUri);
        Assert.Equal(receipt,await(await client.PostAsJsonAsync(publicUri+"/freeze",freeze)).Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(publicUri+"/freeze",new{commandId=Guid.NewGuid().ToString(),revision=freeze.revision+1})).StatusCode);
        Assert.Equal("preparation-locked",(await client.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}")).GetProperty("lifecycle").GetString());
        foreach(var (uri,command,ack) in confirmations)
        {
            Assert.Equal(ack,await(await client.PostAsJsonAsync(uri.Replace("?","/confirm?"),command)).Content.ReadAsStringAsync());
            var team=await client.GetFromJsonAsync<JsonElement>(uri);Assert.Equal("locked",team.GetProperty("lifecycle").GetString());
            var change=new{commandId=Guid.NewGuid().ToString(),revision=team.GetProperty("revision").GetInt64(),assignmentRevision=team.GetProperty("assignmentRevision").GetInt64(),setup=new{aero=1,mechanical=1,tyres=1}};
            foreach(var operation in new[]{"setup","practice","confirm","reopen"})
                Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(uri.Replace("?","/"+operation+"?"),change)).StatusCode);
        }
        Assert.Equal(frozen,await client.GetStringAsync(publicUri));
        await using var restarted=LobbyTests.Server().WithWebHostBuilder(builder=>builder.ConfigureServices(services=>services.Replace(ServiceDescriptor.Singleton<ISessionStore>(store))));
        using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(frozen,await recovered.GetStringAsync(publicUri));
        Assert.Equal(receipt,await(await recovered.PostAsJsonAsync(publicUri+"/freeze",freeze)).Content.ReadAsStringAsync());
        Assert.DoesNotContain("assessment",frozen);Assert.DoesNotContain("preset",frozen);
    }

    private sealed class PreparationFailureStore(ISessionStore inner,bool commitBeforeFailure,bool failRead):ISessionStore
    {
        public bool Armed {get;set;}
        private bool readOutage;
        public StoredSession? Read(string id)
        { if(readOutage){readOutage=false;throw new IOException("Interrupted read");}return inner.Read(id); }
        public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
        public string Write(string id,string? version,string payload,string? code)
        {
            if(Armed){Armed=false;if(commitBeforeFailure)inner.Write(id,version,payload,code);readOutage=failRead;throw new IOException("Interrupted save");}
            return inner.Write(id,version,payload,code);
        }
    }
    [Fact]
    public async Task Authorised_setup_is_complete_atomic_idempotent_private_and_recovers_without_a_race()
    {
        await using var server = LobbyTests.Server();
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await LobbyTests.Identity(false));
        var joined = await (await client.PostAsJsonAsync("/api/lobbies", new { preset="open-equal", laps=6, seed=123, mode="Competitive", name="Host" })).Content.ReadFromJsonAsync<JsonElement>();
        var id = joined.GetProperty("lobby").GetProperty("sessionId").GetString();
        var device = joined.GetProperty("membership").GetProperty("deviceId").GetString();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims", new {teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
        var participants=await(await client.PostAsJsonAsync($"/api/lobbies/{id}/participants",new{name="Tablet player",deviceId=device})).Content.ReadFromJsonAsync<JsonElement>();
        var otherPlayer=participants.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()=="Tablet player").GetProperty("playerId").GetString();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-02",playerId=otherPlayer,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/preparation", new {circuitId="track-monza"})).EnsureSuccessStatusCode();
        var uri = $"/api/lobbies/{id}/preparation/teams/team-01?deviceId={device}";
        var before = await client.GetFromJsonAsync<JsonElement>(uri);
        var revision = before.GetProperty("revision").GetInt64();
        var assignmentRevision = before.GetProperty("assignmentRevision").GetInt64();
        var other=await client.GetFromJsonAsync<JsonElement>(uri.Replace("team-01","team-02"));
        (await client.PostAsJsonAsync(uri.Replace("team-01","team-02").Replace("?","/setup?"),new{commandId=Guid.NewGuid().ToString(),revision=other.GetProperty("revision").GetInt64(),assignmentRevision=other.GetProperty("assignmentRevision").GetInt64(),setup=new{aero=2,mechanical=2,tyres=2}})).EnsureSuccessStatusCode();
        var commandId = Guid.NewGuid().ToString();
        var command = new {commandId,revision,assignmentRevision,setup=new{aero=-2,mechanical=1,tyres=2}};
        var ack = await client.PostAsJsonAsync(uri.Replace("?", "/setup?"),command); ack.EnsureSuccessStatusCode();
        var first = await ack.Content.ReadAsStringAsync();
        Assert.Equal(first, await (await client.PostAsJsonAsync(uri.Replace("?", "/setup?"),command)).Content.ReadAsStringAsync());
        var saved = await client.GetStringAsync(uri);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(uri.Replace("?", "/setup?"),new {commandId=Guid.NewGuid().ToString(),revision,assignmentRevision,setup=new{aero=0,mechanical=0,tyres=0}})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(uri.Replace("?", "/setup?"),new {commandId=Guid.NewGuid().ToString(),revision=revision+1,assignmentRevision,setup=new{aero=0,mechanical=0}})).StatusCode);
        Assert.Equal(saved,await client.GetStringAsync(uri));
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(uri.Replace("?", "/setup?"),new {commandId,revision,assignmentRevision,setup=new{aero=0,mechanical=0,tyres=0}})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(uri.Replace("?", "/setup?"),new {commandId=Guid.NewGuid().ToString(),revision=revision+1,assignmentRevision,setup=new{aero=3,mechanical=0,tyres=0}})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(uri.Replace(device!,"unassigned-device"))).StatusCode);
        Assert.Equal(saved,await client.GetStringAsync(uri));
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/lobbies/{id}/preparation/open/team-01")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).StatusCode);
        var publicText=await client.GetStringAsync($"/api/lobbies/{id}/preparation");
        Assert.DoesNotContain("driverFits",publicText); Assert.DoesNotContain("adaptability",publicText);
        Assert.Equal(2, (await client.GetFromJsonAsync<JsonElement>(uri)).GetProperty("driverFits").GetArrayLength());
    }

    [Fact]
    public async Task Confirmed_practice_is_one_time_private_and_stable_after_final_candidate_edits()
    {
        await using var server=LobbyTests.Server();using var client=server.CreateClient();
        client.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(false));
        var joined=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
        var id=joined.GetProperty("lobby").GetProperty("sessionId").GetString();var device=joined.GetProperty("membership").GetProperty("deviceId").GetString();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monza"})).EnsureSuccessStatusCode();
        var uri=$"/api/lobbies/{id}/preparation/teams/team-01?deviceId={device}";
        var state=await client.GetFromJsonAsync<JsonElement>(uri);var revision=state.GetProperty("revision").GetInt64();var assignmentRevision=state.GetProperty("assignmentRevision").GetInt64();
        var unchanged=await client.GetStringAsync(uri);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(uri.Replace("?","/practice?"),new{commandId="not-a-guid",revision,assignmentRevision})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(uri.Replace("?","/practice?"),new{commandId=Guid.NewGuid().ToString(),revision=revision+1,assignmentRevision})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(uri.Replace("?","/practice?"),new{commandId=Guid.NewGuid().ToString(),revision,assignmentRevision=assignmentRevision+1})).StatusCode);
        Assert.Equal(unchanged,await client.GetStringAsync(uri));
        var commandId=Guid.NewGuid();var command=new{commandId=commandId.ToString("B"),revision,assignmentRevision};
        var response=await client.PostAsJsonAsync(uri.Replace("?","/practice?"),command);response.EnsureSuccessStatusCode();var ack=await response.Content.ReadAsStringAsync();
        Assert.Equal(ack,await(await client.PostAsJsonAsync(uri.Replace("?","/practice?"),new{commandId=commandId.ToString("D"),revision,assignmentRevision})).Content.ReadAsStringAsync());
        var assessed=await client.GetFromJsonAsync<JsonElement>(uri);var feedback=assessed.GetProperty("assessment").GetRawText();
        Assert.Equal("assessed",assessed.GetProperty("lifecycle").GetString());Assert.Equal(2,assessed.GetProperty("assessment").GetProperty("drivers").GetArrayLength());
        Assert.DoesNotContain("seed",feedback,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("diagnostics",feedback,StringComparison.OrdinalIgnoreCase);
        var confirmed=await client.GetStringAsync(uri);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(uri.Replace("?","/setup?"),new{commandId=commandId.ToString("D"),revision=assessed.GetProperty("revision").GetInt64(),assignmentRevision,setup=new{aero=2,mechanical=1,tyres=-2}})).StatusCode);
        Assert.Equal(confirmed,await client.GetStringAsync(uri));
        (await client.PostAsJsonAsync(uri.Replace("?","/setup?"),new{commandId=Guid.NewGuid().ToString(),revision=assessed.GetProperty("revision").GetInt64(),assignmentRevision,setup=new{aero=2,mechanical=1,tyres=-2}})).EnsureSuccessStatusCode();
        var edited=await client.GetFromJsonAsync<JsonElement>(uri);Assert.Equal(feedback,edited.GetProperty("assessment").GetRawText());
        var finalCommand=new{commandId=Guid.NewGuid().ToString(),revision=edited.GetProperty("revision").GetInt64(),assignmentRevision};
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync(uri.Replace("?","/confirm?"),new{revision=finalCommand.revision,assignmentRevision})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(uri.Replace("?","/confirm?"),new{commandId=Guid.NewGuid().ToString(),revision=finalCommand.revision-1,assignmentRevision})).StatusCode);
        var finalResponse=await client.PostAsJsonAsync(uri.Replace("?","/confirm?"),finalCommand);finalResponse.EnsureSuccessStatusCode();
        var finalAck=await finalResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(uri.Replace("?","/confirm?"),new{commandId=finalCommand.commandId,revision=finalCommand.revision+1,assignmentRevision})).StatusCode);
        Assert.Equal("final-ready",(await client.GetFromJsonAsync<JsonElement>(uri)).GetProperty("lifecycle").GetString());
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(uri.Replace("?","/setup?"),new{commandId=Guid.NewGuid().ToString(),revision=finalCommand.revision+1,assignmentRevision,setup=new{aero=0,mechanical=0,tyres=0}})).StatusCode);
        (await client.PostAsJsonAsync(uri.Replace("?","/reopen?"),new{commandId=Guid.NewGuid().ToString(),revision=finalCommand.revision+1,assignmentRevision})).EnsureSuccessStatusCode();
        Assert.Equal(finalAck,await(await client.PostAsJsonAsync(uri.Replace("?","/confirm?"),finalCommand)).Content.ReadAsStringAsync());
        var reopened=await client.GetFromJsonAsync<JsonElement>(uri);
        Assert.Equal("assessed",reopened.GetProperty("lifecycle").GetString());Assert.Equal(feedback,reopened.GetProperty("assessment").GetRawText());
        Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(uri.Replace("?","/practice?"),new{commandId=Guid.NewGuid().ToString(),revision=edited.GetProperty("revision").GetInt64(),assignmentRevision})).StatusCode);
        Assert.Equal(ack,await(await client.PostAsJsonAsync(uri.Replace("?","/practice?"),command)).Content.ReadAsStringAsync());
        Assert.DoesNotContain("assessment",await client.GetStringAsync($"/api/lobbies/{id}/preparation"));
    }

    [Fact]
    public async Task Saved_preparation_and_identical_ack_survive_server_restart_with_real_durable_store()
    {
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Durable()=>LobbyTests.Server().WithWebHostBuilder(builder=>
            builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>
            { ["Persistence:ProjectId"]="demo-f1game", ["Persistence:EmulatorHost"]=Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST") })));
        var token=await LobbyTests.Identity(false);
        await using var original=Durable();using var first=original.CreateClient();first.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var joined=await(await first.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=73,mode="Mixed",name="Persistent host"})).Content.ReadFromJsonAsync<JsonElement>();
        var id=joined.GetProperty("lobby").GetProperty("sessionId").GetString();var device=joined.GetProperty("membership").GetProperty("deviceId").GetString();
        (await first.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-04",deviceId=device})).EnsureSuccessStatusCode();
        (await first.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monaco"})).EnsureSuccessStatusCode();
        var uri=$"/api/lobbies/{id}/preparation/teams/team-04?deviceId={device}";
        var state=await first.GetFromJsonAsync<JsonElement>(uri);
        var command=new{commandId=Guid.NewGuid().ToString(),revision=state.GetProperty("revision").GetInt64(),assignmentRevision=state.GetProperty("assignmentRevision").GetInt64(),setup=new{aero=2,mechanical=-1,tyres=-2}};
        var response=await first.PostAsJsonAsync(uri.Replace("?","/setup?"),command);response.EnsureSuccessStatusCode();var ack=await response.Content.ReadAsStringAsync();var saved=await first.GetStringAsync(uri);
        var practiceCommand=new{commandId=Guid.NewGuid().ToString(),revision=state.GetProperty("revision").GetInt64()+1,assignmentRevision=state.GetProperty("assignmentRevision").GetInt64()};
        var practiceResponse=await first.PostAsJsonAsync(uri.Replace("?","/practice?"),practiceCommand);practiceResponse.EnsureSuccessStatusCode();var practiceAck=await practiceResponse.Content.ReadAsStringAsync();saved=await first.GetStringAsync(uri);
        await using var restarted=Durable();using var second=restarted.CreateClient();second.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(saved,await second.GetStringAsync(uri));
        var retried=await second.PostAsJsonAsync(uri.Replace("?","/setup?"),command);retried.EnsureSuccessStatusCode();Assert.Equal(ack,await retried.Content.ReadAsStringAsync());
        var practiceRetry=await second.PostAsJsonAsync(uri.Replace("?","/practice?"),practiceCommand);practiceRetry.EnsureSuccessStatusCode();Assert.Equal(practiceAck,await practiceRetry.Content.ReadAsStringAsync());
        Assert.Equal(saved,await second.GetStringAsync(uri));
        Assert.Equal("preparing",(await second.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}")).GetProperty("lifecycle").GetString());
    }

    [Fact]
    public async Task Private_signalr_stream_observes_saved_revision_and_revoked_device_cannot_retry()
    {
        var token=await LobbyTests.Identity(false);await using var server=LobbyTests.Server();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var joined=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=4,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
        var id=joined.GetProperty("lobby").GetProperty("sessionId").GetString()!;var device=joined.GetProperty("membership").GetProperty("deviceId").GetString()!;
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monza"})).EnsureSuccessStatusCode();
        await using var hub=new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();options.AccessTokenProvider=()=>Task.FromResult<string?>(token);}).Build();
        var updates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();hub.On<JsonElement>("preparationTeamSnapshot",snapshot=>updates.Writer.TryWrite(snapshot));await hub.StartAsync();
        await Assert.ThrowsAsync<HubException>(()=>hub.InvokeAsync<JsonElement>("SubscribeOpenPreparation",id,"team-01"));
        var state=await hub.InvokeAsync<JsonElement>("SubscribeHouseholdPreparation",id,device,"team-01");
        var command=new{commandId=Guid.NewGuid().ToString(),revision=state.GetProperty("revision").GetInt64(),assignmentRevision=state.GetProperty("assignmentRevision").GetInt64(),setup=new{aero=1,mechanical=-2,tyres=0}};
        var uri=$"/api/lobbies/{id}/preparation/teams/team-01/setup?deviceId={device}";
        (await client.PostAsJsonAsync(uri,command)).EnsureSuccessStatusCode();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var update=await updates.Reader.ReadAsync(timeout.Token);Assert.Equal(2,update.GetProperty("revision").GetInt64());Assert.Equal(-2,update.GetProperty("setup").GetProperty("mechanical").GetInt32());
        var practiceCommand=new{commandId=Guid.NewGuid().ToString(),revision=update.GetProperty("revision").GetInt64(),assignmentRevision=update.GetProperty("assignmentRevision").GetInt64()};
        var practiceUri=uri.Replace("/setup?","/practice?");
        (await client.PostAsJsonAsync(practiceUri,practiceCommand)).EnsureSuccessStatusCode();
        var assessed=await updates.Reader.ReadAsync(timeout.Token);Assert.Equal("assessed",assessed.GetProperty("lifecycle").GetString());
        Assert.Equal((await client.GetFromJsonAsync<JsonElement>(uri.Replace("/setup?","?"))).GetProperty("assessment").GetRawText(),assessed.GetProperty("assessment").GetRawText());
        Assert.DoesNotContain("seed",assessed.GetProperty("assessment").GetRawText(),StringComparison.OrdinalIgnoreCase);
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/devices/{device}/revoke",new{})).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(practiceUri,practiceCommand)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(uri,command)).StatusCode);
        await Assert.ThrowsAsync<HubException>(()=>hub.InvokeAsync<JsonElement>("SubscribeHouseholdPreparation",id,device,"team-01"));
    }
}
