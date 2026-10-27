using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using System.Threading.Channels;
namespace Game.Api.Tests;
public sealed class SharedTabletTests
{
 [Fact]
 public async Task Shared_device_lists_only_its_explicit_assignments_and_unsubscribes_previous_private_stream()
 {
  await using var server=LobbyTests.Server();using var host=server.CreateClient();var token=await LobbyTests.Identity(false);host.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await(await host.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();var device=created.GetProperty("membership").GetProperty("deviceId").GetString();
  foreach(var (name,team) in new[]{("Alex","team-01"),("Sam","team-02")}){var lobby=await(await host.PostAsJsonAsync($"/api/lobbies/{id}/participants",new{name,deviceId=device})).Content.ReadFromJsonAsync<JsonElement>();var player=lobby.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()==name).GetProperty("playerId").GetString();(await host.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId=team,playerId=player,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();}
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
  var access=await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/controllers?deviceId={device}");Assert.Equal(new[]{"team-01","team-02"},access.EnumerateArray().Select(t=>t.GetProperty("teamId").GetString()));Assert.DoesNotContain("estimatedTyreCondition",access.GetRawText());Assert.Equal("McLaren",access[0].GetProperty("constructorName").GetString());Assert.NotEqual(access[0].GetProperty("playerId").GetString(),device);
  Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await host.GetAsync($"/api/lobbies/{id}/controllers?deviceId=wrong")).StatusCode);
  var privateResponse=await host.GetAsync($"/api/lobbies/{id}/teams/team-01?deviceId={device}");Assert.True(privateResponse.Headers.CacheControl?.NoStore);
  await using var connection=new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();options.AccessTokenProvider=()=>Task.FromResult<string?>(token);}).Build();var updates=Channel.CreateUnbounded<JsonElement>();connection.On<JsonElement>("teamSnapshot",state=>updates.Writer.TryWrite(state));await connection.StartAsync();
  var own=await connection.InvokeAsync<JsonElement>("SubscribeHouseholdTeam",id,device,"team-01");await connection.InvokeAsync("UnsubscribeHouseholdTeam",id,"team-01",own.GetProperty("assignmentRevision").GetInt64());await connection.InvokeAsync<JsonElement>("SubscribeHouseholdTeam",id,device,"team-02");
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars=new[]{new{driverId="driver-01",pace="Push",pit="Stay Out",compound=(string?)null,battle="Neutral"},new{driverId="driver-02",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"}}})).EnsureSuccessStatusCode();
  using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));var update=await updates.Reader.ReadAsync(timeout.Token);Assert.Equal("team-02",update.GetProperty("teamId").GetString());Assert.Equal(JsonValueKind.Null,update.GetProperty("confirmedPlan").ValueKind);
 }
}
