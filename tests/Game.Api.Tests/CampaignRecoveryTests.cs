using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Game.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
namespace Game.Api.Tests;

public sealed class CampaignRecoveryTests
{
 private sealed class Clock:TimeProvider {private DateTimeOffset now=DateTimeOffset.Parse("2026-10-25T00:00:00Z");public override DateTimeOffset GetUtcNow()=>now;public void Advance()=>now=now.AddHours(1);}
 [Fact]
 public async Task Active_return_revokes_old_private_connection_then_rebinds_at_boundary_preserving_accepted_lock_and_AI_policy()
 {
  var token=await LobbyTests.Identity(false);var clock=new Clock();var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  await using var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable));s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));}));
  using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var response=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host",Laps:20));response.EnsureSuccessStatusCode();
  var joined=(await response.Content.ReadFromJsonAsync<CampaignJoined>())!;var id=joined.Campaign.CampaignId;var weekend=joined.Campaign.WeekendId;var device=joined.Weekend.Membership.DeviceId;
  (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/claims",new ClaimTeamInput("team-01",device))).EnsureSuccessStatusCode();
  (await client.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).EnsureSuccessStatusCode();
  // Explicit host fallback is durable and does not reroll an already accepted practice.
  var prep=$"/api/lobbies/{weekend}/preparation/teams/team-01?deviceId={device}";
  var ownPrep=(await client.GetFromJsonAsync<PreparationTeamSnapshot>(prep))!;
  (await client.PostAsJsonAsync(prep.Replace("?","/practice?"),new PracticeCommand(Guid.NewGuid().ToString(),ownPrep.Revision,ownPrep.AssignmentRevision))).EnsureSuccessStatusCode();
  ownPrep=(await client.GetFromJsonAsync<PreparationTeamSnapshot>(prep))!;
  using var outsider=server.CreateClient();outsider.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));
  Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync($"/api/campaigns/{id}/absent/prepare",new CampaignAbsentInput("team-01"))).StatusCode);
  var fallback=await client.PostAsJsonAsync($"/api/campaigns/{id}/absent/prepare",new CampaignAbsentInput("team-01"));fallback.EnsureSuccessStatusCode();
  Assert.Equal("host-absent-ai",(await fallback.Content.ReadFromJsonAsync<CampaignSnapshot>())!.Readiness.Single(t=>t.TeamId=="team-01").Source);
  var completedPrep=(await client.GetFromJsonAsync<PreparationTeamSnapshot>(prep))!;Assert.Equal(JsonSerializer.Serialize(ownPrep.Assessment),JsonSerializer.Serialize(completedPrep.Assessment));
  (await client.PostAsJsonAsync($"/api/campaigns/{id}/absent/prepare",new CampaignAbsentInput("team-01"))).EnsureSuccessStatusCode();
  Assert.Equal(completedPrep.Revision,(await client.GetFromJsonAsync<PreparationTeamSnapshot>(prep))!.Revision);
  (await client.PostAsJsonAsync($"/api/campaigns/{id}/start",new CampaignStartInput(Guid.NewGuid().ToString()))).EnsureSuccessStatusCode();
  Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/campaigns/{id}/absent/prepare",new CampaignAbsentInput("team-01"))).StatusCode);
  var sessions=server.Services.GetRequiredService<SessionCoordinator>();
  for(var i=0;i<12;i++){var race=await client.GetAsync($"/api/sessions/{weekend}");if(race.IsSuccessStatusCode){var raceState=(await race.Content.ReadFromJsonAsync<PublicSnapshot>())!;if(raceState.Lifecycle=="awaiting-decisions")break;if(raceState.Lifecycle=="phase-result"){(await client.PostAsJsonAsync($"/api/lobbies/{weekend}/continue",new DueTransitionInput(Guid.NewGuid().ToString(),raceState.Phase,raceState.WindowRevision))).EnsureSuccessStatusCode();break;}}clock.Advance();await sessions.SweepWakeups();}
  var own=(await client.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{weekend}/teams/team-01?deviceId={device}"))!;
  Assert.Equal("Human",own.ControlStatus!.Mode);
  await using var observing=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable));s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));}));
  using var stale=observing.CreateClient();stale.DefaultRequestHeaders.Authorization=new("Bearer",token);
  (await stale.GetAsync($"/api/lobbies/{weekend}/teams/team-01?deviceId={device}")).EnsureSuccessStatusCode();
  await using var hub=new HubConnectionBuilder().WithUrl("http://localhost/hubs/race",options=>{options.Transports=Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;options.HttpMessageHandlerFactory=_=>server.Server.CreateHandler();options.AccessTokenProvider=()=>Task.FromResult<string?>(token);}).Build();
  var updates=new List<TeamSnapshot>();hub.On<TeamSnapshot>("teamSnapshot",snapshot=>updates.Add(snapshot));await hub.StartAsync();await hub.InvokeAsync<TeamSnapshot>("SubscribeHouseholdTeam",weekend,device,"team-01");
  var command=new TeamCommand(Guid.NewGuid().ToString(),own.Phase,own.CommandRevision,own.Cars.Select(c=>new CarCommand(c.DriverId,"Push","Stay Out",null,"Neutral")).ToArray(),own.WindowKind);
  var locked=await client.PostAsJsonAsync($"/api/lobbies/{weekend}/teams/team-01/plans?deviceId={device}",command);Assert.True(locked.IsSuccessStatusCode,await locked.Content.ReadAsStringAsync());
  (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/teams/team-01/control",new ControlRequest(Guid.NewGuid().ToString(),0,"PermanentAi"))).EnsureSuccessStatusCode();
  var returned=await client.PostAsJsonAsync($"/api/campaigns/{id}/return",new{});returned.EnsureSuccessStatusCode();var newDevice=(await returned.Content.ReadFromJsonAsync<JoinedLobby>())!.Membership.DeviceId;
  Assert.Equal(HttpStatusCode.Forbidden,(await stale.GetAsync($"/api/lobbies/{weekend}/teams/team-01?deviceId={device}")).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/lobbies/{weekend}/teams/team-01?deviceId={newDevice}")).StatusCode);
  await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(()=>hub.InvokeAsync<TeamSnapshot>("SubscribeHouseholdTeam",weekend,newDevice,"team-01"));
  var publicState=(await client.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{weekend}"))!;Assert.Contains(publicState.Readiness!,r=>r.TeamId=="team-01"&&r.Locked);
  await using var restarted=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable));s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));}));
  var restartedSessions=restarted.Services.GetRequiredService<SessionCoordinator>();
  using var resumed=restarted.CreateClient();resumed.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var restored=(await resumed.GetFromJsonAsync<PublicSnapshot>($"/api/sessions/{weekend}"))!;
  // Existing presentation recovery deliberately pauses. The authenticated host resumes it;
  // returning membership itself must not advance the race or grant phase control.
  if(restored.Paused)(await resumed.PostAsJsonAsync($"/api/lobbies/{weekend}/resume",new{})).EnsureSuccessStatusCode();

  for(var i=0;i<8;i++){clock.Advance();await restartedSessions.SweepWakeups();var access=await client.GetAsync($"/api/lobbies/{weekend}/teams/team-01?deviceId={newDevice}");if(access.IsSuccessStatusCode)break;}
  var rebound=(await client.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{weekend}/teams/team-01?deviceId={newDevice}"))!;Assert.Equal("PermanentAi",rebound.ControlStatus!.Mode);Assert.True(rebound.Phase>own.Phase);
  Assert.DoesNotContain(updates,s=>s.Phase>own.Phase);await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(()=>hub.InvokeAsync<TeamSnapshot>("SubscribeHouseholdTeam",weekend,device,"team-01"));
  (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/teams/team-01/reclaim?deviceId={newDevice}",new ControlRequest(Guid.NewGuid().ToString(),rebound.ControlStatus.Revision,"Human"))).EnsureSuccessStatusCode();
  var pending=(await client.GetFromJsonAsync<TeamSnapshot>($"/api/lobbies/{weekend}/teams/team-01?deviceId={newDevice}"))!;Assert.Equal("PermanentAi",pending.ControlStatus!.Mode);Assert.Equal("Human",pending.ControlStatus.PendingMode);
  var otherResponse=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Other"));otherResponse.EnsureSuccessStatusCode();var other=(await otherResponse.Content.ReadFromJsonAsync<CampaignJoined>())!;
  Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/campaigns/{other.Campaign.CampaignId}?deviceId={newDevice}")).StatusCode);
  await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(()=>hub.InvokeAsync<TeamSnapshot>("SubscribeHouseholdTeam",other.Campaign.WeekendId,newDevice,"team-01"));
 }
 [Theory][InlineData("model")][InlineData("calendar")][InlineData("missing")][InlineData("malformed")]
 public async Task Unsupported_or_damaged_campaign_fails_actionably_without_overwriting_saved_inputs(string damage)
 {
  var token=await LobbyTests.Identity(false);
  var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  await using var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));
  using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var response=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host"));response.EnsureSuccessStatusCode();
  var joined=(await response.Content.ReadFromJsonAsync<CampaignJoined>())!;var id=joined.Campaign.CampaignId;
  var saved=durable.Read("campaign-"+id)!;var json=JsonNode.Parse(saved.Payload)!;
  if(damage=="model")json["versions"]!["replay"]="future-replay-v99";
  if(damage=="calendar")json["eventIndex"]=999;
  if(damage=="missing")json["frozenPresets"]=null;
  var payload=damage=="malformed"?"{broken":json.ToJsonString();
  durable.Write("campaign-"+id,saved.Version,payload,null);
  var broken=await client.GetAsync($"/api/campaigns/{id}?deviceId={joined.Weekend.Membership.DeviceId}");
  Assert.Equal(HttpStatusCode.Conflict,broken.StatusCode);Assert.Contains("Preserve",await broken.Content.ReadAsStringAsync());
  Assert.Equal(payload,durable.Read("campaign-"+id)!.Payload);
 }
 [Fact]
 public async Task Materialised_campaign_retains_understood_old_source_versions()
 {
  var token=await LobbyTests.Identity(false);var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  await using var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));
  using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var response=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host"));response.EnsureSuccessStatusCode();
  var joined=(await response.Content.ReadFromJsonAsync<CampaignJoined>())!;var id=joined.Campaign.CampaignId;
  var saved=durable.Read("campaign-"+id)!;var json=JsonNode.Parse(saved.Payload)!;json["contentVersion"]="frozen-source-2025";json["calendarVersion"]="reviewed-old-source-calendar";
  durable.Write("campaign-"+id,saved.Version,json.ToJsonString(),null);
  var recovered=await client.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={joined.Weekend.Membership.DeviceId}");
  Assert.Equal("frozen-source-2025",recovered!.ContentVersion);Assert.Equal("reviewed-old-source-calendar",recovered.CalendarVersion);
 }
}
