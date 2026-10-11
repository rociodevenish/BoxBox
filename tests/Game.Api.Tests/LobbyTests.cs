using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
namespace Game.Api.Tests;
public sealed class LobbyTests
{
 [Fact]
 public async Task Verified_account_creates_lobby_and_guest_joins_without_receiving_host_authority()
 {
  var host=await Identity(false);var guest=await Identity(true);
  await using var server=Server();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",host);
  var response=await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Alex"});response.EnsureSuccessStatusCode();
  var created=await response.Content.ReadFromJsonAsync<JsonElement>();var lobby=created.GetProperty("lobby");var id=lobby.GetProperty("sessionId").GetString();
  Assert.Equal(11,lobby.GetProperty("teams").GetArrayLength());Assert.True(created.GetProperty("membership").GetProperty("isHost").GetBoolean());
  client.DefaultRequestHeaders.Authorization=new("Bearer",guest);
  var joined=await client.PostAsJsonAsync("/api/lobbies/join",new{code=lobby.GetProperty("joinCode").GetString(),name="Sam"});joined.EnsureSuccessStatusCode();
  var membership=(await joined.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("membership");Assert.False(membership.GetProperty("isHost").GetBoolean());
  Assert.NotEqual(membership.GetProperty("playerId").GetString(),membership.GetProperty("deviceId").GetString());
  Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).StatusCode);
  var publicJson=await client.GetStringAsync($"/api/lobbies/{id}");Assert.DoesNotContain("controllerCapability",publicJson);Assert.DoesNotContain(host,publicJson);Assert.DoesNotContain("authUid",publicJson);
 }
 [Fact]
 public async Task Simultaneous_claim_has_one_winner_and_current_membership_is_required()
 {
  var host=await Identity(false);var first=await Identity(true);var second=await Identity(true);
  await using var server=Server();using var h=server.CreateClient();h.DefaultRequestHeaders.Authorization=new("Bearer",host);
  var created=await(await h.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Mixed",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();
  var lobby=created.GetProperty("lobby");var id=lobby.GetProperty("sessionId").GetString();var code=lobby.GetProperty("joinCode").GetString();
  using var a=server.CreateClient();a.DefaultRequestHeaders.Authorization=new("Bearer",first);using var b=server.CreateClient();b.DefaultRequestHeaders.Authorization=new("Bearer",second);
  var aj=await(await a.PostAsJsonAsync("/api/lobbies/join",new{code,name="First"})).Content.ReadFromJsonAsync<JsonElement>();var bj=await(await b.PostAsJsonAsync("/api/lobbies/join",new{code,name="Second"})).Content.ReadFromJsonAsync<JsonElement>();
  var claims=await Task.WhenAll(a.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-03",deviceId=aj.GetProperty("membership").GetProperty("deviceId").GetString()}),b.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-03",deviceId=bj.GetProperty("membership").GetProperty("deviceId").GetString()}));
  Assert.Single(claims,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(claims,r=>r.StatusCode==HttpStatusCode.Conflict);
  var state=await h.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}");Assert.Single(state.GetProperty("teams").EnumerateArray(),t=>t.GetProperty("playerId").ValueKind==JsonValueKind.String);
  var before=await h.GetStringAsync($"/api/lobbies/{id}");Assert.Equal(HttpStatusCode.Forbidden,(await a.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-04",deviceId=bj.GetProperty("membership").GetProperty("deviceId").GetString()})).StatusCode);Assert.Equal(before,await h.GetStringAsync($"/api/lobbies/{id}"));
 }
 [Fact]
 public async Task Host_assigns_shared_participant_and_start_freezes_human_and_scripted_teams_without_changing_drivers()
 {
  var token=await Identity(false);await using var server=Server();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="restricted-mixed",laps=6,seed=123,mode="Mixed",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();var member=created.GetProperty("membership");
  (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=member.GetProperty("deviceId").GetString()})).EnsureSuccessStatusCode();
  var participant=await(await client.PostAsJsonAsync($"/api/lobbies/{id}/participants",new{name="Junior",deviceId=member.GetProperty("deviceId").GetString()})).Content.ReadFromJsonAsync<JsonElement>();var child=participant.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()=="Junior").GetProperty("playerId").GetString();
  (await client.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-04",playerId=child,deviceId=member.GetProperty("deviceId").GetString(),control="SharedTablet"})).EnsureSuccessStatusCode();
  var start=await client.PostAsJsonAsync($"/api/lobbies/{id}/start",new{});start.EnsureSuccessStatusCode();var state=await start.Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(new[]{"team-01","team-04"},state.GetProperty("requiredTeams").EnumerateArray().Select(t=>t.GetString()));Assert.Equal(9,state.GetProperty("scriptedTeams").GetArrayLength());
  var race=await client.GetFromJsonAsync<JsonElement>($"/api/sessions/{id}");Assert.Equal(0,race.GetProperty("raceTime").GetDouble());Assert.Equal(22,race.GetProperty("classification").GetArrayLength());Assert.Equal("driver-01",race.GetProperty("classification")[0].GetProperty("driverId").GetString());
  var before=await client.GetStringAsync($"/api/lobbies/{id}");Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId="team-04",playerId=member.GetProperty("playerId").GetString(),deviceId=member.GetProperty("deviceId").GetString(),control="Private"})).StatusCode);Assert.Equal(before,await client.GetStringAsync($"/api/lobbies/{id}"));
  Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/sessions/{id}/team")).StatusCode);
 }
 [Fact]
 public async Task Joined_signalr_subscriber_observes_claims_but_locator_and_other_session_device_grant_no_membership()
 {
  var token=await Identity(false);await using var server=Server();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();var id=created.GetProperty("lobby").GetProperty("sessionId").GetString()!;var device=created.GetProperty("membership").GetProperty("deviceId").GetString()!;
  await using var hub=new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();options.AccessTokenProvider=()=>Task.FromResult<string?>(token);}).Build();
  var updates=System.Threading.Channels.Channel.CreateUnbounded<JsonElement>();hub.On<JsonElement>("lobbySnapshot",snapshot=>updates.Writer.TryWrite(snapshot));await hub.StartAsync();
  await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(()=>hub.InvokeAsync<JsonElement>("SubscribeLobby",id,created.GetProperty("lobby").GetProperty("joinCode").GetString()));
  await hub.InvokeAsync<JsonElement>("SubscribeLobby",id,device);
  (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-02",deviceId=device})).EnsureSuccessStatusCode();using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));var update=await updates.Reader.ReadAsync(timeout.Token);Assert.Equal(created.GetProperty("membership").GetProperty("playerId").GetString(),update.GetProperty("teams")[1].GetProperty("playerId").GetString());
  var other=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();var otherId=other.GetProperty("lobby").GetProperty("sessionId").GetString();Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync($"/api/lobbies/{otherId}/claims",new{teamId="team-03",deviceId=device})).StatusCode);
 }
 [Fact]
 public async Task Only_verified_project_accounts_can_host_or_correct_and_eleven_claims_keep_capacity_coherent()
 {
  var token=await Identity(false);var guest=await Identity(true);await using var server=Server();using var host=server.CreateClient();host.DefaultRequestHeaders.Authorization=new("Bearer",guest);
  var input=new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Host"};Assert.Equal(HttpStatusCode.Forbidden,(await host.PostAsJsonAsync("/api/lobbies",input)).StatusCode);
  host.DefaultRequestHeaders.Authorization=new("Bearer","malformed");Assert.Equal(HttpStatusCode.Forbidden,(await host.PostAsJsonAsync("/api/lobbies",input)).StatusCode);
  var parts=token.Split('.');var payload=System.Text.Json.Nodes.JsonNode.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[1].Replace('-','+').Replace('_','/').PadRight((parts[1].Length+3)/4*4,'='))))!;payload["exp"]=1;
  parts[1]=Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(payload.ToJsonString())).TrimEnd('=').Replace('+','-').Replace('/','_');host.DefaultRequestHeaders.Authorization=new("Bearer",string.Join('.',parts));Assert.Equal(HttpStatusCode.Forbidden,(await host.PostAsJsonAsync("/api/lobbies",input)).StatusCode);

  await using(var wrong=Server("demo-other-project")){using var wrongClient=wrong.CreateClient();wrongClient.DefaultRequestHeaders.Authorization=new("Bearer",token);Assert.Equal(HttpStatusCode.Forbidden,(await wrongClient.PostAsJsonAsync("/api/lobbies",input)).StatusCode);}
  host.DefaultRequestHeaders.Authorization=new("Bearer",token);var created=await(await host.PostAsJsonAsync("/api/lobbies",input)).Content.ReadFromJsonAsync<JsonElement>();var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();var code=created.GetProperty("lobby").GetProperty("joinCode").GetString();
  for(var i=1;i<=11;i++){using var player=server.CreateClient();player.DefaultRequestHeaders.Authorization=new("Bearer",await Identity(true));var joined=await(await player.PostAsJsonAsync("/api/lobbies/join",new{code,name="Player "+i})).Content.ReadFromJsonAsync<JsonElement>();var member=joined.GetProperty("membership");(await player.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId=$"team-{i:00}",deviceId=member.GetProperty("deviceId").GetString()})).EnsureSuccessStatusCode();
   Assert.Equal(HttpStatusCode.Forbidden,(await player.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId=$"team-{i:00}",playerId=member.GetProperty("playerId").GetString(),deviceId=member.GetProperty("deviceId").GetString(),control="Private"})).StatusCode);
   (await host.PostAsJsonAsync($"/api/lobbies/{id}/assignments",new{teamId=$"team-{i:00}",playerId=member.GetProperty("playerId").GetString(),deviceId=(string?)null,control="Private"})).EnsureSuccessStatusCode();
  }
  var snapshot=await host.GetFromJsonAsync<JsonElement>($"/api/lobbies/{id}");Assert.Equal(11,snapshot.GetProperty("teams").EnumerateArray().Count(t=>t.GetProperty("approved").GetBoolean()));
  using(var extra=server.CreateClient()){extra.DefaultRequestHeaders.Authorization=new("Bearer",await Identity(true));var joined=await(await extra.PostAsJsonAsync("/api/lobbies/join",new{code,name="Twelfth"})).Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(HttpStatusCode.Conflict,(await extra.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-01",deviceId=joined.GetProperty("membership").GetProperty("deviceId").GetString()})).StatusCode);}
  var started=await(await host.PostAsJsonAsync($"/api/lobbies/{id}/start",new{})).Content.ReadFromJsonAsync<JsonElement>();Assert.Equal(11,started.GetProperty("requiredTeams").GetArrayLength());Assert.Empty(started.GetProperty("scriptedTeams").EnumerateArray());
 }
 [Fact]
 public async Task Human_can_choose_any_constructor_and_all_other_teams_have_frozen_scripted_inputs()
 {
  await using var server=Server();using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",await Identity(false));var created=await(await client.PostAsJsonAsync("/api/lobbies",new{preset="open-equal",laps=6,seed=123,mode="Competitive",name="Host"})).Content.ReadFromJsonAsync<JsonElement>();var id=created.GetProperty("lobby").GetProperty("sessionId").GetString();
  (await client.PostAsJsonAsync($"/api/lobbies/{id}/claims",new{teamId="team-04",deviceId=created.GetProperty("membership").GetProperty("deviceId").GetString()})).EnsureSuccessStatusCode();var result=await client.PostAsJsonAsync($"/api/lobbies/{id}/start",new{});result.EnsureSuccessStatusCode();var state=await result.Content.ReadFromJsonAsync<JsonElement>();Assert.Contains(state.GetProperty("scriptedTeams").EnumerateArray(),t=>t.GetString()=="team-01");Assert.Equal("team-04",Assert.Single(state.GetProperty("requiredTeams").EnumerateArray()).GetString());
 }
 internal static WebApplicationFactory<Program> Server(string project="demo-f1game")=>new WebApplicationFactory<Program>().WithWebHostBuilder(builder=>builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>{{"Firebase:ProjectId",project}})));
 internal static async Task<string> Identity(bool guest)
 {
  var emulatorHost=Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST")??"127.0.0.1:19099";Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST",emulatorHost);using var auth=new HttpClient();
  var response=await auth.PostAsJsonAsync($"http://{emulatorHost}/identitytoolkit.googleapis.com/v1/accounts:signUp?key=demo-local-key",guest?(object)new{returnSecureToken=true}:new{email=Guid.NewGuid()+"@example.test",password="Local-test-password-123",returnSecureToken=true});response.EnsureSuccessStatusCode();
  return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idToken").GetString()!;
 }
}
