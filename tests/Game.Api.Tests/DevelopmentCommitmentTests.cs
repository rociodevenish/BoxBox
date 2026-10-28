using System.Net;
using System.Net.Http.Json;
using Game.Application;
using Game.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class DevelopmentCommitmentTests
{
 private sealed class Clock:TimeProvider {private DateTimeOffset now=DateTimeOffset.Parse("2026-10-25T00:00:00Z");public override DateTimeOffset GetUtcNow()=>now;public void Advance()=>now=now.AddHours(1);}
 private sealed class InterruptedStore(ISessionStore inner):ISessionStore
 {
  public bool Armed;public bool CommitFirst;
  public StoredSession? Read(string id)=>inner.Read(id);
  public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
  public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
  public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
  public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup)
  {if(Armed&&id.StartsWith("campaign-")){Armed=false;if(CommitFirst)inner.Write(id,version,payload,code,wakeup);throw new IOException("Lost campaign acknowledgement");}return inner.Write(id,version,payload,code,wakeup);}
 }
 [Theory][InlineData(false)][InlineData(true)]
 public async Task Development_is_private_guarded_and_same_commit_survives_failed_write_or_lost_ack(bool commitFirst)
 {
  var token=await LobbyTests.Identity(false);var clock=new Clock();
  var store=new InterruptedStore(new CampaignTestSessionScope(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19600")));
  await using var server=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store));}));
  using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host"));created.EnsureSuccessStatusCode();var joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;
  var id=joined.Campaign.CampaignId;var first=joined.Campaign.WeekendId;var device=joined.Weekend.Membership.DeviceId;
  (await client.PostAsJsonAsync($"/api/lobbies/{first}/claims",new ClaimTeamInput("team-01",device))).EnsureSuccessStatusCode();
  (await client.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).EnsureSuccessStatusCode();
  var uri=$"/api/lobbies/{first}/preparation/teams/team-01?deviceId={device}";
  foreach(var operation in new[]{"practice","confirm"}){var own=(await client.GetFromJsonAsync<PreparationTeamSnapshot>(uri))!;(await client.PostAsJsonAsync(uri.Replace("?","/"+operation+"?"),new PracticeCommand(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision))).EnsureSuccessStatusCode();}
  (await client.PostAsJsonAsync($"/api/campaigns/{id}/start",new CampaignStartInput(Guid.NewGuid().ToString()))).EnsureSuccessStatusCode();
  var sessions=server.Services.GetRequiredService<SessionCoordinator>();for(var n=0;n<80&&sessions.CommittedCampaignResult(first) is null;n++){clock.Advance();await sessions.SweepWakeups();}
  (await client.PostAsJsonAsync($"/api/campaigns/{id}/commit-result?deviceId={device}",new{})).EnsureSuccessStatusCode();
  var next=await client.PostAsJsonAsync($"/api/campaigns/{id}/next/prepare",new{});next.EnsureSuccessStatusCode();var state=(await next.Content.ReadFromJsonAsync<CampaignSnapshot>())!;
  Assert.NotNull(state.Development);Assert.False(state.NextPreparation!.CanStart);
  var second=state.NextPreparation.SessionId;
  Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync($"/api/lobbies/{second}/preparation",new BeginPreparationInput("track-monaco"))).StatusCode);
  var returned=await client.PostAsJsonAsync($"/api/campaigns/{id}/return",new CampaignReturnInput(true,device));returned.EnsureSuccessStatusCode();device=(await returned.Content.ReadFromJsonAsync<JoinedLobby>())!.Membership.DeviceId;
  var endpoint=$"/api/campaigns/{id}/development/teams/team-01?deviceId={device}";
  var ownDevelopment=(await client.GetFromJsonAsync<DevelopmentTeamSnapshot>(endpoint))!;
  Assert.Equal(106,ownDevelopment.Balance);
  using var outsider=server.CreateClient();outsider.DefaultRequestHeaders.Authorization=new("Bearer",await LobbyTests.Identity(true));Assert.Equal(HttpStatusCode.Forbidden,(await outsider.GetAsync(endpoint)).StatusCode);
  var quote=ownDevelopment.Options.Single(q=>q.Option.Id=="speed");
  var draft=new DevelopmentEdit(Guid.NewGuid().ToString(),ownDevelopment.OpportunityId,ownDevelopment.Revision,"PermanentRefinement","speed",quote.Id,ownDevelopment.AssignmentRevision);
  var edited=await client.PostAsJsonAsync(endpoint.Replace("?","/draft?"),draft);edited.EnsureSuccessStatusCode();ownDevelopment=(await edited.Content.ReadFromJsonAsync<DevelopmentTeamSnapshot>())!;Assert.Equal(106,ownDevelopment.Balance);
  var command=new DevelopmentCommit(Guid.NewGuid().ToString(),ownDevelopment.OpportunityId,ownDevelopment.Revision,ownDevelopment.Draft.Revision,quote.Id,ownDevelopment.AssignmentRevision);
  Assert.Equal(HttpStatusCode.Conflict,(await client.PostAsJsonAsync(endpoint.Replace("?","/commit?"),command with{AssignmentRevision=command.AssignmentRevision-1})).StatusCode);
  store.Armed=true;store.CommitFirst=commitFirst;
  Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync(endpoint.Replace("?","/commit?"),command)).StatusCode);
  await using var restarted=LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store));}));
  using var retry=restarted.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var committed=await retry.PostAsJsonAsync(endpoint.Replace("?","/commit?"),command);committed.EnsureSuccessStatusCode();var accepted=(await committed.Content.ReadFromJsonAsync<DevelopmentTeamSnapshot>())!;
  Assert.Equal(90,accepted.Balance);Assert.NotNull(accepted.Commitment);Assert.Equal(quote.Id,accepted.Commitment.Quote!.Id);
  var again=await retry.PostAsJsonAsync(endpoint.Replace("?","/commit?"),command);again.EnsureSuccessStatusCode();Assert.Equal(90,(await again.Content.ReadFromJsonAsync<DevelopmentTeamSnapshot>())!.Balance);
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync(endpoint.Replace("?","/commit?"),command with{CommandId=Guid.NewGuid().ToString()})).StatusCode);
  var ledger=(await retry.GetFromJsonAsync<EconomyTeamSnapshot>($"/api/campaigns/{id}/economy/teams/team-01?deviceId={joined.Weekend.Membership.DeviceId}"))!;Assert.Single(ledger.Ledger,e=>e.Reason=="permanent-development");
 }
}

