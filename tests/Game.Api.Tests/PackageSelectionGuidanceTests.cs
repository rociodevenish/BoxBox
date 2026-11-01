using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Game.Simulation;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace Game.Api.Tests;
public sealed class PackageSelectionGuidanceTests
{
 [Fact]
 public async Task Selection_advice_is_privileged_free_retryable_and_separately_replayed_after_restart()
 {
  Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Server()=>LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["Persistence:ProjectId"]="demo-f1game",["Persistence:EmulatorHost"]=Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"),["Designer:Key"]="selection-test"})));
  var token=await LobbyTests.Identity(false);await using var server=Server();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Selection host"})).Content.ReadFromJsonAsync<JsonElement>();
  var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();var device=created.GetProperty("membership").GetProperty("deviceId").GetString();var player=created.GetProperty("membership").GetProperty("playerId").GetString();
  (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
  (await client.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monza"})).EnsureSuccessStatusCode();
  var uri=$"/api/lobbies/{id}/preparation/teams/team-01?deviceId={device}";var route=uri.Replace("?","/package?");
  var state=await client.GetFromJsonAsync<JsonElement>(uri);
  Assert.Equal(JsonValueKind.Null,state.GetProperty("guidance").GetProperty("packageRecommendation").ValueKind);
  object Command(JsonElement value)=>new{commandId=Guid.NewGuid().ToString(),revision=value.GetProperty("revision").GetInt64(),assignmentRevision=value.GetProperty("assignmentRevision").GetInt64(),selection=new{packageId="neutral-package-v1",version=1},useRecommendation=true};
  Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(route,Command(state))).StatusCode);
  (await client.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Assisted",autoAcceptRecommendation=false})).EnsureSuccessStatusCode();
  state=await client.GetFromJsonAsync<JsonElement>(uri);
  Assert.Equal("public-package-demand-fit-v1",state.GetProperty("guidance").GetProperty("packageRecommendation").GetProperty("policyVersion").GetString());
  var command=Command(state);(await client.PostAsJsonAsync(route,command)).EnsureSuccessStatusCode();
  var saved=await client.GetStringAsync(uri);var selected=JsonDocument.Parse(saved).RootElement;
  Assert.Equal("assisted",selected.GetProperty("guidance").GetProperty("packageChoices")[0].GetProperty("source").GetString());
  Assert.Equal(JsonValueKind.Null,selected.GetProperty("assessment").ValueKind);
  Assert.DoesNotContain("packageRecommendation",await client.GetStringAsync($"/api/lobbies/{id}/preparation"));
  client.DefaultRequestHeaders.Add("X-Designer-Key","selection-test");
  var records=await client.GetFromJsonAsync<PreparationAutomaticRecord[]>($"/api/designer/lobbies/{id}/preparation");
  Assert.Equal(10,records!.Length);
  foreach(var record in records){var selection=Assert.Single(record.PackageChoices!);Assert.Equal("automatic",selection.Source);Assert.Equal(selection.Recommendation.Selection,selection.Selection);Assert.Equal("neutral-package-v1",selection.Selection.PackageId);Assert.Equal(2,record.Choices.Count);}
  await using var restart=Server();using var recovered=restart.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
  Assert.Equal(saved,await recovered.GetStringAsync(uri));(await recovered.PostAsJsonAsync(route,command)).EnsureSuccessStatusCode();Assert.Equal(saved,await recovered.GetStringAsync(uri));
  (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/players/{player}/assistance",new{helpLevel="Standard",autoAcceptRecommendation=false})).EnsureSuccessStatusCode();
  var standard=await recovered.GetFromJsonAsync<JsonElement>(uri);Assert.Equal(JsonValueKind.Null,standard.GetProperty("guidance").GetProperty("packageChoices").ValueKind);
 }
}
