using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class CampaignDiscoveryTests
{
 private static string RecoveryHost=>Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST")??"127.0.0.1:19100";
 [Theory][InlineData(false)][InlineData(true)]
 public async Task Accounts_discover_unscheduled_hosted_and_joined_campaigns_without_private_details(bool durable)
 {
  ISessionStore store=durable?new FirestoreSessionStore("demo-f1game",RecoveryHost):new VolatileSessionStore();
  await using var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton(store))));
  using var host=server.CreateClient();using var member=server.CreateClient();using var outsider=server.CreateClient();
  host.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(false));member.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(false));outsider.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(false));
  Assert.Equal(HttpStatusCode.Forbidden,(await server.CreateClient().GetAsync("/api/campaigns")).StatusCode);
  var created=await host.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Alex"));created.EnsureSuccessStatusCode();
  var campaign=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;
  Assert.Empty(await outsider.GetFromJsonAsync<JsonElement[]>("/api/campaigns")??[]);
  var listing=await host.GetAsync("/api/campaigns");listing.EnsureSuccessStatusCode();Assert.Contains("no-store",listing.Headers.CacheControl!.ToString());
  var summary=Assert.Single((await listing.Content.ReadFromJsonAsync<JsonElement[]>())!);Assert.Equal(campaign.Campaign.CampaignId,summary.GetProperty("campaignId").GetString());Assert.Equal("host",summary.GetProperty("role").GetString());
  Assert.Equal(new[]{"campaignId","eventNumber","hostName","lifecycle","role","trackName"},summary.EnumerateObject().Select(p=>p.Name).Order().ToArray());
  (await member.PostAsJsonAsync("/api/lobbies/join",new{code=campaign.Weekend.Lobby.JoinCode,name="Sam"})).EnsureSuccessStatusCode();
  var joined=Assert.Single((await member.GetFromJsonAsync<JsonElement[]>("/api/campaigns"))!);Assert.Equal("member",joined.GetProperty("role").GetString());
  (await member.PostAsJsonAsync($"/api/campaigns/{campaign.Campaign.CampaignId}/return",new{})).EnsureSuccessStatusCode();
  var authoritative=store.Read(campaign.Campaign.WeekendId)!;var corrected=System.Text.Json.Nodes.JsonNode.Parse(authoritative.Payload)!;var players=corrected["lobby"]!["players"]!.AsArray();players.Remove(players.Single(player=>player!["name"]!.GetValue<string>()=="Sam"));store.Write(campaign.Campaign.WeekendId,authoritative.Version,corrected.ToJsonString(),campaign.Weekend.Lobby.JoinCode);
  Assert.Empty((await member.GetFromJsonAsync<JsonElement[]>("/api/campaigns"))!);Assert.Single((await host.GetFromJsonAsync<JsonElement[]>("/api/campaigns"))!);Assert.Equal(HttpStatusCode.Forbidden,(await member.PostAsJsonAsync($"/api/campaigns/{campaign.Campaign.CampaignId}/return",new{})).StatusCode);
 }
 [Fact]
 public void Indexed_records_above_the_audit_page_size_survive_restart_without_rewrites()
 {
  using var durable=new FirestoreSessionStore("demo-f1game",RecoveryHost);
  var ids=Enumerable.Range(0,4).Select(_=>"audit-"+Guid.NewGuid().ToString("N")).ToArray();
  foreach(var id in ids)durable.Write(id,null,"{}",null);
  var versions=ids.Select(id=>durable.Read(id)!.Version).ToArray();
  durable.PrepareMembershipIndex(1);
  using var restarted=new FirestoreSessionStore("demo-f1game",RecoveryHost);restarted.PrepareMembershipIndex(1);
  Assert.Equal(versions,ids.Select(id=>restarted.Read(id)!.Version));
 }
 private sealed class Clock:TimeProvider {private DateTimeOffset now=DateTimeOffset.Parse("2026-10-25T00:00:00Z");public override DateTimeOffset GetUtcNow()=>now;public void Advance()=>now=now.AddHours(1);}
 [Fact]
 public async Task Completed_campaign_survives_legacy_metadata_migration_and_restart_while_stale_index_cannot_restore_membership()
 {
  using var durable=new FirestoreSessionStore("demo-f1game",RecoveryHost);var scoped=new CampaignTestSessionScope(durable);var clock=new Clock();
  var token=await LobbyTests.Identity(false);var otherToken=await LobbyTests.Identity(false);var participantToken=await LobbyTests.Identity(false);string id,first;string[] payloads;string[] recordIds;
  await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<ISessionStore>(scoped));s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));})))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var creation=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Legacy host"));creation.EnsureSuccessStatusCode();var joined=(await creation.Content.ReadFromJsonAsync<CampaignJoined>())!;
   id=joined.Campaign.CampaignId;first=joined.Campaign.WeekendId;var device=joined.Weekend.Membership.DeviceId;
   using var initialParticipant=server.CreateClient();initialParticipant.DefaultRequestHeaders.Authorization=new("Bearer",participantToken);(await initialParticipant.PostAsJsonAsync("/api/lobbies/join",new{code=joined.Weekend.Lobby.JoinCode,name="Sam"})).EnsureSuccessStatusCode();
   (await client.PostAsJsonAsync($"/api/lobbies/{first}/claims",new ClaimTeamInput("team-01",device))).EnsureSuccessStatusCode();
   var sessions=server.Services.GetRequiredService<SessionCoordinator>();
   for(var eventIndex=0;eventIndex<2;eventIndex++)
   {
    var preparation=await client.PostAsJsonAsync($"/api/campaigns/{id}/"+(eventIndex==0?"prepare":"next/prepare"),new{});Assert.True(preparation.IsSuccessStatusCode,await preparation.Content.ReadAsStringAsync());
    if(eventIndex==1){var next=(await preparation.Content.ReadFromJsonAsync<CampaignSnapshot>())!;(await client.PostAsJsonAsync($"/api/campaigns/{id}/development/close",new CloseDevelopmentInput(Guid.NewGuid().ToString(),next.Development!.OpportunityId,next.Development.Revision,true))).EnsureSuccessStatusCode();var returned=await client.PostAsJsonAsync($"/api/campaigns/{id}/return",new{nextEvent=true});returned.EnsureSuccessStatusCode();joined=joined with{Weekend=(await returned.Content.ReadFromJsonAsync<JoinedLobby>())!};device=joined.Weekend.Membership.DeviceId;}
    var weekend=joined.Weekend.Lobby.SessionId;
    (await client.PostAsJsonAsync($"/api/campaigns/{id}/absent/prepare",new CampaignAbsentInput("team-01",eventIndex==1))).EnsureSuccessStatusCode();
    (await client.PostAsJsonAsync($"/api/campaigns/{id}/"+(eventIndex==0?"start":"next/start"),new CampaignStartInput(Guid.NewGuid().ToString()))).EnsureSuccessStatusCode();
    for(var iteration=0;iteration<80&&sessions.CommittedCampaignResult(weekend) is null;iteration++){clock.Advance();await sessions.SweepWakeups();}
    Assert.NotNull(sessions.CommittedCampaignResult(weekend));
    var commit=await client.PostAsJsonAsync($"/api/campaigns/{id}/commit-result?deviceId={device}",new{});commit.EnsureSuccessStatusCode();
   }
   var summary=Assert.Single((await client.GetFromJsonAsync<JsonElement[]>("/api/campaigns"))!);Assert.Equal("season-complete",summary.GetProperty("lifecycle").GetString());
   recordIds=new[]{"campaign-"+id,first,joined.Weekend.Lobby.SessionId};payloads=recordIds.Select(record=>durable.Read(record)!.Payload).ToArray();
  }
  // Arrange a real pre-index deployment: remove only newly introduced manifest metadata.
  using var firestore=new HttpClient();firestore.DefaultRequestHeaders.Authorization=new("Bearer","owner");
  var documents=$"http://{RecoveryHost}/v1/projects/demo-f1game/databases/(default)/documents";
  foreach(var record in recordIds){var response=await firestore.PatchAsJsonAsync(documents+"/sessions/"+record+"?updateMask.fieldPaths=memberUids",new{fields=new{}});response.EnsureSuccessStatusCode();}
  durable.PrepareMembershipIndex(1);Assert.Equal(payloads,recordIds.Select(record=>durable.Read(record)!.Payload));
  var indexedVersions=recordIds.Select(record=>durable.Read(record)!.Version).ToArray();durable.PrepareMembershipIndex(1);Assert.Equal(indexedVersions,recordIds.Select(record=>durable.Read(record)!.Version));
  durable.PrepareMembershipIndex();Assert.Equal(payloads,recordIds.Select(record=>durable.Read(record)!.Payload));durable.PrepareMembershipIndex();
  await using var restarted=LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(scoped))));
  using var host=restarted.CreateClient();host.DefaultRequestHeaders.Authorization=new("Bearer",token);
  Assert.Equal("season-complete",Assert.Single((await host.GetFromJsonAsync<JsonElement[]>("/api/campaigns"))!).GetProperty("lifecycle").GetString());
  var recovered=await host.PostAsJsonAsync($"/api/campaigns/{id}/return",new{});recovered.EnsureSuccessStatusCode();var member=(await recovered.Content.ReadFromJsonAsync<JoinedLobby>())!;
  var results=(await host.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={member.Membership.DeviceId}"))!;Assert.Equal(2,results.Results!.Count);
  using var participant=restarted.CreateClient();participant.DefaultRequestHeaders.Authorization=new("Bearer",participantToken);Assert.Single((await participant.GetFromJsonAsync<JsonElement[]>("/api/campaigns"))!);
  var current=recordIds[2];var authoritative=durable.Read(current)!;var edited=System.Text.Json.Nodes.JsonNode.Parse(authoritative.Payload)!;var players=edited["lobby"]!["players"]!.AsArray();players.Remove(players.Single(player=>player!["name"]!.GetValue<string>()=="Sam"));
  durable.Write(current,authoritative.Version,edited.ToJsonString(),edited["lobby"]!["code"]!.GetValue<string>());
  Assert.Empty((await participant.GetFromJsonAsync<JsonElement[]>("/api/campaigns"))!);Assert.Equal(HttpStatusCode.Forbidden,(await participant.PostAsJsonAsync($"/api/campaigns/{id}/return",new{})).StatusCode);
  // A stale/corrupt lookup candidate must never substitute for authoritative lobby membership.
  var authPayload=JsonDocument.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(otherToken.Split('.')[1].Replace('-','+').Replace('_','/').PadRight((otherToken.Split('.')[1].Length+3)/4*4,'='))));var uid=authPayload.RootElement.GetProperty("sub").GetString();
  var stale=await firestore.PatchAsJsonAsync(documents+"/sessions/"+first+"?updateMask.fieldPaths=memberUids",new{fields=new{memberUids=new{arrayValue=new{values=new[]{new{stringValue=uid}}}}}});stale.EnsureSuccessStatusCode();
  using var outsider=restarted.CreateClient();outsider.DefaultRequestHeaders.Authorization=new("Bearer",otherToken);Assert.Empty((await outsider.GetFromJsonAsync<JsonElement[]>("/api/campaigns"))!);Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync($"/api/campaigns/{id}/return",new{})).StatusCode);
 }

}
