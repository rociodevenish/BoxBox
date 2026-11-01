using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Api;
using Game.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public class RacePackageSelectionTests
{
 [Fact]
 public async Task Free_selection_is_private_retryable_durable_and_qualifying_locked()
 {
  var store=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
  var token=await LobbyTests.Identity(false);
  Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Server()=>LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store))));
  await using var server=Server();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Package host"})).Content.ReadFromJsonAsync<JsonElement>();
  var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();var device=created.GetProperty("membership").GetProperty("deviceId").GetString();
  (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
  (await client.PostAsJsonAsync($"/api/lobbies/{id}/preparation",new{circuitId="track-monza"})).EnsureSuccessStatusCode();
  var uri=$"/api/lobbies/{id}/preparation/teams/team-01?deviceId={device}";
  async Task<JsonElement> Current()=>await client.GetFromJsonAsync<JsonElement>(uri);
  object Command(JsonElement state,string package="neutral-package-v1")=>new{commandId=Guid.NewGuid().ToString(),revision=state.GetProperty("revision").GetInt64(),assignmentRevision=state.GetProperty("assignmentRevision").GetInt64(),selection=new{packageId=package,version=1}};
  var initial=await Current();Assert.Single(initial.GetProperty("preset").GetProperty("packageLibrary").EnumerateArray());
  var route=uri.Replace("?","/package?");var command=Command(initial);
  (await client.PostAsJsonAsync(route,command)).EnsureSuccessStatusCode();var saved=await client.GetStringAsync(uri);
  Assert.DoesNotContain("packageLibrary",await client.GetStringAsync($"/api/lobbies/{id}/preparation"));
  await using var restart=Server();using var recovered=restart.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
  Assert.Equal(saved,await recovered.GetStringAsync(uri));
  var retry=await recovered.PostAsJsonAsync(route,command);retry.EnsureSuccessStatusCode();Assert.Equal(saved,await recovered.GetStringAsync(uri));
  Assert.Equal(HttpStatusCode.BadRequest,(await recovered.PostAsJsonAsync(route,Command(await recovered.GetFromJsonAsync<JsonElement>(uri),"missing"))).StatusCode);
  async Task Apply(string action){var state=await recovered.GetFromJsonAsync<JsonElement>(uri);(await recovered.PostAsJsonAsync(uri.Replace("?","/"+action+"?"),new{commandId=Guid.NewGuid().ToString(),revision=state.GetProperty("revision").GetInt64(),assignmentRevision=state.GetProperty("assignmentRevision").GetInt64()})).EnsureSuccessStatusCode();}
  await Apply("practice");await Apply("confirm");
  Assert.Equal(HttpStatusCode.Conflict,(await recovered.PostAsJsonAsync(route,Command(await recovered.GetFromJsonAsync<JsonElement>(uri)))).StatusCode);
  var publicState=await recovered.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/preparation");
  (await recovered.PostAsJsonAsync($"/api/lobbies/{id}/preparation/freeze",new{commandId=Guid.NewGuid().ToString(),revision=publicState.GetProperty("revision").GetInt64()})).EnsureSuccessStatusCode();
  Assert.Equal(HttpStatusCode.Conflict,(await recovered.PostAsJsonAsync(route,Command(await recovered.GetFromJsonAsync<JsonElement>(uri)))).StatusCode);
 }
}
