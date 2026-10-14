using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR;
using Xunit;
namespace Game.Api.Tests;

public sealed class PreparationTests
{
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
