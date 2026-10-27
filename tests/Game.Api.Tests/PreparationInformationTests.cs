using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
namespace Game.Api.Tests;

public sealed class PreparationInformationTests
{
    [Fact]
    public async Task Private_and_open_preparation_expose_preferences_and_one_recorded_observation_never_exact_fit()
    {
        var token=await LobbyTests.Identity(false);await using var server=LobbyTests.Server();using var client=server.CreateClient();
        client.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var joined=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=41,mode="Family",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
        var id=joined.GetProperty("lobby").GetProperty("sessionId").GetString();var membership=joined.GetProperty("membership");
        var device=membership.GetProperty("deviceId").GetString();var player=membership.GetProperty("playerId").GetString();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-01",playerId=player,deviceId=device,control="Open"})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monza"})).EnsureSuccessStatusCode();
        var uri=$"/api/lobbies/{id}/preparation/teams/team-01?deviceId={device}";var openUri=$"/api/lobbies/{id}/preparation/open/team-01";
        HubConnection Connection(bool authenticated)=>new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>
        {options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();if(authenticated)options.AccessTokenProvider=()=>Task.FromResult<string?>(token);}).Build();
        await using var household=Connection(true);await using var open=Connection(false);
        var ownUpdates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();var openUpdates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();
        household.On<JsonElement>("preparationTeamSnapshot",s=>ownUpdates.Writer.TryWrite(s));open.On<JsonElement>("preparationTeamSnapshot",s=>openUpdates.Writer.TryWrite(s));
        await household.StartAsync();await open.StartAsync();
        var state=await household.InvokeAsync<JsonElement>("SubscribeHouseholdPreparation",id,device,"team-01");
        NoDiagnostics(state);NoDiagnostics(await open.InvokeAsync<JsonElement>("SubscribeOpenPreparation",id,"team-01"));
        NoDiagnostics(await client.GetFromJsonAsync<JsonElement>(uri));NoDiagnostics(await client.GetFromJsonAsync<JsonElement>(openUri));
        var preferences=state.GetProperty("preset").GetProperty("cars").GetRawText();
        Assert.Equal(JsonValueKind.Null,state.GetProperty("assessment").ValueKind);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(20));
        async Task<JsonElement> Saved(string operation,object command)
        {
            (await client.PostAsJsonAsync(uri.Replace("?","/"+operation+"?"),command)).EnsureSuccessStatusCode();
            var own=await ownUpdates.Reader.ReadAsync(deadline.Token);var publicOpen=await openUpdates.Reader.ReadAsync(deadline.Token);
            NoDiagnostics(own);NoDiagnostics(publicOpen);
            Assert.Equal(own.GetRawText(),publicOpen.GetRawText());
            var current=await client.GetFromJsonAsync<JsonElement>(uri);NoDiagnostics(current);NoDiagnostics(await client.GetFromJsonAsync<JsonElement>(openUri));
            Assert.Equal(current.GetRawText(),own.GetRawText());return current;
        }
        state=await Saved("setup",new{commandId=Guid.NewGuid().ToString(),revision=state.GetProperty("revision").GetInt64(),assignmentRevision=state.GetProperty("assignmentRevision").GetInt64(),setup=new{aero=0,mechanical=-2,tyres=0}});
        Assert.Equal(preferences,state.GetProperty("preset").GetProperty("cars").GetRawText());
        state=await Saved("practice",new{commandId=Guid.NewGuid().ToString(),revision=state.GetProperty("revision").GetInt64(),assignmentRevision=state.GetProperty("assignmentRevision").GetInt64()});
        var feedback=state.GetProperty("assessment").GetRawText();
        Assert.All(state.GetProperty("assessment").GetProperty("drivers").EnumerateArray(),driver=>
        {Assert.False(string.IsNullOrWhiteSpace(driver.GetProperty("confidence").GetString()));Assert.False(string.IsNullOrWhiteSpace(driver.GetProperty("uncertainty").GetString()));});
        foreach(var mechanical in new[]{-1,0,1,2})
        {
            state=await Saved("setup",new{commandId=Guid.NewGuid().ToString(),revision=state.GetProperty("revision").GetInt64(),assignmentRevision=state.GetProperty("assignmentRevision").GetInt64(),setup=new{aero=0,mechanical,tyres=0}});
            Assert.Equal(feedback,state.GetProperty("assessment").GetRawText());Assert.Equal(preferences,state.GetProperty("preset").GetProperty("cars").GetRawText());
        }
    }

    private static void NoDiagnostics(JsonElement snapshot)
    {
        var text=snapshot.GetRawText();
        Assert.DoesNotContain("driverFits",text,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"compatibility\"",text,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exactFits",text,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("diagnostics",text,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("seed",text,StringComparison.OrdinalIgnoreCase);
        Assert.All(snapshot.GetProperty("preset").GetProperty("cars").EnumerateArray(),car=>
        {Assert.NotEmpty(car.GetProperty("driver").GetProperty("preferences").EnumerateArray());Assert.True(car.GetProperty("driver").GetProperty("ratings").GetProperty("adaptability").GetDouble()>0);});
    }
}
