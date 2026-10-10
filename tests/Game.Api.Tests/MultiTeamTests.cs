using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc.Testing;
namespace Game.Api.Tests;
public sealed class MultiTeamTests
{
 [Theory]
 [InlineData(false,false)]
 [InlineData(true,false)]
 [InlineData(false,true)]
 public async Task Every_required_team_locks_before_one_resolution_and_private_access_is_scoped(bool reverse,bool concurrent)
 {
  await using var server=LobbyTests.Server();using var host=server.CreateClient();var ht=await LobbyTests.Identity(false);host.DefaultRequestHeaders.Authorization=new("Bearer",ht);
  var created=await(await host.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
  var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();var device=created.GetProperty("membership").GetProperty("deviceId").GetString();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=device})).EnsureSuccessStatusCode();
  using var guest=server.CreateClient();var gt=await LobbyTests.Identity(true);guest.DefaultRequestHeaders.Authorization=new("Bearer",gt);
  var joined=await(await guest.PostAsJsonAsync("/api/lobbies/join",new{code=created.GetProperty("lobby").GetProperty("joinCode").GetString(),name="Guest"})).Content.ReadFromJsonAsync<JsonElement>();
  var gd=joined.GetProperty("membership").GetProperty("deviceId").GetString();var gp=joined.GetProperty("membership").GetProperty("playerId").GetString();
  (await guest.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-02",deviceId=gd})).EnsureSuccessStatusCode();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-02",playerId=gp,deviceId=gd,control="Private"})).EnsureSuccessStatusCode();
  var secondHost=await(await host.PostAsJsonAsync("/api/lobbies/join",new{code=created.GetProperty("lobby").GetProperty("joinCode").GetString(),name="Host again"})).Content.ReadFromJsonAsync<JsonElement>();var secondDevice=secondHost.GetProperty("membership").GetProperty("deviceId").GetString();
  Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await guest.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/controllers",new{deviceId=gd})).StatusCode);
  Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/controllers",new{deviceId=gd})).StatusCode);
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/teams/team-01/controllers",new{deviceId=secondDevice})).EnsureSuccessStatusCode();
  var ungranted=await(await host.PostAsJsonAsync("/api/lobbies/join",new{code=created.GetProperty("lobby").GetProperty("joinCode").GetString(),name="Host third"})).Content.ReadFromJsonAsync<JsonElement>();
  (await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).EnsureSuccessStatusCode();
  await using var display=Connection(server,null);await using var privateHost=Connection(server,ht);await using var privateGuest=Connection(server,gt);
  var publicUpdates=Channel.CreateUnbounded<JsonElement>();var hostUpdates=Channel.CreateUnbounded<JsonElement>();var guestUpdates=Channel.CreateUnbounded<JsonElement>();
  display.On<JsonElement>("publicSnapshot",state=>publicUpdates.Writer.TryWrite(state));privateHost.On<JsonElement>("teamSnapshot",state=>hostUpdates.Writer.TryWrite(state));privateGuest.On<JsonElement>("teamSnapshot",state=>guestUpdates.Writer.TryWrite(state));
  await display.StartAsync();await privateHost.StartAsync();await privateGuest.StartAsync();
  await display.InvokeAsync<JsonElement>("SubscribePublic",id);
  await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(()=>display.InvokeAsync<JsonElement>("SubscribeHouseholdTeam",id,device,"team-01"));
  await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(()=>privateGuest.InvokeAsync<JsonElement>("SubscribeHouseholdTeam",id,gd,"team-01"));
  var hostInitial=await privateHost.InvokeAsync<JsonElement>("SubscribeHouseholdTeam",id,secondDevice,"team-01");await privateGuest.InvokeAsync<JsonElement>("SubscribeHouseholdTeam",id,gd,"team-02");
  Assert.Equal(new[]{"driver-01","driver-02"},hostInitial.GetProperty("cars").EnumerateArray().Select(car=>car.GetProperty("driverId").GetString()));
  Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await host.GetAsync($"/api/lobbies/{id}/teams/team-01?deviceId={ungranted.GetProperty("membership").GetProperty("deviceId").GetString()}")).StatusCode);
  var own=await host.GetAsync($"/api/lobbies/{id}/teams/team-01?deviceId={device}");own.EnsureSuccessStatusCode();
  Assert.Equal(System.Net.HttpStatusCode.Forbidden,(await guest.GetAsync($"/api/lobbies/{id}/teams/team-01?deviceId={gd}")).StatusCode);
  object Plan(int a,int b)=>new{commandId=Guid.NewGuid().ToString(),phase=0,revision=1,cars=new[]{new{driverId=$"driver-{a:00}",pace="Push",pit="Stay Out",compound=(string?)null,battle="Neutral"},new{driverId=$"driver-{b:00}",pace="Standard",pit="Stay Out",compound=(string?)null,battle="Neutral"}}};
  var firstClient=reverse?guest:host;var lastClient=reverse?host:guest;var ft=reverse?"team-02":"team-01";var lt=reverse?"team-01":"team-02";var fd=reverse?gd:device;var ld=reverse?device:gd;
  var first=reverse?Plan(3,4):Plan(1,2);
  var before=await host.GetStringAsync($"/api/sessions/{id}");
  var malformed=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(first))!;malformed["cars"]![1]!["pace"]="Boost";
  Assert.Equal(System.Net.HttpStatusCode.BadRequest,(await firstClient.PostAsJsonAsync($"/api/lobbies/{id}/teams/{ft}/plans?deviceId={fd}",malformed)).StatusCode);Assert.Equal(before,await host.GetStringAsync($"/api/sessions/{id}"));
  malformed["cars"]![1]!["pace"]="Standard";malformed["phase"]=5;
  Assert.Equal(System.Net.HttpStatusCode.Conflict,(await firstClient.PostAsJsonAsync($"/api/lobbies/{id}/teams/{ft}/plans?deviceId={fd}",malformed)).StatusCode);Assert.Equal(before,await host.GetStringAsync($"/api/sessions/{id}"));
  malformed["phase"]=0;malformed["revision"]=999;Assert.Equal(System.Net.HttpStatusCode.Conflict,(await firstClient.PostAsJsonAsync($"/api/lobbies/{id}/teams/{ft}/plans?deviceId={fd}",malformed)).StatusCode);Assert.Equal(before,await host.GetStringAsync($"/api/sessions/{id}"));
  var last=reverse?Plan(1,2):Plan(3,4);
  if(concurrent){var results=await Task.WhenAll(firstClient.PostAsJsonAsync($"/api/lobbies/{id}/teams/{ft}/plans?deviceId={fd}",first),lastClient.PostAsJsonAsync($"/api/lobbies/{id}/teams/{lt}/plans?deviceId={ld}",last));foreach(var result in results)result.EnsureSuccessStatusCode();var final=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(1,final.GetProperty("phase").GetInt32());(await firstClient.PostAsJsonAsync($"/api/lobbies/{id}/teams/{ft}/plans?deviceId={fd}",first)).EnsureSuccessStatusCode();Assert.Equal(final.GetRawText(),(await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}")).GetRawText());await AssertOneOutcome(publicUpdates);return;}
  (await firstClient.PostAsJsonAsync($"/api/lobbies/{id}/teams/{ft}/plans?deviceId={fd}",first)).EnsureSuccessStatusCode();
  var waiting=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(0,waiting.GetProperty("raceTime").GetDouble());Assert.Equal(0,waiting.GetProperty("phase").GetInt32());Assert.DoesNotContain("pace",waiting.GetRawText());
  using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));var publicLock=await publicUpdates.Reader.ReadAsync(timeout.Token);Assert.DoesNotContain("confirmedPlan",publicLock.GetRawText());Assert.DoesNotContain("estimatedTyreCondition",publicLock.GetRawText());
  var ownLock=await (reverse?guestUpdates:hostUpdates).Reader.ReadAsync(timeout.Token);var otherLock=await (reverse?hostUpdates:guestUpdates).Reader.ReadAsync(timeout.Token);Assert.Equal(firstClient==host?"team-01":"team-02",ownLock.GetProperty("teamId").GetString());Assert.Equal(JsonValueKind.Object,ownLock.GetProperty("confirmedPlan").ValueKind);Assert.Equal(JsonValueKind.Null,otherLock.GetProperty("confirmedPlan").ValueKind);
  Assert.Equal(System.Net.HttpStatusCode.Conflict,(await firstClient.PostAsJsonAsync($"/api/lobbies/{id}/teams/{ft}/plans?deviceId={fd}",reverse?Plan(3,4):Plan(1,2))).StatusCode);
  var changed=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(first))!;changed["cars"]![0]!["pace"]="Conserve";Assert.Equal(System.Net.HttpStatusCode.Conflict,(await firstClient.PostAsJsonAsync($"/api/lobbies/{id}/teams/{ft}/plans?deviceId={fd}",changed)).StatusCode);
  (await firstClient.PostAsJsonAsync($"/api/lobbies/{id}/teams/{ft}/plans?deviceId={fd}",first)).EnsureSuccessStatusCode();
  (await lastClient.PostAsJsonAsync($"/api/lobbies/{id}/teams/{lt}/plans?deviceId={ld}",last)).EnsureSuccessStatusCode();
  var next=await host.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(1,next.GetProperty("phase").GetInt32());Assert.True(next.GetProperty("raceTime").GetDouble()>0);await AssertOneOutcome(publicUpdates);
 }
 private static async Task AssertOneOutcome(Channel<JsonElement> updates)
 {using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));var resolving=0;var results=0;while(true){var update=await updates.Reader.ReadAsync(timeout.Token);var life=update.GetProperty("lifecycle").GetString();if(life=="resolving")resolving++;if(life=="phase-result")results++;if(update.GetProperty("phase").GetInt32()==1&&life=="awaiting-decisions")break;}Assert.Equal(1,resolving);Assert.Equal(1,results);}
 private static HubConnection Connection(WebApplicationFactory<Program> server,string? token)=>new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();options.AccessTokenProvider=()=>Task.FromResult(token);}).Build();
}
