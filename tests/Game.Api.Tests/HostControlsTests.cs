using Microsoft.AspNetCore.SignalR.Client;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
namespace Game.Api.Tests;
public sealed class HostControlsTests
{
 private static string Stable(string json){var node=System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();node.Remove("serverTimeUtc");return node.ToJsonString();}
 [Fact]
 public async Task Pause_preserves_locks_and_resume_opens_once_without_a_display()
 {
  await using var server=LobbyTests.Server();using var host=server.CreateClient();host.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(false));
  var created=await(await host.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
  var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();var device=created.GetProperty("membership").GetProperty("deviceId").GetString();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
  var participants=await(await host.PostAsJsonAsync($"/api/lobbies/{id}/participants",new{name="Second team",deviceId=device})).Content.ReadFromJsonAsync<JsonElement>();var child=participants.GetProperty("players").EnumerateArray().Single(player=>player.GetProperty("name").GetString()=="Second team").GetProperty("playerId").GetString();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-02",playerId=child,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
  var accepted=new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars=new[]{new{driverId="driver-01",pace="Push",pit="Stay Out",compound=(string?)null,battle="Neutral"},new{driverId="driver-02",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"}}};
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",accepted)).EnsureSuccessStatusCode();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/pause",new{})).EnsureSuccessStatusCode();
  var retained=await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/teams/team-01?deviceId={device}");Assert.Equal("Push",retained.GetProperty("confirmedPlan").GetProperty("cars")[0].GetProperty("pace").GetString());
  var paused=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal("paused-decisions",paused.GetProperty("lifecycle").GetString());
  object Plan()=>new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars=new[]{new{driverId="driver-03",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"},new{driverId="driver-04",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"}}};
  Assert.Equal(HttpStatusCode.Conflict,(await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-02/plans?deviceId={device}",Plan())).StatusCode);
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();var before=await host.GetStringAsync($"/api/sessions/{id}");
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})).EnsureSuccessStatusCode();Assert.Equal(Stable(before),Stable(await host.GetStringAsync($"/api/sessions/{id}")));
  var command=Plan();(await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-02/plans?deviceId={device}",command)).EnsureSuccessStatusCode();
  var next=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(1,next.GetProperty("phase").GetInt32());Assert.Equal("awaiting-decisions",next.GetProperty("lifecycle").GetString());
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-02/plans?deviceId={device}",command)).EnsureSuccessStatusCode();Assert.Equal(Stable(next.GetRawText()),Stable((await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}")).GetRawText()));
 }
 [Fact]
 public async Task Revoked_device_loses_all_explicit_grants_and_obsolete_private_streams_but_frozen_teams_still_wait()
 {
  await using var server=LobbyTests.Server();using var host=server.CreateClient();var token=await LobbyTests.Identity(false);host.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await(await host.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Mixed",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();var device=created.GetProperty("membership").GetProperty("deviceId").GetString();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
  var second=await(await host.PostAsJsonAsync("/api/lobbies/join",new{code=created.GetProperty("lobby").GetProperty("joinCode").GetString(),name="Second device"})).Content.ReadFromJsonAsync<JsonElement>();var other=second.GetProperty("membership").GetProperty("deviceId").GetString();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/controllers",new{deviceId=other})).EnsureSuccessStatusCode();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
  await using var hub=new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();options.AccessTokenProvider=()=>Task.FromResult<string?>(token);}).Build();
  var updates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();hub.On<JsonElement>("teamSnapshot",state=>updates.Writer.TryWrite(state));await hub.StartAsync();var initial=await hub.InvokeAsync<JsonElement>("SubscribeHouseholdTeam",id,device,"team-01");
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/devices/{device}/revoke",new{})).EnsureSuccessStatusCode();
  Assert.Equal(HttpStatusCode.Forbidden,(await host.GetAsync($"/api/lobbies/{id}/teams/team-01?deviceId={device}")).StatusCode);
  await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(()=>hub.InvokeAsync<JsonElement>("SubscribeHouseholdTeam",id,device,"team-01"));
  var metadata=await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/controllers?deviceId={device}");Assert.Empty(metadata.EnumerateArray());
  var current=await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}/teams/team-01?deviceId={other}");Assert.True(current.GetProperty("assignmentRevision").GetInt64()>initial.GetProperty("assignmentRevision").GetInt64());
  var plan=new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars=new[]{new{driverId="driver-01",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"},new{driverId="driver-02",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"}}};
  Assert.Equal(HttpStatusCode.Forbidden,(await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",plan)).StatusCode);
  var waiting=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(0,waiting.GetProperty("phase").GetInt32());Assert.Single(waiting.GetProperty("readiness").EnumerateArray());Assert.False(waiting.GetProperty("readiness")[0].GetProperty("locked").GetBoolean());
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={other}",plan)).EnsureSuccessStatusCode();
  using var quiet=new CancellationTokenSource(TimeSpan.FromMilliseconds(500));await Assert.ThrowsAnyAsync<OperationCanceledException>(async()=>await updates.Reader.ReadAsync(quiet.Token));
 } [Fact]
 public async Task Pause_observed_during_committed_resolution_does_not_cancel_outcome_and_concurrent_resume_opens_once()
 {
  await using var server=LobbyTests.Server();using var host=server.CreateClient();var token=await LobbyTests.Identity(false);host.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await(await host.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=50,seed=123,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();var device=created.GetProperty("membership").GetProperty("deviceId").GetString();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();(await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
  using var spectator=server.CreateClient();spectator.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));
  foreach(var action in new[]{"pause","resume","devices/"+device+"/revoke"})Assert.Equal(HttpStatusCode.Forbidden,(await spectator.PostAsJsonAsync($"/api/lobbies/{id}/{action}",new{})).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await spectator.GetAsync($"/api/lobbies/{id}/controller-devices")).StatusCode);
  await using var hub=new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();}).Build();
  var committed=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var outcomes=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();hub.On<JsonElement>("publicSnapshot",state=>{outcomes.Writer.TryWrite(state);if(state.GetProperty("lifecycle").GetString()=="resolving")committed.TrySetResult();});await hub.StartAsync();await hub.InvokeAsync<JsonElement>("SubscribePublic",id);
  var plan=new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars=new[]{new{driverId="driver-01",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"},new{driverId="driver-02",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"}}};
  var submission=host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",plan);await committed.Task.WaitAsync(TimeSpan.FromSeconds(10));
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/pause",new{})).EnsureSuccessStatusCode();var response=await submission;response.EnsureSuccessStatusCode();var ack=await response.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(0,ack.GetProperty("phase").GetInt32());
  var resumes=await Task.WhenAll(Enumerable.Range(0,8).Select(_=>host.PostAsJsonAsync($"/api/lobbies/{id}/resume",new{})));foreach(var resume in resumes)resume.EnsureSuccessStatusCode();
  var next=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(1,next.GetProperty("phase").GetInt32());Assert.Equal("awaiting-decisions",next.GetProperty("lifecycle").GetString());Assert.True(next.GetProperty("raceTime").GetDouble()>0);
  using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));var resolved=new HashSet<string>();while(true){var state=await outcomes.Reader.ReadAsync(deadline.Token);if(state.GetProperty("lifecycle").GetString()=="phase-result")resolved.Add(state.GetProperty("presentation").GetProperty("cursor").GetString()!);if(state.GetProperty("revision").GetInt64()==next.GetProperty("revision").GetInt64())break;}Assert.Single(resolved);
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/plans?deviceId={device}",plan)).EnsureSuccessStatusCode();Assert.Equal(Stable(next.GetRawText()),Stable((await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}")).GetRawText()));
 }}
