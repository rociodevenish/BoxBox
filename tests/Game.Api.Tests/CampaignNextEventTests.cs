using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Net.Http.Json;
using Game.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class CampaignNextEventTests
{
 private sealed class Clock:TimeProvider {private DateTimeOffset now=DateTimeOffset.Parse("2026-10-25T00:00:00Z");public override DateTimeOffset GetUtcNow()=>now;public void Advance()=>now=now.AddHours(1);}
 private sealed class InterruptedStore(ISessionStore inner):ISessionStore
 {
  public string? Match;public int Remaining;public bool CommitFirst;
  public StoredSession? Read(string id)=>inner.Read(id);
  public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
  public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
  public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
  public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup)
  {
   if(Match is not null&&id.Contains(Match)&&--Remaining==0){Match=null;if(CommitFirst)inner.Write(id,version,payload,code,wakeup);throw new IOException("Lost transition acknowledgement");}
   return inner.Write(id,version,payload,code,wakeup);
  }
 }
 [Theory][InlineData("Competitive",0,false)][InlineData("Family",0,false)][InlineData("Mixed",0,false)]
 [InlineData("Competitive",1,false)][InlineData("Competitive",1,true)]
 [InlineData("Competitive",2,false)][InlineData("Competitive",2,true)]
 [InlineData("Competitive",3,false)][InlineData("Competitive",3,true)]
 [InlineData("Competitive",4,false)][InlineData("Competitive",4,true)]
 public async Task Returning_member_prepares_second_circuit_without_changing_history_or_rerolling_practice(string mode,int interrupted,bool committedFirst)
 {
  var token=await LobbyTests.Identity(false);var clock=new Clock();
  var store=new InterruptedStore(new CampaignTestSessionScope(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19600")));
  await using var server=LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store));}));
  using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host",mode));created.EnsureSuccessStatusCode();var joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;
  var id=joined.Campaign.CampaignId;var first=joined.Campaign.WeekendId;var device=joined.Weekend.Membership.DeviceId;
  (await client.PostAsJsonAsync($"/api/lobbies/{first}/claims",new ClaimTeamInput("team-01",device))).EnsureSuccessStatusCode();
  (await client.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).EnsureSuccessStatusCode();
  await Ready(client,first,device);
  (await client.PostAsJsonAsync($"/api/campaigns/{id}/start",new CampaignStartInput(Guid.NewGuid().ToString()))).EnsureSuccessStatusCode();
  var sessions=server.Services.GetRequiredService<SessionCoordinator>();
  for(var n=0;n<80&&sessions.CommittedCampaignResult(first) is null;n++){clock.Advance();await sessions.SweepWakeups();}
  (await client.PostAsJsonAsync($"/api/campaigns/{id}/commit-result?deviceId={device}",new{})).EnsureSuccessStatusCode();
  var newDeviceReturn=await client.PostAsJsonAsync($"/api/campaigns/{id}/return",new{});newDeviceReturn.EnsureSuccessStatusCode();
  var readOnly=(await newDeviceReturn.Content.ReadFromJsonAsync<JoinedLobby>())!;
  (await client.GetAsync($"/api/campaigns/{id}?deviceId={readOnly.Membership.DeviceId}")).EnsureSuccessStatusCode();
  Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/lobbies/{first}/preparation/teams/team-01?deviceId={readOnly.Membership.DeviceId}")).StatusCode);
  if(interrupted is 1 or 2){store.Match=interrupted==1?"campaign-":"event-02-weekend";store.Remaining=1;store.CommitFirst=committedFirst;var lost=await client.PostAsJsonAsync($"/api/campaigns/{id}/next/prepare",new{});Assert.Equal(HttpStatusCode.ServiceUnavailable,lost.StatusCode);}
  var next=await client.PostAsJsonAsync($"/api/campaigns/{id}/next/prepare",new{});next.EnsureSuccessStatusCode();
  var state=(await next.Content.ReadFromJsonAsync<CampaignSnapshot>())!;Assert.Equal(0,state.EventIndex);Assert.Single(state.Results!);
  (await client.PostAsJsonAsync($"/api/campaigns/{id}/development/close",new CloseDevelopmentInput(Guid.NewGuid().ToString(),state.Development!.OpportunityId,state.Development.Revision,true))).EnsureSuccessStatusCode();
  await using var observing=LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store));}));
  using var observer=observing.CreateClient();observer.DefaultRequestHeaders.Authorization=new("Bearer",token);
  (await observer.GetAsync($"/api/lobbies/{state.NextPreparation!.SessionId}/preparation/teams/team-01?deviceId={device}")).EnsureSuccessStatusCode();
  var returning=await client.PostAsJsonAsync($"/api/campaigns/{id}/return",new{nextEvent=true,deviceId=readOnly.Membership.DeviceId});returning.EnsureSuccessStatusCode();var membership=(await returning.Content.ReadFromJsonAsync<JoinedLobby>())!;
  Assert.Equal(joined.Weekend.Membership.PlayerId,membership.Membership.PlayerId);Assert.NotEqual(first,membership.Lobby.SessionId);
  var second=membership.Lobby.SessionId;var nextDevice=membership.Membership.DeviceId;
  Assert.Equal(HttpStatusCode.Forbidden,(await observer.GetAsync($"/api/lobbies/{second}/preparation/teams/team-01?deviceId={device}")).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync($"/api/lobbies/{second}/preparation/teams/team-01?deviceId={device}")).StatusCode);
  Assert.Equal(mode=="Family"?HttpStatusCode.OK:HttpStatusCode.Forbidden,(await client.GetAsync($"/api/lobbies/{second}/preparation/open/team-01")).StatusCode);
  using var outsider=server.CreateClient();outsider.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));
  Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync($"/api/campaigns/{id}/return",new{nextEvent=true,deviceId=nextDevice})).StatusCode);
  Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/campaigns/{id}/next/start",new CampaignStartInput(Guid.NewGuid().ToString()))).StatusCode);
  await Ready(client,second,nextDevice);
  var own=(await client.GetFromJsonAsync<PreparationTeamSnapshot>($"/api/lobbies/{second}/preparation/teams/team-01?deviceId={nextDevice}"))!;
  Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/lobbies/{second}/preparation/teams/team-01/practice?deviceId={nextDevice}",new PracticeCommand(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision))).StatusCode);
  await server.DisposeAsync();
  await using var recovered=LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store));}));
  using var retry=recovered.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var saved=(await retry.GetFromJsonAsync<PreparationTeamSnapshot>($"/api/lobbies/{second}/preparation/teams/team-01?deviceId={nextDevice}"))!;Assert.Equal(System.Text.Json.JsonSerializer.Serialize(own.Assessment),System.Text.Json.JsonSerializer.Serialize(saved.Assessment));
  var preparation=(await retry.GetFromJsonAsync<PreparationPublicSnapshot>($"/api/lobbies/{second}/preparation"))!;
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync($"/api/lobbies/{second}/qualifying",new StartQualifyingCommand(Guid.NewGuid().ToString(),preparation.Revision))).StatusCode);
  if(interrupted==3){store.Match="campaign-";store.Remaining=1;store.CommitFirst=committedFirst;var lost=await retry.PostAsJsonAsync($"/api/campaigns/{id}/next/start",new CampaignStartInput(Guid.NewGuid().ToString()));Assert.Equal(HttpStatusCode.ServiceUnavailable,lost.StatusCode);}
  var starts=await Task.WhenAll(retry.PostAsJsonAsync($"/api/campaigns/{id}/next/start",new CampaignStartInput(Guid.NewGuid().ToString())),observer.PostAsJsonAsync($"/api/campaigns/{id}/next/start",new CampaignStartInput(Guid.NewGuid().ToString())));foreach(var response in starts)response.EnsureSuccessStatusCode();
  state=(await starts[0].Content.ReadFromJsonAsync<CampaignSnapshot>())!;Assert.Equal(1,state.EventIndex);Assert.Equal(second,state.WeekendId);Assert.Single(state.Results!);Assert.Equal(101,state.Standings!.Drivers.Sum(r=>r.Points));
  var resumed=recovered.Services.GetRequiredService<SessionCoordinator>();resumed.RefreshCampaignWeekend(second);for(var n=0;n<80&&resumed.CommittedCampaignResult(second) is null;n++){clock.Advance();await resumed.SweepWakeups();}
  if(interrupted==4){store.Match="campaign-";store.Remaining=1;store.CommitFirst=committedFirst;var lost=await retry.PostAsJsonAsync($"/api/campaigns/{id}/commit-result?deviceId={nextDevice}",new{});Assert.Equal(committedFirst?HttpStatusCode.OK:HttpStatusCode.ServiceUnavailable,lost.StatusCode);}
  var committed=await retry.PostAsJsonAsync($"/api/campaigns/{id}/commit-result?deviceId={nextDevice}",new{});committed.EnsureSuccessStatusCode();state=(await committed.Content.ReadFromJsonAsync<CampaignSnapshot>())!;
  Assert.Equal(2,state.Results!.Count);Assert.Equal(202,state.Standings!.Drivers.Sum(r=>r.Points));
  Assert.Equal("season-complete",state.Lifecycle);
  var finalRevision=state.Revision;
  var again=await retry.PostAsJsonAsync($"/api/campaigns/{id}/commit-result?deviceId={nextDevice}",new{});again.EnsureSuccessStatusCode();
  Assert.Equal(finalRevision,(await again.Content.ReadFromJsonAsync<CampaignSnapshot>())!.Revision);
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync($"/api/campaigns/{id}/next/prepare",new{})).StatusCode);
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync($"/api/campaigns/{id}/next/start",new CampaignStartInput(Guid.NewGuid().ToString()))).StatusCode);
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).StatusCode);
  var afterRestart=(await observer.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{id}?deviceId={nextDevice}"))!;
  Assert.Equal("season-complete",afterRestart.Lifecycle);Assert.Equal(finalRevision,afterRestart.Revision);Assert.Equal(2,afterRestart.Results!.Count);
 }
 private static async Task Ready(HttpClient client,string weekend,string device)
 {
  var uri=$"/api/lobbies/{weekend}/preparation/teams/team-01?deviceId={device}";
  foreach(var operation in new[]{"practice","confirm"}){var own=(await client.GetFromJsonAsync<PreparationTeamSnapshot>(uri))!;(await client.PostAsJsonAsync(uri.Replace("?","/"+operation+"?"),new PracticeCommand(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision))).EnsureSuccessStatusCode();}
 }
}
