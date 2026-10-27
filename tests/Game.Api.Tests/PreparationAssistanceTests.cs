using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Xunit;
using Game.Application;
using Game.Simulation;
using Microsoft.AspNetCore.SignalR.Client;
namespace Game.Api.Tests;

public sealed class PreparationAssistanceTests
{
    [Fact]
    public async Task Shared_players_keep_separate_guidance_and_revocation_closes_private_signalr_delivery()
    {
        var token=await LobbyTests.Identity(false);await using var server=LobbyTests.Server();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var joined=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=27,mode="Mixed",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
        var id=joined.GetProperty("lobby").GetProperty("sessionId").GetString();var device=joined.GetProperty("membership").GetProperty("deviceId").GetString();
        var hostPlayer=joined.GetProperty("membership").GetProperty("playerId").GetString();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
        var people=await(await client.PostAsJsonAsync($"/api/lobbies/{id}/participants",new{name="Shared player",deviceId=device})).Content.ReadFromJsonAsync<JsonElement>();
        var player=people.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()=="Shared player").GetProperty("playerId").GetString();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-02",playerId=player,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Guided",autoAcceptRecommendation=false})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monza"})).EnsureSuccessStatusCode();
        var uri=$"/api/lobbies/{id}/preparation/teams/team-02?deviceId={device}";
        var shared=await client.GetFromJsonAsync<JsonElement>(uri);
        Assert.Equal("Guided",shared.GetProperty("guidance").GetProperty("helpLevel").GetString());
        var own=await client.GetFromJsonAsync<JsonElement>(uri.Replace("team-02","team-01"));
        Assert.Equal("Standard",own.GetProperty("guidance").GetProperty("helpLevel").GetString());
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/lobbies/{id}/preparation/open/team-02")).StatusCode);
        await using var hub=new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();options.AccessTokenProvider=()=>Task.FromResult<string?>(token);}).Build();
        var updates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();hub.On<JsonElement>("preparationTeamSnapshot",snapshot=>updates.Writer.TryWrite(snapshot));await hub.StartAsync();
        await hub.InvokeAsync<JsonElement>("SubscribeHouseholdPreparation",id,device,"team-02");
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Assisted",autoAcceptRecommendation=true})).EnsureSuccessStatusCode();
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));var update=await updates.Reader.ReadAsync(deadline.Token);
        Assert.Equal("Assisted",update.GetProperty("guidance").GetProperty("helpLevel").GetString());
        Assert.Equal(shared.GetProperty("setup").GetRawText(),update.GetProperty("setup").GetRawText());
        Assert.Equal(shared.GetProperty("revision").GetInt64(),update.GetProperty("revision").GetInt64());
        Assert.Equal("Standard",(await client.GetFromJsonAsync<JsonElement>(uri.Replace("team-02","team-01"))).GetProperty("guidance").GetProperty("helpLevel").GetString());
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/devices/{device}/revoke",new{})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Guided",autoAcceptRecommendation=false})).EnsureSuccessStatusCode();
        Assert.False(updates.Reader.TryRead(out _));
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(uri)).StatusCode);
        await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(()=>hub.InvokeAsync<JsonElement>("SubscribeHouseholdPreparation",id,device,"team-02"));
    }

    [Fact]
    public async Task Unclaimed_teams_prepare_once_and_human_guidance_is_private_observation_only_and_durable()
    {
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Durable()=>LobbyTests.Server().WithWebHostBuilder(builder=>
            builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>
            { ["Persistence:ProjectId"]="demo-f1game",["Persistence:EmulatorHost"]=Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"),["Designer:Key"]="preparation-test-diagnostics" })));
        var token=await LobbyTests.Identity(false);await using var server=Durable();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
        var joined=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
        var id=joined.GetProperty("lobby").GetProperty("sessionId").GetString();var member=joined.GetProperty("membership");var device=member.GetProperty("deviceId").GetString();var player=member.GetProperty("playerId").GetString();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Guided",autoAcceptRecommendation=true})).EnsureSuccessStatusCode();
        var begin=await client.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monza"});begin.EnsureSuccessStatusCode();
        var publicState=await begin.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10,publicState.GetProperty("readiness").EnumerateArray().Count(t=>t.GetProperty("finalReady").GetBoolean()));
        var recordsUri=$"/api/designer/lobbies/{id}/preparation";
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(recordsUri)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Designer-Key","preparation-test-diagnostics");
        var records=await client.GetFromJsonAsync<PreparationAutomaticRecord[]>(recordsUri);Assert.Equal(10,records!.Length);
        var recorded=await client.GetStringAsync(recordsUri);
        foreach(var record in records)
        {
            Assert.Equal("unclaimed-ai",record.Source);Assert.Equal(new[]{"provisional","final"},record.Choices.Select(c=>c.Stage));
            Assert.Null(record.Choices[0].Observation.Practice);Assert.NotNull(record.Choices[1].Observation.Practice);
            foreach(var choice in record.Choices)
            {
                Assert.Equal("preparation-observation-v1",choice.Recommendation.PolicyVersion);
                Assert.Contains("2026.10.08",choice.Observation.SourceVersion);
                Assert.Equal(-1,choice.Recommendation.Setup.Aero);Assert.Equal(-1,choice.Recommendation.Setup.Tyres);
                choice.Recommendation.Setup.Validate();
                Assert.Equal(JsonSerializer.Serialize(choice.Recommendation),JsonSerializer.Serialize(PreparationDecisionPolicy.Evaluate(choice.Observation)));
            }
        }
        Assert.DoesNotContain("seed",recorded,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("exactFits",recorded,StringComparison.OrdinalIgnoreCase);
        var uri=$"/api/lobbies/{id}/preparation/teams/team-01?deviceId={device}";
        var human=await client.GetFromJsonAsync<JsonElement>(uri);Assert.Equal("provisional",human.GetProperty("lifecycle").GetString());
        var recommendation=human.GetProperty("guidance").GetProperty("recommendation").GetRawText();
        Assert.Equal("Guided",human.GetProperty("guidance").GetProperty("helpLevel").GetString());
        Assert.Equal(2,human.GetProperty("guidance").GetProperty("recommendation").GetProperty("drivers").GetArrayLength());
        Assert.DoesNotContain("diagnostics",recommendation,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("seed",recommendation,StringComparison.OrdinalIgnoreCase);
        var revision=human.GetProperty("revision").GetInt64();var assignmentRevision=human.GetProperty("assignmentRevision").GetInt64();
        var saved=await client.PostAsJsonAsync(uri.Replace("?","/setup?"),new{commandId=Guid.NewGuid().ToString(),revision,assignmentRevision,setup=new{aero=2,mechanical=2,tyres=2}});saved.EnsureSuccessStatusCode();
        var changed=await client.GetFromJsonAsync<JsonElement>(uri);
        // Compare designer diagnostics only through the approved public simulation seam.
        var source=JsonSerializer.Deserialize<PreparationTeamSnapshot>(human.GetRawText(),new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var savedCandidate=JsonSerializer.Deserialize<PreparationTeamSnapshot>(changed.GetRawText(),new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.NotEqual(JsonSerializer.Serialize(WeekendPreparation.Prepare(source.Preset,6,source.Setup).DriverFits),
            JsonSerializer.Serialize(WeekendPreparation.Prepare(savedCandidate.Preset,6,savedCandidate.Setup).DriverFits));
        Assert.False(changed.TryGetProperty("driverFits",out _));
        Assert.Equal(recommendation,changed.GetProperty("guidance").GetProperty("recommendation").GetRawText());
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Standard",autoAcceptRecommendation=false})).EnsureSuccessStatusCode();
        var standard=await client.GetFromJsonAsync<JsonElement>(uri);Assert.Equal(JsonValueKind.Null,standard.GetProperty("guidance").GetProperty("recommendation").ValueKind);
        Assert.Equal(changed.GetProperty("setup").GetRawText(),standard.GetProperty("setup").GetRawText());Assert.Equal(changed.GetProperty("revision").GetInt64(),standard.GetProperty("revision").GetInt64());
        (await client.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Assisted",autoAcceptRecommendation=true})).EnsureSuccessStatusCode();
        var assessed=await client.PostAsJsonAsync(uri.Replace("?","/practice?"),new{commandId=Guid.NewGuid().ToString(),revision=changed.GetProperty("revision").GetInt64(),assignmentRevision});assessed.EnsureSuccessStatusCode();
        var candidate=await client.GetFromJsonAsync<JsonElement>(uri);var feedback=candidate.GetProperty("assessment").GetRawText();
        (await client.PostAsJsonAsync(uri.Replace("?","/confirm?"),new{commandId=Guid.NewGuid().ToString(),revision=candidate.GetProperty("revision").GetInt64(),assignmentRevision})).EnsureSuccessStatusCode();
        var ready=await client.GetStringAsync($"/api/lobbies/{id}/preparation");
        Assert.DoesNotContain("recommendation",ready);Assert.DoesNotContain("guidance",ready);Assert.DoesNotContain("automatic",ready);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync(uri.Replace("team-01","team-02"))).StatusCode);
        await using var restarted=Durable();using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
        Assert.Equal(ready,await recovered.GetStringAsync($"/api/lobbies/{id}/preparation"));
        recovered.DefaultRequestHeaders.Add("X-Designer-Key","preparation-test-diagnostics");Assert.Equal(recorded,await recovered.GetStringAsync(recordsUri));
        Assert.Equal(feedback,(await recovered.GetFromJsonAsync<JsonElement>(uri)).GetProperty("assessment").GetRawText());
        var retry=await recovered.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monza"});retry.EnsureSuccessStatusCode();Assert.Equal(ready,await retry.Content.ReadAsStringAsync());
        publicState=await recovered.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/preparation");
        (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/preparation/freeze",new{commandId=Guid.NewGuid().ToString(),revision=publicState.GetProperty("revision").GetInt64()})).EnsureSuccessStatusCode();
    }
}
