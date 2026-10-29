using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Game.Application;
using Game.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class DevelopmentGuidanceApiTests
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
 public async Task Advice_is_free_and_unclaimed_AI_uses_one_ledger_action_with_durable_retry(bool commitFirst)
 {
  var token=await LobbyTests.Identity(false);var clock=new Clock();
  var store=new InterruptedStore(new CampaignTestSessionScope(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19600")));
  await using var server=LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store));}));
  using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var created=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host"));created.EnsureSuccessStatusCode();var joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;
  var id=joined.Campaign.CampaignId;var first=joined.Campaign.WeekendId;var device=joined.Weekend.Membership.DeviceId;
  (await client.PostAsJsonAsync($"/api/lobbies/{first}/claims",new ClaimTeamInput("team-01",device))).EnsureSuccessStatusCode();
  (await client.PostAsJsonAsync($"/api/lobbies/{first}/players/{joined.Weekend.Membership.PlayerId}/assistance",new PlayerAssistanceInput("Assisted",false))).EnsureSuccessStatusCode();
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
  Assert.Equal("Assisted",ownDevelopment.Help!.HelpLevel);Assert.NotNull(ownDevelopment.Advice);
  (await client.PostAsJsonAsync($"/api/lobbies/{second}/players/{joined.Weekend.Membership.PlayerId}/assistance",new PlayerAssistanceInput("Standard",false))).EnsureSuccessStatusCode();
  ownDevelopment=(await client.GetFromJsonAsync<DevelopmentTeamSnapshot>(endpoint))!;Assert.Null(ownDevelopment.Advice);
  var standardCommand=new DevelopmentRecommendationInput(Guid.NewGuid().ToString(),ownDevelopment.OpportunityId,ownDevelopment.Revision,ownDevelopment.AssignmentRevision);
  Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync(endpoint.Replace("?","/recommend?"),standardCommand)).StatusCode);
  (await client.PostAsJsonAsync($"/api/lobbies/{second}/players/{joined.Weekend.Membership.PlayerId}/assistance",new PlayerAssistanceInput("Assisted",false))).EnsureSuccessStatusCode();
  ownDevelopment=(await client.GetFromJsonAsync<DevelopmentTeamSnapshot>(endpoint))!;
  Assert.Equal("handling",ownDevelopment.Advice!.OptionId);
  var advice=new DevelopmentRecommendationInput(Guid.NewGuid().ToString(),ownDevelopment.OpportunityId,ownDevelopment.Revision,ownDevelopment.AssignmentRevision);
  Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync(endpoint.Replace("?","/recommend?"),advice)).StatusCode);
  var recommended=await client.PostAsJsonAsync(endpoint.Replace("?","/recommend?"),advice);recommended.EnsureSuccessStatusCode();ownDevelopment=(await recommended.Content.ReadFromJsonAsync<DevelopmentTeamSnapshot>())!;
  Assert.Equal(106,ownDevelopment.Balance);Assert.Null(ownDevelopment.Commitment);Assert.Equal("handling",ownDevelopment.Draft.OptionId);Assert.Single(ownDevelopment.PolicyChoices!);
  (await client.PostAsJsonAsync(endpoint.Replace("?","/recommend?"),advice)).EnsureSuccessStatusCode();
  (await client.PostAsJsonAsync($"/api/lobbies/{second}/players/{joined.Weekend.Membership.PlayerId}/assistance",new PlayerAssistanceInput("Standard",false))).EnsureSuccessStatusCode();
  ownDevelopment=(await client.GetFromJsonAsync<DevelopmentTeamSnapshot>(endpoint))!;Assert.Null(ownDevelopment.Advice);Assert.Empty(ownDevelopment.PolicyChoices!);
  (await client.PostAsJsonAsync($"/api/lobbies/{second}/players/{joined.Weekend.Membership.PlayerId}/assistance",new PlayerAssistanceInput("Guided",false))).EnsureSuccessStatusCode();
  ownDevelopment=(await client.GetFromJsonAsync<DevelopmentTeamSnapshot>(endpoint))!;Assert.NotNull(ownDevelopment.Advice);Assert.Equal("handling",ownDevelopment.Draft.OptionId);
  if(!commitFirst){(await client.PostAsJsonAsync($"/api/lobbies/{second}/players/{joined.Weekend.Membership.PlayerId}/assistance",new PlayerAssistanceInput("Standard",false))).EnsureSuccessStatusCode();}
  var auto=new DevelopmentAutomaticInput(Guid.NewGuid().ToString(),ownDevelopment.OpportunityId,ownDevelopment.Revision);
  Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync($"/api/campaigns/{id}/development/automatic",auto)).StatusCode);
  store.Armed=true;store.CommitFirst=commitFirst;
  Assert.Equal(HttpStatusCode.ServiceUnavailable,(await client.PostAsJsonAsync($"/api/campaigns/{id}/development/automatic",auto)).StatusCode);
  await using var restarted=LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store));}));
  using var retry=restarted.CreateClient();retry.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var automatic=await retry.PostAsJsonAsync($"/api/campaigns/{id}/development/automatic",auto);automatic.EnsureSuccessStatusCode();var snapshot=(await automatic.Content.ReadFromJsonAsync<CampaignSnapshot>())!;
  Assert.Equal(10,snapshot.Development!.Readiness.Count(r=>r.Committed));Assert.False(snapshot.Development.Readiness.Single(r=>r.TeamId=="team-01").Committed);
  (await retry.PostAsJsonAsync($"/api/campaigns/{id}/development/automatic",auto)).EnsureSuccessStatusCode();
  ownDevelopment=(await retry.GetFromJsonAsync<DevelopmentTeamSnapshot>(endpoint))!;Assert.Equal(106,ownDevelopment.Balance);Assert.Equal("handling",ownDevelopment.Draft.OptionId);Assert.Null(ownDevelopment.Commitment);
  Assert.Equal(commitFirst?"Guided":"Standard",ownDevelopment.Help!.HelpLevel);Assert.True(ownDevelopment.Help.CanChange);
  if(commitFirst)Assert.NotNull(ownDevelopment.Advice);else{Assert.Null(ownDevelopment.Advice);Assert.Empty(ownDevelopment.PolicyChoices!);}
  Assert.Equal(HttpStatusCode.Forbidden,(await outsider.PostAsJsonAsync($"/api/lobbies/{second}/players/{joined.Weekend.Membership.PlayerId}/assistance",new PlayerAssistanceInput("Assisted",false))).StatusCode);
  var lobby=(await retry.GetFromJsonAsync<LobbySnapshot>($"/api/lobbies/{second}"))!;
  var participant=await outsider.PostAsJsonAsync("/api/lobbies/join",new JoinLobbyInput(lobby.JoinCode,"Returning player"));participant.EnsureSuccessStatusCode();var participantMembership=(await participant.Content.ReadFromJsonAsync<JoinedLobby>())!.Membership;var participantDevice=participantMembership.DeviceId;
  (await outsider.PostAsJsonAsync($"/api/lobbies/{second}/claims",new ClaimTeamInput("team-02",participantDevice))).EnsureSuccessStatusCode();
  (await client.PostAsJsonAsync($"/api/lobbies/{second}/assignments",new AssignTeamInput("team-02",participantMembership.PlayerId,participantDevice,"Private"))).EnsureSuccessStatusCode();
  (await client.PostAsJsonAsync($"/api/lobbies/{second}/players/{participantMembership.PlayerId}/assistance",new PlayerAssistanceInput("Assisted",false))).EnsureSuccessStatusCode();
  var automaticTeam=(await outsider.GetFromJsonAsync<DevelopmentTeamSnapshot>($"/api/campaigns/{id}/development/teams/team-02?deviceId={participantDevice}"))!;
  Assert.False(automaticTeam.Help!.CanChange);
  Assert.Equal(92,automaticTeam.Balance);Assert.Equal("unclaimed-ai",automaticTeam.Commitment!.Source);Assert.Single(automaticTeam.PolicyChoices!);Assert.Equal("handling",automaticTeam.Commitment.Quote!.Option.Id);
  Assert.Equal(HttpStatusCode.Conflict,(await retry.PostAsJsonAsync($"/api/campaigns/{id}/development/close",new CloseDevelopmentInput(Guid.NewGuid().ToString(),snapshot.Development.OpportunityId,snapshot.Development.Revision))).StatusCode);
  var closed=await retry.PostAsJsonAsync($"/api/campaigns/{id}/development/close",new CloseDevelopmentInput(Guid.NewGuid().ToString(),snapshot.Development.OpportunityId,snapshot.Development.Revision,true));closed.EnsureSuccessStatusCode();
  ownDevelopment=(await retry.GetFromJsonAsync<DevelopmentTeamSnapshot>(endpoint))!;Assert.Equal("Save",ownDevelopment.Commitment!.ActionKind);Assert.Equal("host-absent-save",ownDevelopment.Commitment.Source);Assert.Equal(106,ownDevelopment.Balance);
  Assert.DoesNotContain("observation",await closed.Content.ReadAsStringAsync(),StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("entropy",await closed.Content.ReadAsStringAsync(),StringComparison.OrdinalIgnoreCase);
 }
}
