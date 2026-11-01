using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Application;
using Game.Domain;
using Game.Simulation;
using Xunit;
using Xunit.Abstractions;
namespace Game.Api.Tests;
public sealed class SessionStorageGrowthTests(ITestOutputHelper output)
{
 private static string Command()=>Guid.NewGuid().ToString();
 private sealed class Clock:TimeProvider {private DateTimeOffset now=DateTimeOffset.Parse("2026-11-01T00:00:00Z");public override DateTimeOffset GetUtcNow()=>now;public void Advance()=>now=now.AddHours(1);}
 private sealed class Notifications:ISessionNotifications
 {
  public Task Publish(PublicSnapshot display,TeamSnapshot team)=>Task.CompletedTask;
  public Task PublishHousehold(PublicSnapshot display,IReadOnlyList<TeamSnapshot> teams,IReadOnlyList<OpenTeamSnapshot> open)=>Task.CompletedTask;
 }
 private sealed class Interrupted(ISessionStore inner,string target,bool commitFirst):ISessionStore
 {
  public bool Armed;
  public StoredSession? Read(string id)=>inner.Read(id);
  public IReadOnlyList<string> ListSessions()=>inner.ListSessions();
  public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
  public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
  public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup){if(Armed&&id==target){Armed=false;if(commitFirst)inner.Write(id,version,payload,code,wakeup);throw new IOException("Uncertain session acknowledgement");}return inner.Write(id,version,payload,code,wakeup);}
 }
 // Focused public-domain late-library fixture, not a completed full campaign.
 // All22 paid projects use100 initialcredits plus22 ordinary six-credit grants.
 private static ImmutableArray<RacePackageVersion> LateLibrary(string team)
 {
  var staff=new DevelopmentPersonnel("preset-development-neutral-v1",0,0,0,0);var risk=DevelopmentRiskControls.Only("normal");
  var ledger=ImmutableArray.Create(new CreditEntry("start",team,100,"starting-credit"));
  var state=new DevelopmentState(PermanentDevelopment.Version,[],new string('a',64));
  var library=ImmutableArray.Create(RacePackages.Baseline);var calendar=Enumerable.Range(0,23).Select(i=>"event-"+i).ToArray();
  for(var i=0;i<22;i++){
   ledger=ledger.Add(new("income-"+i,team,6,"event-income"));
   var op=PermanentDevelopment.Open(team+"/op-"+i,calendar[i],calendar[i+1],[team]);
   var source=i==0?library[0]:library[1];var create=i==0;
   var quote=create?PackageDevelopment.QuoteRich("downforce",staff,risk):PackageDevelopment.QuoteRefinement(source,"strengthen",staff,risk,i+1);
   op=PermanentDevelopment.Edit(op,team,new(Command(),op.Id,op.Revision,create?"PackageCreation":"PackageRefinement","downforce",quote.Id,Package:create?null:new(source.PackageId,source.Version),Refinement:create?null:"strengthen"),staff,risk:risk,library:library);
   var paid=PermanentDevelopment.Commit(op,ledger,team,new(Command(),op.Id,op.Revision,op.Drafts[team].Revision,quote.Id),staff);ledger=paid.Ledger;
   state=PermanentDevelopment.Close(state with{Opportunities=state.Opportunities.Add(paid.State)},op.Id,calendar);
   library=ImmutableArray.Create(RacePackages.Baseline).AddRange(state.PackageOutcomes.Select(o=>o.Package!).Where(p=>p.Completed));
  }
  Assert.Equal(4,ledger.Sum(e=>e.Credits));Assert.Equal(23,library.Length);return library;
 }
 [Fact]
 public async Task Late_owned_library_preparation_exceeding_raw_adapter_bound_survives_durable_restart_without_losing_frozen_inputs()
 {
  var host=new AuthIdentity("storage-"+Command(),false);var memory=new VolatileSessionStore();
  var sourceSessions=new SessionCoordinator(new Notifications(),new DryRacePresets(),memory);
  var sourceCampaigns=new CampaignCoordinator(sourceSessions,new CampaignStore(memory));
  var created=sourceCampaigns.Create(host,new(Command(),"Fixture",Calendar:"full",ContentVersion:ReleaseContent.FullCalendarVersion));
  var record=new CampaignStore(memory).Read(created.Campaign.CampaignId)!.Record;var first=record.Calendar[0];
  var frozen=record.FrozenPresets[first.EventId].Select(p=>p with{PackageLibrary=LateLibrary(p.Cars[0].TeamId)}).ToArray();
  var id=Command();var clock=new Clock();var sessions=new SessionCoordinator(new Notifications(),new DryRacePresets(),memory,clock);
  var joined=sessions.EnsureCampaignLobby(id,host,new("open-equal",6,123,"Competitive","Storage fixture"),frozen,record.EventConditions[first.EventId]);
  sessions.ClaimTeam(id,host,new("team-01",joined.Membership.DeviceId));
  sessions.BeginCampaignPreparation(id,host,frozen);sessions.PrepareAbsentCampaignTeam(id,host,"team-01");
  sessions.StartQualifying(id,host,new(Command(),sessions.PreparationPublic(id).Revision));
  var expected=JsonSerializer.Serialize(sessions.FrozenPreparation(id,host),BrowserReplay.JsonOptions);
  for(var step=0;step<100;step++){
   if(sessions.HasQualifying(id)){var q=sessions.QualifyingPublic(id);if(q.Lifecycle=="session-result"&&q.Session!="Q3")await sessions.ContinueQualifying(id,host,new(Command(),int.Parse(q.Session[1..]),q.WindowRevision));else{clock.Advance();await sessions.SweepWakeups();}}
   else {var r=sessions.Public(id);if(r.Lifecycle=="finished")break;if(r.Lifecycle=="phase-result")await sessions.ContinueHousehold(id,host,new(Command(),r.Phase,r.WindowRevision));else{clock.Advance();await sessions.SweepWakeups();}}
  }
  var saved=memory.Read(id)!;using var envelope=JsonDocument.Parse(saved.Payload);var rawBytes=envelope.RootElement.TryGetProperty("uncompressedBytes",out var declared)?declared.GetInt32():Encoding.UTF8.GetByteCount(saved.Payload);output.WriteLine("Late library recovery payload {0} bytes",rawBytes);Assert.True(rawBytes>4_000_000,$"Fixture must exceed raw bound; observed {rawBytes}");
  Assert.InRange(Encoding.UTF8.GetByteCount(saved.Payload),1,4_000_000);
  // The read-through below must work with the real adapter, not only volatile memory.
  using var firestore=new FirestoreSessionStore("demo-f1game",Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")!);
  var durable=new CampaignTestSessionScope(firestore);durable.Write(id,null,saved.Payload,null);
  var restored=new SessionCoordinator(new Notifications(),new DryRacePresets(),durable);
  Assert.Equal(expected,JsonSerializer.Serialize(restored.FrozenPreparation(id,host),BrowserReplay.JsonOptions));
  Assert.Equal(11,restored.FrozenPreparation(id,host).Teams.Count);
  Assert.True(BrowserReplay.Verify(restored.DesignerRecord(id)).Matches);
  Assert.Equal(JsonSerializer.Serialize(sessions.DesignerRecord(id) with{Timing=null},BrowserReplay.JsonOptions),JsonSerializer.Serialize(restored.DesignerRecord(id) with{Timing=null},BrowserReplay.JsonOptions));
  output.WriteLine("Encoded session payload {0} bytes",Encoding.UTF8.GetByteCount(saved.Payload));
  foreach(var commitFirst in new[]{false,true}){
   var boundary=new Interrupted(durable,id,commitFirst);var before=durable.Read(id)!;
   var failing=new SessionCoordinator(new Notifications(),new DryRacePresets(),boundary);
   _=failing.Public(id);boundary.Armed=true;
   var failure=await Assert.ThrowsAsync<SessionFault>(()=>failing.SetHouseholdPaused(id,host,true));Assert.Equal(SessionFailure.StorageUnavailable,failure.Failure);
   if(!commitFirst)Assert.Equal(before,durable.Read(id));
   var retry=new SessionCoordinator(new Notifications(),new DryRacePresets(),boundary);
   await retry.SetHouseholdPaused(id,host,true);await retry.SetHouseholdPaused(id,host,true);
   Assert.True(retry.Public(id).Paused);Assert.Equal(expected,JsonSerializer.Serialize(retry.FrozenPreparation(id,host),BrowserReplay.JsonOptions));Assert.True(BrowserReplay.Verify(retry.DesignerRecord(id)).Matches);
   await retry.SetHouseholdPaused(id,host,false);
  }
  var stale=durable.Read(id)!;var currentVersion=durable.Write(id,stale.Version,stale.Payload,null);
  Assert.Throws<SessionFault>(()=>durable.Write(id,stale.Version,stale.Payload,null));Assert.Equal(currentVersion,durable.Read(id)!.Version);
  var good=durable.Read(id)!;
  foreach(var corruption in new[]{"format","length","oversize","digest","data","truncated","trailing"}){
   var damaged=JsonNode.Parse(good.Payload)!;
   switch(corruption){
    case "format":damaged["storageFormat"]="future-session-v999";break;
    case "length":damaged["uncompressedBytes"]=1;break;
    case "oversize":damaged["uncompressedBytes"]=128*1024*1024+1;break;
    case "digest":damaged["sha256"]=new string('0',64);break;
    case "data":damaged["data"]="not base64";break;
    case "truncated":damaged["data"]=Convert.ToBase64String(Convert.FromBase64String(damaged["data"]!.GetValue<string>())[..^1]);break;
    case "trailing":damaged["data"]=Convert.ToBase64String([..Convert.FromBase64String(damaged["data"]!.GetValue<string>()),0]);break;
   }
   durable.Write(id,durable.Read(id)!.Version,damaged.ToJsonString(),null);var before=durable.Read(id)!;
   var rejected=new SessionCoordinator(new Notifications(),new DryRacePresets(),durable);
   var fault=Assert.Throws<SessionFault>(()=>rejected.FrozenPreparation(id,host));Assert.Equal(SessionFailure.Conflict,fault.Failure);Assert.Contains("Preserve",fault.Message);Assert.Equal(before,durable.Read(id));
   durable.Write(id,before.Version,good.Payload,null);
  }
  // Existing small raw recovery graphs remain readable without a format rewrite.
  var legacy=memory.Read(first.SessionId)!;Assert.DoesNotContain("storageFormat",legacy.Payload);
  durable.Write(first.SessionId,null,legacy.Payload,null);var legacySaved=durable.Read(first.SessionId)!;
  var legacyReader=new SessionCoordinator(new Notifications(),new DryRacePresets(),durable);
  Assert.Equal(created.Campaign.Teams.Count,legacyReader.LobbyPublic(first.SessionId).Teams.Count);
  Assert.Equal(legacySaved,durable.Read(first.SessionId));
 }
}



