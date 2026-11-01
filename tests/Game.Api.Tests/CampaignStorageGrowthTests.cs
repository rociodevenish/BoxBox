using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Application;
using Game.Domain;
using Game.Simulation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using Xunit.Abstractions;
namespace Game.Api.Tests;

public sealed class CampaignStorageGrowthTests(ITestOutputHelper output)
{
 private sealed class Clock:TimeProvider
 {
  private DateTimeOffset now=DateTimeOffset.Parse("2026-10-30T00:00:00Z");
  public override DateTimeOffset GetUtcNow()=>now;
  public void Advance()=>now=now.AddHours(1);
 }
 private sealed class Notifications:ISessionNotifications
 {
  public Task Publish(PublicSnapshot display,TeamSnapshot team)=>Task.CompletedTask;
  public Task PublishHousehold(PublicSnapshot display,IReadOnlyList<TeamSnapshot> teams,IReadOnlyList<OpenTeamSnapshot> open)=>Task.CompletedTask;
 }
 private sealed class InterruptedStore(ISessionStore inner):ISessionStore
 {
  public bool Armed,CommitFirst;
  public StoredSession? Read(string id)=>inner.Read(id);
  public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
  public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
  public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
  public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup)
  {
   if(Armed&&id.StartsWith("campaign-",StringComparison.Ordinal)){Armed=false;if(CommitFirst)inner.Write(id,version,payload,code,wakeup);throw new IOException("Interrupted campaign acknowledgement");}
   return inner.Write(id,version,payload,code,wakeup);
  }
 }
 [Fact]
 public async Task Growing_legitimate_advice_history_survives_durable_restart_CAS_and_uncertain_acknowledgements_without_losing_integrity()
 {
  var token=await LobbyTests.Identity(false);
  var tokenBody=token.Split('.')[1];using var claims=JsonDocument.Parse(Convert.FromBase64String(tokenBody.Replace('-','+').Replace('_','/').PadRight((tokenBody.Length+3)/4*4,'=')));
  var host=new AuthIdentity(claims.RootElement.GetProperty("sub").GetString()!,false);
  var clock=new Clock();var memory=new VolatileSessionStore();var campaignsStore=new CampaignStore(memory);
  var weekends=new SessionCoordinator(new Notifications(),new DryRacePresets(),memory,clock);
  var campaigns=new CampaignCoordinator(weekends,campaignsStore,clock);
  var joined=campaigns.Create(host,new(Guid.NewGuid().ToString(),"Storage host"));var id=joined.Campaign.CampaignId;
  var first=joined.Campaign.WeekendId;var device=joined.Weekend.Membership.DeviceId;
  weekends.ClaimTeam(first,host,new("team-01",device));
  await weekends.SetPlayerAssistance(first,host,joined.Weekend.Membership.PlayerId,new("Assisted",false));
  campaigns.Prepare(id,host);campaigns.PrepareAbsent(id,host,new("team-01"));campaigns.Start(id,host,new(Guid.NewGuid().ToString()));
  for(var n=0;n<100&&weekends.CommittedCampaignResult(first) is null;n++){clock.Advance();await weekends.SweepWakeups();}
  Assert.NotNull(weekends.CommittedCampaignResult(first));campaigns.CommitResult(id,host,device);campaigns.PrepareNext(id,host);
  device=campaigns.Return(id,host,new(true,device)).Membership.DeviceId;
  var own=campaigns.DevelopmentTeam(id,host,device,"team-01");
  var timer=Stopwatch.StartNew();CampaignRecord record=campaignsStore.Read(id)!.Record;var rawBytes=0;
  for(var n=0;n<240;n++)
  {
   own=campaigns.RecommendDevelopment(id,host,device,"team-01",new(Guid.NewGuid().ToString(),own.OpportunityId,own.Revision,own.AssignmentRevision));
   if(n%10!=9)continue;
   record=campaignsStore.Read(id)!.Record;rawBytes=JsonSerializer.SerializeToUtf8Bytes(record,BrowserReplay.JsonOptions).Length;
   if(rawBytes>4_100_000)break;
  }
  Assert.True(rawBytes>4_000_000,"The public advice workload must exceed the unchanged durable raw-payload limit.");
  var expected=JsonSerializer.Serialize(record,BrowserReplay.JsonOptions);var choiceCount=record.DevelopmentChoices!.Length;
  output.WriteLine("Public advice workload: {0} choices, {1} raw UTF-8 bytes, {2:F0}ms",choiceCount,rawBytes,timer.Elapsed.TotalMilliseconds);
  using var firestore=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  var durable=new CampaignTestSessionScope(firestore);var storage=new CampaignStore(durable);
  foreach(var e in record.Calendar)
  {
   var session=memory.Read(e.SessionId);if(session is not null)durable.Write(e.SessionId,null,session.Payload,null);
  }
  var version=storage.Write(id,null,record);
  var persisted=durable.Read("campaign-"+id)!;
  output.WriteLine("Durable campaign payload: {0} UTF-8 bytes",Encoding.UTF8.GetByteCount(persisted.Payload));
  Assert.InRange(Encoding.UTF8.GetByteCount(persisted.Payload),1,4_000_000);
  Assert.Equal(expected,JsonSerializer.Serialize(new CampaignStore(durable).Read(id)!.Record,BrowserReplay.JsonOptions));
  var nextVersion=storage.Write(id,version,record);
  Assert.Throws<SessionFault>(()=>storage.Write(id,version,record));
  Assert.Equal(nextVersion,durable.Read("campaign-"+id)!.Version);
  var interrupted=new InterruptedStore(durable);
  foreach(var commitFirst in new[]{false,true})
  {
   await using var server=Server(interrupted,clock);using var client=Client(server,token);
   var endpoint=$"/api/campaigns/{id}/development/teams/team-01?deviceId={device}";
   var restored=(await client.GetFromJsonAsync<DevelopmentTeamSnapshot>(endpoint))!;Assert.Equal(choiceCount,restored.PolicyChoices!.Count);
   var command=new DevelopmentRecommendationInput(Guid.NewGuid().ToString(),restored.OpportunityId,restored.Revision,restored.AssignmentRevision);
   interrupted.Armed=true;interrupted.CommitFirst=commitFirst;
   var failed=await client.PostAsJsonAsync(endpoint.Replace("?","/recommend?"),command);Assert.Equal(HttpStatusCode.ServiceUnavailable,failed.StatusCode);
   await using var restarted=Server(interrupted,clock);using var retry=Client(restarted,token);
   (await retry.PostAsJsonAsync(endpoint.Replace("?","/recommend?"),command)).EnsureSuccessStatusCode();
   var duplicate=await retry.PostAsJsonAsync(endpoint.Replace("?","/recommend?"),command);duplicate.EnsureSuccessStatusCode();
   var after=(await duplicate.Content.ReadFromJsonAsync<DevelopmentTeamSnapshot>())!;
   Assert.Equal(++choiceCount,after.PolicyChoices!.Count);Assert.Equal(106,after.Balance);Assert.Null(after.Commitment);
  }
  // A cached pure recommendation must not bypass the surrounding saved receipt/roster/ledger checks.
  var valid=storage.Read(id)!.Record;var good=durable.Read("campaign-"+id)!;
  // Hostile oversized persistence requests must fail before publishing a replacement manifest.
  // These are rejected requests, not fabricated accepted campaign histories.
  var expandedChoices=Enumerable.Range(0,40).SelectMany(_=>valid.DevelopmentChoices!).ToArray();
  var rawLimit=Assert.Throws<SessionFault>(()=>storage.Write(id,good.Version,valid with{DevelopmentChoices=expandedChoices}));
  Assert.Equal(SessionFailure.InvalidInput,rawLimit.Failure);Assert.Contains("No saved data was changed",rawLimit.Message);
  Assert.Equal(good,durable.Read("campaign-"+id));
  var noise=Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4_000_000));
  var encodedLimit=Assert.Throws<SessionFault>(()=>storage.Write(id,good.Version,valid with{CreationFingerprint=noise}));
  Assert.Equal(SessionFailure.InvalidInput,encodedLimit.Failure);Assert.Contains("Preserve",encodedLimit.Message);
  Assert.Equal(good,durable.Read("campaign-"+id));
  foreach(var corruption in new[]{"recommendation","source","balance","library"})
  {
   var choices=valid.DevelopmentChoices!.ToArray();choices[0]=corruption=="source"?choices[0] with{Source="forged"}:choices[0] with{Recommendation=choices[0].Recommendation with{Cost=999}};
   if(corruption is "balance" or "library")
   {
    var observed=valid.DevelopmentChoices![0].Observation;
    observed=corruption=="balance"?observed with{Balance=observed.Balance-1}:observed with{Library=observed.Library.Add(PackageDevelopment.PreviewOutcome(PackageDevelopment.QuoteRich("downforce",new("synthetic-not-owned",0,0,0,0)),"normal"))};
    // Prime the pure cache with a valid evaluation, then verify record context still rejects it.
    choices[0]=valid.DevelopmentChoices![0] with{Observation=observed,Recommendation=DevelopmentDecisionPolicy.Evaluate(observed)};
   }
   storage.Write(id,durable.Read("campaign-"+id)!.Version,valid with{DevelopmentChoices=choices});
   await RejectedWithoutMutation(durable,id,device,token,clock);
   durable.Write("campaign-"+id,durable.Read("campaign-"+id)!.Version,good.Payload,null);
  }
  // Mutate only the actual stored envelope, then enter through the ordinary campaign API.
  foreach(var corruption in new[]{"format","length","oversize","digest","data","truncated","trailing"})
  {
   var envelope=JsonNode.Parse(good.Payload)!;
   switch(corruption)
   {
    case "format":envelope["storageFormat"]="future-campaign-v999";break;
    case "length":envelope["uncompressedBytes"]=1;break;
    case "oversize":envelope["uncompressedBytes"]=128*1024*1024+1;break;
    case "digest":envelope["sha256"]=new string('0',64);break;
    case "data":envelope["data"]="not base64";break;
    case "truncated":envelope["data"]=Convert.ToBase64String(Convert.FromBase64String(envelope["data"]!.GetValue<string>())[..^1]);break;
    case "trailing":envelope["data"]=Convert.ToBase64String([..Convert.FromBase64String(envelope["data"]!.GetValue<string>()),0]);break;
   }
   durable.Write("campaign-"+id,durable.Read("campaign-"+id)!.Version,envelope.ToJsonString(),null);
   await RejectedWithoutMutation(durable,id,device,token,clock);
   durable.Write("campaign-"+id,durable.Read("campaign-"+id)!.Version,good.Payload,null);
  }
 }
 private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Server(ISessionStore store,TimeProvider clock)=>LobbyTests.Server().WithWebHostBuilder(b=>b.UseSetting("Wakeups:Mode","manual").ConfigureServices(s=>{s.Replace(ServiceDescriptor.Singleton<ISessionStore>(store));s.Replace(ServiceDescriptor.Singleton(clock));}));
 private static HttpClient Client(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> server,string token){var client=server.CreateClient();client.DefaultRequestHeaders.Authorization=new("Bearer",token);return client;}
 private static async Task RejectedWithoutMutation(ISessionStore durable,string id,string device,string token,TimeProvider clock)
 {
  var before=durable.Read("campaign-"+id)!;await using var server=Server(durable,clock);using var client=Client(server,token);
  var rejected=await client.GetAsync($"/api/campaigns/{id}?deviceId={device}");Assert.Equal(HttpStatusCode.Conflict,rejected.StatusCode);Assert.Contains("Preserve",await rejected.Content.ReadAsStringAsync());
  Assert.Equal(before,durable.Read("campaign-"+id));
 }
}
