using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class PersonnelAcquisitionTests
{
 [Fact]
 public async Task Independent_coordinators_cannot_sell_the_same_offer_twice_and_private_reads_refresh()
 {
  var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);var token=await LobbyTests.Identity(false);
  await using var first=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));
  await using var second=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));
  using var a=first.CreateClient();using var b=second.CreateClient();a.DefaultRequestHeaders.Authorization=new("Bearer",token);b.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var response=await a.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Concurrent host",personnelMode="acquisition",seed=1});response.EnsureSuccessStatusCode();
  var joined=(await response.Content.ReadFromJsonAsync<CampaignJoined>())!;var team=joined.Campaign.Acquisition!.ActiveTeamId!;var device=joined.Weekend.Membership.DeviceId;
  (await a.PostAsJsonAsync($"/api/lobbies/{joined.Campaign.WeekendId}/claims",new ClaimTeamInput(team,device))).EnsureSuccessStatusCode();
  var path=$"/api/campaigns/{joined.Campaign.CampaignId}/acquisition/teams/{team}?deviceId={device}";var own=(await a.GetFromJsonAsync<AcquisitionTeamSnapshot>(path))!;
  Assert.Equal(own.Revision,(await b.GetFromJsonAsync<AcquisitionTeamSnapshot>(path))!.Revision);
  var person=own.Advice!.PersonId;var price=own.Offers.Single(p=>p.Id==person).Price;
  var results=await Task.WhenAll(a.PostAsJsonAsync(path.Replace("?","/hire?"),new Game.Domain.PersonnelHire(Guid.NewGuid().ToString(),own.Revision,team,person)),b.PostAsJsonAsync(path.Replace("?","/hire?"),new Game.Domain.PersonnelHire(Guid.NewGuid().ToString(),own.Revision,team,person)));
  Assert.Single(results,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(results,r=>r.StatusCode==HttpStatusCode.Conflict);
  foreach(var client in new[]{a,b}){var after=(await client.GetFromJsonAsync<AcquisitionTeamSnapshot>(path))!;Assert.Single(after.Hires);Assert.Equal(100-price,after.Balance);Assert.Equal(2,after.Ledger.Count);}
 }
 [Theory]
 [InlineData(false,0)]
 [InlineData(true,0)]
 [InlineData(false,1)]
 [InlineData(true,1)]
 [InlineData(false,2)]
 [InlineData(true,2)]
 public async Task Legal_completion_reserves_and_final_roster_survive_a_failed_final_write_and_restart(bool committed,int interruptedStage)
 {
  var token=await LobbyTests.Identity(false);
  var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  var interrupted=new InterruptedStore(durable,committed);
  string id,device,path="";object finalCommand=new{};string? poolVersion=null;var rejections=0;
  await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted)))))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var created=await client.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Completion host",personnelMode="acquisition",seed=1});created.EnsureSuccessStatusCode();
   var joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;id=joined.Campaign.CampaignId;device=joined.Weekend.Membership.DeviceId;var weekend=joined.Campaign.WeekendId;
   for(var team=1;team<=11;team++)
   {
    var name="Completion player "+team;
    var participants=await client.PostAsJsonAsync($"/api/lobbies/{weekend}/participants",new{name,deviceId=device});participants.EnsureSuccessStatusCode();
    var lobby=await participants.Content.ReadFromJsonAsync<JsonElement>();
    var player=lobby.GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("name").GetString()==name).GetProperty("playerId").GetString();
    (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/assignments",new{teamId=$"team-{team:00}",playerId=player,deviceId=device,control="SharedTablet"})).EnsureSuccessStatusCode();
   }
   for(var pick=0;pick<77;pick++)
   {
    var campaign=await client.GetFromJsonAsync<JsonElement>($"/api/campaigns/{id}?deviceId={device}");
    var team=campaign.GetProperty("acquisition").GetProperty("activeTeamId").GetString()!;
    path=$"/api/campaigns/{id}/acquisition/teams/{team}?deviceId={device}";
    var own=await client.GetFromJsonAsync<JsonElement>(path);
    if(own.GetProperty("scoutUnavailable").ValueKind==JsonValueKind.Null){var scouted=await client.PostAsJsonAsync(path.Replace("?","/scout?"),new{commandId=Guid.NewGuid().ToString(),revision=own.GetProperty("revision").GetInt64(),teamId=team});scouted.EnsureSuccessStatusCode();own=await scouted.Content.ReadFromJsonAsync<JsonElement>();}
    if(pick==0){Assert.Equal(96,own.GetProperty("completionReserve").GetInt32());Assert.Equal(7,own.GetProperty("remainingSlots").GetArrayLength());}
    Assert.False(own.GetProperty("rosterReady").GetBoolean());
    poolVersion??=own.GetProperty("poolVersion").GetString();Assert.Equal(poolVersion,own.GetProperty("poolVersion").GetString());
    var offers=own.GetProperty("offers").EnumerateArray().ToArray();
    var choices=own.GetProperty("hireOptions").EnumerateArray().ToArray();
    foreach(var illegal in choices.Where(c=>!c.GetProperty("canHire").GetBoolean()).Take(1))
    {
     var reject=await client.PostAsJsonAsync(path.Replace("?","/hire?"),new{commandId=Guid.NewGuid().ToString(),revision=own.GetProperty("revision").GetInt64(),teamId=team,personId=illegal.GetProperty("personId").GetString()});
     Assert.Equal(HttpStatusCode.Conflict,reject.StatusCode);Assert.Contains("reserve",await reject.Content.ReadAsStringAsync());
     Assert.Equal(own.ToString(),(await client.GetFromJsonAsync<JsonElement>(path)).ToString());rejections++;
    }
    var person=choices.Where(c=>c.GetProperty("canHire").GetBoolean()).Select(c=>c.GetProperty("personId").GetString()).OrderByDescending(personId=>offers.Single(p=>p.GetProperty("id").GetString()==personId).GetProperty("price").GetInt32()).First();
    var command=new{commandId=Guid.NewGuid().ToString(),revision=own.GetProperty("revision").GetInt64(),teamId=team,personId=person};
    if(pick==76){finalCommand=command;interrupted.WritesBeforeFailure=interruptedStage;interrupted.IncludeLobby=true;interrupted.Armed=true;Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(path.Replace("?","/hire?"),command)).StatusCode);}
    else (await client.PostAsJsonAsync(path.Replace("?","/hire?"),command)).EnsureSuccessStatusCode();
   }
  }
  Assert.True(rejections>0);
  await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));
  using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var completed=await retry.PostAsJsonAsync(path.Replace("?","/hire?"),finalCommand);completed.EnsureSuccessStatusCode();
  var after=await completed.Content.ReadFromJsonAsync<JsonElement>();
  Assert.True(after.GetProperty("rosterReady").GetBoolean());Assert.Equal(18,after.GetProperty("completionReserve").GetInt32());Assert.Empty(after.GetProperty("remainingSlots").EnumerateArray());Assert.Equal(7,after.GetProperty("roster").GetArrayLength());Assert.Equal(8,after.GetProperty("ledger").GetArrayLength());
  var duplicate=await retry.PostAsJsonAsync(path.Replace("?","/hire?"),finalCommand);duplicate.EnsureSuccessStatusCode();Assert.Equal(after.ToString(),(await duplicate.Content.ReadFromJsonAsync<JsonElement>()).ToString());
  var final=await retry.GetFromJsonAsync<JsonElement>($"/api/campaigns/{id}?deviceId={device}");
  Assert.Equal("created",final.GetProperty("lifecycle").GetString());Assert.True(final.GetProperty("acquisition").GetProperty("allRostersReady").GetBoolean());Assert.Equal(77,final.GetProperty("acquisition").GetProperty("filledSlots").GetInt32());
  var finalTeam=final.GetProperty("teams").EnumerateArray().Single(t=>t.GetProperty("teamId").GetString()==after.GetProperty("teamId").GetString());
  Assert.Equal(after.GetProperty("roster").EnumerateArray().Where(p=>p.GetProperty("role").GetString()=="Driver").Select(p=>p.GetProperty("id").GetString()).Order(),finalTeam.GetProperty("drivers").EnumerateArray().Select(p=>p.GetProperty("personId").GetString()).Order());
  (await retry.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).EnsureSuccessStatusCode();
  var preparation=await retry.GetFromJsonAsync<JsonElement>($"/api/lobbies/{final.GetProperty("weekendId").GetString()}/preparation/teams/{after.GetProperty("teamId").GetString()}?deviceId={device}");
  Assert.Equal(finalTeam.GetProperty("drivers").EnumerateArray().Select(d=>d.GetProperty("personId").GetString()),preparation.GetProperty("preset").GetProperty("cars").EnumerateArray().Select(c=>c.GetProperty("driver").GetProperty("id").GetString()));
  Assert.All(preparation.GetProperty("preset").GetProperty("cars").EnumerateArray(),car=>Assert.StartsWith("staff-race-engineer-",car.GetProperty("engineer").GetProperty("id").GetString()));
 }
 [Theory]
 [InlineData(false)]
 [InlineData(true)]
 public async Task Reserved_first_offer_and_atomic_hire_survive_restart_and_uncertain_acknowledgement(bool committed)
 {
  var token=await LobbyTests.Identity(false);
  var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:21980");
  var interrupted=new InterruptedStore(durable,committed);
  string id,weekend,device;JsonElement offer;object command;
  await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted)))))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var created=await client.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Host",personnelMode="acquisition"});created.EnsureSuccessStatusCode();
   var joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;
   Assert.Equal("acquiring",joined.Campaign.Lifecycle);
   id=joined.Campaign.CampaignId;weekend=joined.Campaign.WeekendId;device=joined.Weekend.Membership.DeviceId;
   (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/claims",new ClaimTeamInput("team-03",device))).EnsureSuccessStatusCode();
   var path=$"/api/campaigns/{id}/acquisition/teams/team-03?deviceId={device}";
   offer=await client.GetFromJsonAsync<JsonElement>(path);
   Assert.Equal(100,offer.GetProperty("balance").GetInt32());Assert.Equal(3,offer.GetProperty("offers").GetArrayLength());
   Assert.Equal(offer.ToString(),(await client.GetFromJsonAsync<JsonElement>(path)).ToString());
   var person=offer.GetProperty("offers")[0];
   command=new{commandId=Guid.NewGuid().ToString(),revision=offer.GetProperty("revision").GetInt64(),teamId="team-03",personId=person.GetProperty("id").GetString()};
   Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).StatusCode);
   using var outsider=server.CreateClient();outsider.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));
   Assert.Equal(HttpStatusCode.Forbidden,(await outsider.GetAsync(path)).StatusCode);
   Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync(path.Replace("?","/hire?"),command)).StatusCode);
   Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(path.Replace("?","/hire?"),new{commandId=Guid.NewGuid().ToString(),revision=0,teamId="team-03",personId=person.GetProperty("id").GetString()})).StatusCode);
   Assert.Equal(offer.ToString(),(await client.GetFromJsonAsync<JsonElement>(path)).ToString());
   Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/lobbies/{weekend}/preparation",new BeginPreparationInput("track-monza"))).StatusCode);
   interrupted.Armed=true;
   Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(path.Replace("?","/hire?"),command)).StatusCode);
  }
  await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));
  using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var uri=$"/api/campaigns/{id}/acquisition/teams/team-03?deviceId={device}";
  var purchased=await retry.PostAsJsonAsync(uri.Replace("?","/hire?"),command);purchased.EnsureSuccessStatusCode();
  var after=(await purchased.Content.ReadFromJsonAsync<JsonElement>())!;
  Assert.Equal(100-offer.GetProperty("offers")[0].GetProperty("price").GetInt32(),after.GetProperty("balance").GetInt32());
  Assert.Equal(1,after.GetProperty("cursor").GetInt32());Assert.Equal(2,after.GetProperty("drawCount").GetInt32());Assert.Equal("team-01",after.GetProperty("activeTeamId").GetString());Assert.Empty(after.GetProperty("offers").EnumerateArray());
  Assert.Single(after.GetProperty("hires").EnumerateArray());Assert.Equal(2,after.GetProperty("ledger").GetArrayLength());
  var duplicate=await retry.PostAsJsonAsync(uri.Replace("?","/hire?"),command);duplicate.EnsureSuccessStatusCode();Assert.Equal(after.ToString(),(await duplicate.Content.ReadFromJsonAsync<JsonElement>()).ToString());
  var publicSnapshot=await retry.GetStringAsync($"/api/campaigns/{id}?deviceId={device}");Assert.DoesNotContain("\"balance\":",publicSnapshot);Assert.DoesNotContain("\"offers\":",publicSnapshot);
 }
 [Theory]
 [InlineData(false)]
 [InlineData(true)]
 public async Task Scout_commit_is_durable_and_replayed_once_after_write_failure_or_lost_acknowledgement(bool committed)
 {
  var token=await LobbyTests.Identity(false);
  var durable=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:22080");
  var interrupted=new InterruptedStore(durable,committed);
  string path;JsonElement before;object scout;
  await using(var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(interrupted)))))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var response=await client.PostAsJsonAsync("/api/campaigns",new{commandId=Guid.NewGuid().ToString(),name="Scout host",personnelMode="acquisition"});response.EnsureSuccessStatusCode();
   var joined=(await response.Content.ReadFromJsonAsync<CampaignJoined>())!;
   var device=joined.Weekend.Membership.DeviceId;var weekend=joined.Campaign.WeekendId;
   (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/claims",new ClaimTeamInput("team-03",device))).EnsureSuccessStatusCode();
   path=$"/api/campaigns/{joined.Campaign.CampaignId}/acquisition/teams/team-03?deviceId={device}";
   before=await client.GetFromJsonAsync<JsonElement>(path);
   scout=new{commandId=Guid.NewGuid().ToString(),revision=before.GetProperty("revision").GetInt64(),teamId="team-03"};
   using var outsider=server.CreateClient();outsider.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));
   Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync(path.Replace("?","/scout?"),scout)).StatusCode);
   interrupted.Armed=true;
   Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(path.Replace("?","/scout?"),scout)).StatusCode);
  }
  await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>s.Replace(ServiceDescriptor.Singleton<ISessionStore>(durable))));
  using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var result=await retry.PostAsJsonAsync(path.Replace("?","/scout?"),scout);result.EnsureSuccessStatusCode();
  var after=(await result.Content.ReadFromJsonAsync<JsonElement>())!;
  Assert.Equal(before.GetProperty("scoutTokens").GetInt32()-1,after.GetProperty("scoutTokens").GetInt32());
  Assert.Equal(before.GetProperty("balance").GetInt32(),after.GetProperty("balance").GetInt32());
  Assert.Equal(before.GetProperty("cursor").GetInt32(),after.GetProperty("cursor").GetInt32());
  Assert.Equal(before.GetProperty("drawCount").GetInt32()+1,after.GetProperty("drawCount").GetInt32());
  var original=before.GetProperty("offers").EnumerateArray().Select(p=>p.GetProperty("id").GetString());
  Assert.Empty(after.GetProperty("offers").EnumerateArray().Select(p=>p.GetProperty("id").GetString()).Intersect(original));
  var duplicate=await retry.PostAsJsonAsync(path.Replace("?","/scout?"),scout);duplicate.EnsureSuccessStatusCode();
  Assert.Equal(after.ToString(),(await duplicate.Content.ReadFromJsonAsync<JsonElement>()).ToString());
  Assert.Equal(after.ToString(),(await retry.GetFromJsonAsync<JsonElement>(path)).ToString());
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync(path.Replace("?","/scout?"),new{commandId=Guid.NewGuid().ToString(),revision=before.GetProperty("revision").GetInt64(),teamId="team-03"})).StatusCode);
  Assert.Equal(after.ToString(),(await retry.GetFromJsonAsync<JsonElement>(path)).ToString());
 }
 private sealed class InterruptedStore(ISessionStore inner,bool committed):ISessionStore
 {
  public bool Armed {get;set;}
  public int WritesBeforeFailure;public bool IncludeLobby;
  public StoredSession? Read(string id)=>inner.Read(id);
  public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
  public string Write(string id,string? version,string payload,string? code)
  {
   if(Armed&&(IncludeLobby||id.StartsWith("campaign-",StringComparison.Ordinal))&&WritesBeforeFailure--==0){Armed=false;if(committed)inner.Write(id,version,payload,code);throw new IOException("Save failed or acknowledgement lost");}return inner.Write(id,version,payload,code);
  }
 }
}
