using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using System.Threading.Channels;
using Game.Domain;
using Game.Simulation;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace Game.Api.Tests;
public sealed class SessionTests
{
 [Fact]
 public async Task Creates_allowlisted_rolling_field_with_separate_team_grant()
 {
  await using var server = new WebApplicationFactory<Program>();
  using var client = server.CreateClient();
  var response = await client.PostAsJsonAsync("/api/sessions", new {preset="open-equal",laps=6,seed=123});
  response.EnsureSuccessStatusCode();
  var created = await response.Content.ReadFromJsonAsync<JsonElement>();
  Assert.NotEqual(created.GetProperty("sessionId").GetString(),created.GetProperty("controllerCapability").GetString());
  var snapshot=await client.GetFromJsonAsync<JsonElement>("/api/sessions/"+created.GetProperty("sessionId").GetString());
  Assert.Equal("awaiting-decisions",snapshot.GetProperty("lifecycle").GetString());
  Assert.Equal(22,snapshot.GetProperty("classification").GetArrayLength());
  Assert.Equal(11,snapshot.GetProperty("classification").EnumerateArray().Select(row=>row.GetProperty("teamId").GetString()).Distinct().Count());
  Assert.Equal(.5,snapshot.GetProperty("classification")[1].GetProperty("gapToLeader").GetProperty("seconds").GetDouble());
 }
 [Fact]
 public async Task Public_session_identity_does_not_grant_team_access()
 {
  await using var server = new WebApplicationFactory<Program>();
  using var client = server.CreateClient();
  var response = await client.PostAsJsonAsync("/api/sessions", new {preset="restricted-mixed",laps=6,seed=73});
  var created = await response.Content.ReadFromJsonAsync<JsonElement>();
  var id=created.GetProperty("sessionId").GetString();
  Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await client.GetAsync($"/api/sessions/{id}/team")).StatusCode);
  client.DefaultRequestHeaders.Authorization=new("Bearer",created.GetProperty("controllerCapability").GetString());
  var own=await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}/team");
  Assert.Equal("team-01",own.GetProperty("teamId").GetString());
  Assert.Equal(new[]{"driver-01","driver-02"},own.GetProperty("cars").EnumerateArray().Select(car=>car.GetProperty("driverId").GetString()));
  foreach(var car in own.GetProperty("cars").EnumerateArray())
   Assert.Equal(new[]{"driverId","compound","tyreAge","estimatedTyreCondition","inPits","stopQueued","battleOptions"},car.EnumerateObject().Select(field=>field.Name));
  var publicJson=await client.GetStringAsync($"/api/sessions/{id}");
  Assert.DoesNotContain("estimatedTyreCondition",publicJson);
  Assert.DoesNotContain(created.GetProperty("controllerCapability").GetString()!,publicJson);
 } [Fact]
 public async Task Complete_team_plan_resolves_one_engine_phase_with_ordered_public_and_private_notifications()
 {
  await using var server = new WebApplicationFactory<Program>(); using var client=server.CreateClient();
  var created=await (await client.PostAsJsonAsync("/api/sessions",new{preset="open-equal",laps=6,seed=123})).Content.ReadFromJsonAsync<JsonElement>();
  var id=created.GetProperty("sessionId").GetString()!;var capability=created.GetProperty("controllerCapability").GetString()!;
  await using var display=Connection(server);await using var team=Connection(server);
  var publicUpdates=Channel.CreateUnbounded<JsonElement>();var privateUpdates=Channel.CreateUnbounded<JsonElement>();
  display.On<JsonElement>("publicSnapshot",snapshot=>publicUpdates.Writer.TryWrite(snapshot));
  team.On<JsonElement>("teamSnapshot",snapshot=>privateUpdates.Writer.TryWrite(snapshot));
  await display.StartAsync();await team.StartAsync();
  await display.InvokeAsync<JsonElement>("SubscribePublic",id);
  await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(()=>display.InvokeAsync<JsonElement>("SubscribeTeam",id,id));
  var initial=await team.InvokeAsync<JsonElement>("SubscribeTeam",id,capability);
  Assert.Equal(2,initial.GetProperty("cars").GetArrayLength());
  client.DefaultRequestHeaders.Authorization=new("Bearer",capability);
  var command=new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars=new[]{new{driverId="driver-01",pace="Push",pit="Stay Out",compound=(string?)null,battle="Neutral"},new{driverId="driver-02",pace="Conserve",pit="Pit",compound=(string?)"Hard",battle="Neutral"}}};
  var submitted=await client.PostAsJsonAsync($"/api/sessions/{id}/plans",command);submitted.EnsureSuccessStatusCode();
  using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));
  var resolving=await publicUpdates.Reader.ReadAsync(timeout.Token);var result=await publicUpdates.Reader.ReadAsync(timeout.Token);
  Assert.Equal("resolving",resolving.GetProperty("lifecycle").GetString());Assert.Equal("phase-result",result.GetProperty("lifecycle").GetString());
  Assert.True(result.GetProperty("revision").GetInt64()>resolving.GetProperty("revision").GetInt64());
  Assert.Equal("resolving",(await privateUpdates.Reader.ReadAsync(timeout.Token)).GetProperty("lifecycle").GetString());
  var ownResult=await privateUpdates.Reader.ReadAsync(timeout.Token);Assert.Equal("Hard",ownResult.GetProperty("cars")[1].GetProperty("compound").GetString());
  var expected=new RaceEngine(new RaceConfiguration(6,LaboratoryFixtures.SegmentedTrack(false),LapVariation:.2),LaboratoryFixtures.SegmentedField(false),123).ResolvePhase(new Dictionary<string,DriverDecision>{{"driver-01",new(PaceInstruction.Push)},{"driver-02",new(PaceInstruction.Conserve,TyreCompound.Hard)}});
  Assert.Equal(expected.Time,result.GetProperty("raceTime").GetDouble());Assert.Equal(expected.Classification.Select(row=>row.DriverId),result.GetProperty("classification").EnumerateArray().Select(row=>row.GetProperty("driverId").GetString()));
  foreach(var car in result.GetProperty("classification").EnumerateArray()) Assert.Equal(new[]{"driverId","name","teamId","position","completedLaps","progress","gapToLeader","gapToAhead","compound","tyreAge","pitState","eligibleAtFinish"},car.EnumerateObject().Select(field=>field.Name));
  Assert.NotEmpty(result.GetProperty("events").EnumerateArray());
  foreach(var item in result.GetProperty("events").EnumerateArray()) Assert.Equal(new[]{"id","kind","time","lap","driverId","otherDriverId","compound"},item.EnumerateObject().Select(field=>field.Name));
 }
 private static HubConnection Connection(WebApplicationFactory<Program> server) => new HubConnectionBuilder().WithUrl(new Uri(server.Server.BaseAddress,"/hubs/race"),options=>{options.Transports=HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();}).Build(); [Fact]
 public async Task Malformed_second_car_and_unauthorised_commands_leave_the_complete_phase_editable()
 {
  await using var server=new WebApplicationFactory<Program>();using var client=server.CreateClient();
  var created=await (await client.PostAsJsonAsync("/api/sessions",new{preset="open-equal",laps=6,seed=123})).Content.ReadFromJsonAsync<JsonElement>();
  var id=created.GetProperty("sessionId").GetString();
  var command=new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars=new[]{new{driverId="driver-01",pace="Push",pit="Stay Out",compound=(string?)null,battle="Neutral"},new{driverId="driver-02",pace="Boost",pit="Stay Out",compound=(string?)null,battle="Neutral"}}};
  Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await client.PostAsJsonAsync($"/api/sessions/{id}/plans",command)).StatusCode);
  client.DefaultRequestHeaders.Authorization=new("Bearer",created.GetProperty("controllerCapability").GetString());
  Assert.Equal(System.Net.HttpStatusCode.BadRequest,(await client.PostAsJsonAsync($"/api/sessions/{id}/plans",command)).StatusCode);
  var snapshot=await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(1,snapshot.GetProperty("revision").GetInt64());Assert.Equal(0,snapshot.GetProperty("raceTime").GetDouble());Assert.Equal("awaiting-decisions",snapshot.GetProperty("lifecycle").GetString());
 }}
