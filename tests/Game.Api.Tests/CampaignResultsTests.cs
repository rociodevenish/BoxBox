using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Game.Api;
using Game.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace Game.Api.Tests;
public sealed class CampaignResultsTests
{
 private sealed class Clock:TimeProvider {private DateTimeOffset now=DateTimeOffset.Parse("2026-10-25T00:00:00Z");public override DateTimeOffset GetUtcNow()=>now;public void Advance()=>now=now.AddHours(1);}
 private sealed class ScopedStore(ISessionStore inner):ISessionStore
 {
  private readonly HashSet<string> ids=[];public bool Fail;public bool CommitFirst;
  public bool CorruptResult;
  public StoredSession? Read(string id){var result=inner.Read(id);if(result is null||!CorruptResult||!id.StartsWith("campaign-"))return result;var json=System.Text.Json.Nodes.JsonNode.Parse(result.Payload)!;json["contributions"]![0]!["result"]!["hash"]="damaged";return result with{Payload=json.ToJsonString()};}
  public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
  public IReadOnlyList<string> ListSessions()=>ids.Where(id=>!id.StartsWith("campaign-")).ToArray();
  public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
  public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup)
  {
   if(Fail&&id.StartsWith("campaign-")){Fail=false;if(CommitFirst)inner.Write(id,version,payload,code,wakeup);throw new IOException("deliberate lost write");}
   var result=inner.Write(id,version,payload,code,wakeup);ids.Add(id);return result;
  }
 }
 private static WebApplicationFactory<Program> Server(ISessionStore store,Clock clock)=>LobbyTests.Server().WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton(store));s.Replace(ServiceDescriptor.Singleton<TimeProvider>(clock));}));
 [Theory][InlineData(false)][InlineData(true)]
 public async Task Finished_authoritative_weekend_scores_once_across_concurrent_coordinators_uncertain_write_and_restart(bool committed)
 {
  var token=await LobbyTests.Identity(false);var outsider=await LobbyTests.Identity(true);var clock=new Clock();
  var store=new ScopedStore(new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??"127.0.0.1:19590"));
  CampaignJoined joined;
  await using(var server=Server(store,clock))
  {
   using var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var created=await client.PostAsJsonAsync("/api/campaigns",new CreateCampaignInput(Guid.NewGuid().ToString(),"Host"));created.EnsureSuccessStatusCode();joined=(await created.Content.ReadFromJsonAsync<CampaignJoined>())!;
   var id=joined.Campaign.CampaignId;var weekend=joined.Campaign.WeekendId;var device=joined.Weekend.Membership.DeviceId;
   (await client.PostAsJsonAsync($"/api/lobbies/{weekend}/claims",new ClaimTeamInput("team-01",device))).EnsureSuccessStatusCode();
   (await client.PostAsJsonAsync($"/api/campaigns/{id}/prepare",new{})).EnsureSuccessStatusCode();
   var untrusted=await client.PostAsJsonAsync($"/api/campaigns/{id}/commit-result?deviceId={device}",new{classification=new[]{new{personId="driver-lando-norris",position=1,points=9999}}});untrusted.EnsureSuccessStatusCode();Assert.Empty((await untrusted.Content.ReadFromJsonAsync<CampaignSnapshot>())!.Results!);
   var ownUri=$"/api/lobbies/{weekend}/preparation/teams/team-01?deviceId={device}";
   foreach(var operation in new[]{"practice","confirm"})
   {
    var own=(await client.GetFromJsonAsync<PreparationTeamSnapshot>(ownUri))!;
    (await client.PostAsJsonAsync(ownUri.Replace("?","/"+operation+"?"),new PracticeCommand(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision))).EnsureSuccessStatusCode();
   }
   (await client.PostAsJsonAsync($"/api/campaigns/{id}/start",new CampaignStartInput(Guid.NewGuid().ToString()))).EnsureSuccessStatusCode();
   var sessions=server.Services.GetRequiredService<SessionCoordinator>();
   // The same public due delivery used by the hosted scheduler advances actual
   // qualifying, starting tyres, opening and race operations; no supplied result.
   for(var iteration=0;iteration<80;iteration++)
   {
    clock.Advance();await sessions.SweepWakeups();
    var state=await client.GetFromJsonAsync<JsonElement>($"/api/lobbies/{weekend}");
    if(sessions.CommittedCampaignResult(weekend) is not null)break;
    if(iteration==79)throw new Exception("Weekend never completed: "+state);
   }
   var durable=sessions.CommittedCampaignResult(weekend);Assert.NotNull(durable);Assert.Equal(22,durable.Classification.Length);Assert.All(durable.Classification,r=>Assert.StartsWith("driver-",r.PersonId));
   var endpoint=$"/api/campaigns/{id}/commit-result?deviceId={device}";
   using var foreign=server.CreateClient();foreign.DefaultRequestHeaders.Authorization=new("Bearer",outsider);
   Assert.Equal(HttpStatusCode.Forbidden,(await foreign.PostAsJsonAsync(endpoint,new{})).StatusCode);
   store.CommitFirst=committed;store.Fail=true;
   var uncertain=await client.PostAsJsonAsync(endpoint,new{});Assert.Equal(committed?HttpStatusCode.OK:HttpStatusCode.ServiceUnavailable,uncertain.StatusCode);
   await using var other=Server(store,clock);using var otherClient=other.CreateClient();otherClient.DefaultRequestHeaders.Authorization=new("Bearer",token);
   var responses=await Task.WhenAll(client.PostAsJsonAsync(endpoint,new{}),otherClient.PostAsJsonAsync(endpoint,new{}));
   foreach(var response in responses)response.EnsureSuccessStatusCode();
   var snapshot=(await responses[0].Content.ReadFromJsonAsync<CampaignSnapshot>())!;
   Assert.Equal("event-result-committed",snapshot.Lifecycle);Assert.Single(snapshot.Results!);
   var lateStart=await client.PostAsJsonAsync($"/api/campaigns/{id}/start",new CampaignStartInput(Guid.NewGuid().ToString()));lateStart.EnsureSuccessStatusCode();Assert.Equal("event-result-committed",(await lateStart.Content.ReadFromJsonAsync<CampaignSnapshot>())!.Lifecycle);Assert.Equal(22,snapshot.Standings!.Drivers.Count);Assert.Equal(11,snapshot.Standings.Constructors.Count);
   Assert.Equal(101,snapshot.Standings.Drivers.Sum(r=>r.Points));Assert.Equal(101,snapshot.Standings.Constructors.Sum(r=>r.Points));
   var report=JsonSerializer.Serialize(snapshot);foreach(var hidden in new[]{"Setup","Ratings","Recommendations","HostUid","Seed","Choices"})Assert.DoesNotContain(hidden,report);
  }
  await using var restarted=Server(store,clock);using var recovered=restarted.CreateClient();recovered.DefaultRequestHeaders.Authorization=new("Bearer",token);
  var after=(await recovered.GetFromJsonAsync<CampaignSnapshot>($"/api/campaigns/{joined.Campaign.CampaignId}?deviceId={joined.Weekend.Membership.DeviceId}"))!;
  Assert.Single(after.Results!);Assert.Equal(101,after.Standings!.Drivers.Sum(r=>r.Points));Assert.Equal("event-result-committed",after.Lifecycle);
  store.CorruptResult=true;Assert.Equal(HttpStatusCode.Conflict,(await recovered.GetAsync($"/api/campaigns/{joined.Campaign.CampaignId}?deviceId={joined.Weekend.Membership.DeviceId}")).StatusCode);
 }
}




