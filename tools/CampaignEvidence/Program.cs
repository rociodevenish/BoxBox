using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Game.Application;
using Game.Domain;
using Game.Simulation;

var json=BrowserReplay.JsonOptions;
if(args.Length==2&&args[0]=="--replay")
{
 using var file=File.OpenRead(args[1]);using var gzip=new GZipStream(file,CompressionMode.Decompress);
 var archive=JsonSerializer.Deserialize<Archive>(gzip,json)??throw new Exception("Missing archive.");
 Verify(archive);Console.WriteLine("Verified two frozen weekends, raw results, awards, calendar cursor and both championship rebuilds without catalog access.");return;
}
if(args.Length!=1)throw new ArgumentException("Pass a new/empty output directory or --replay short-campaign.json.gz.");
if(Directory.Exists(args[0])&&Directory.EnumerateFileSystemEntries(args[0]).Any())throw new ArgumentException("Output must be new or empty.");
Directory.CreateDirectory(args[0]);
var clock=new Clock();var store=new MeteredStore();
SessionCoordinator Weekends()=>new(new Notifications(),new DryRacePresets(),store,clock);
var weekends=Weekends();var campaigns=new CampaignCoordinator(weekends,new CampaignStore(store));
var host=new AuthIdentity("synthetic-evidence-host",false);
var joined=campaigns.Create(host,new("70c17215-3fb3-4497-849b-08340bced525","Evidence host",Seed:123));
var id=joined.Campaign.CampaignId;var device=joined.Weekend.Membership.DeviceId;
var replays=new List<BrowserReplayRecord>();var tables=new List<ChampionshipTable>();
weekends.ClaimTeam(joined.Campaign.WeekendId,host,new("team-01",device));campaigns.Prepare(id,host);campaigns.PrepareAbsent(id,host,new("team-01"));campaigns.Start(id,host,new("eb954102-7c77-441d-837f-a9f4f51760e6"));
for(var index=0;index<2;index++)
{
 var current=campaigns.Snapshot(id,host,device);
 for(var step=0;weekends.CommittedCampaignResult(current.WeekendId) is null;step++)
 {
  if(step>=100)throw new Exception("Weekend exceeded bounded delivery count.");clock.Advance();await weekends.SweepWakeups();
 }
 replays.Add(weekends.DesignerRecord(current.WeekendId) with{Timing=null});
 var result=campaigns.CommitResult(id,host,device);tables.Add(result.Standings!);
 // Duplicate commit after reconstructed application coordinators uses saved authority.
 weekends=Weekends();campaigns=new(weekends,new CampaignStore(store));
 var retry=campaigns.CommitResult(id,host,device);
 Equal(result.Results,retry.Results,"Duplicate commit");
 if(index==0){campaigns.PrepareNext(id,host);campaigns.PrepareAbsent(id,host,new("team-01",true));var returned=campaigns.Return(id,host,new(true));device=returned.Membership.DeviceId;campaigns.StartNext(id,host,new("ea1ee8b1-d504-4f0e-bdb7-fbf06cc2a385"));}
}
var record=new CampaignStore(store).Read(id)!.Record;
var archiveCreated=new Archive("short-campaign-evidence-v1",record,replays.ToArray(),tables.ToArray());Verify(archiveCreated);
using(var file=File.Create(Path.Combine(args[0],"short-campaign.json.gz")))using(var gzip=new GZipStream(file,CompressionLevel.SmallestSize))JsonSerializer.Serialize(gzip,archiveCreated,json);
File.WriteAllText(Path.Combine(args[0],"usage.json"),JsonSerializer.Serialize(new{scope="One synthetic two-event campaign; volatile application store, including restart recovery and duplicate commits",store.ReadAttempts,store.WriteAttempts,store.SerializedWriteBytes,store.SerializedReadBytes,store.ListAttempts,store.PeakSerializedWriteBytes,chunking="unobserved; volatile adapter stores whole application payloads",actualFirestoreRpc="unobserved",billableCloudUnits="unobserved",hostedCost="unobserved; no deployment",householdUnderstanding="unmeasured; ready for human evaluation"},json));
Console.WriteLine($"Saved two complete 22-person/11-constructor events; {store.ReadAttempts} application reads, {store.WriteAttempts} writes, {store.SerializedWriteBytes} UTF-8 bytes written.");

void Equal<T>(T expected,T actual,string context){if(JsonSerializer.Serialize(expected,json)!=JsonSerializer.Serialize(actual,json))throw new Exception(context+" differs.");}
void Verify(Archive archive)
{
 if(archive.Schema!="short-campaign-evidence-v1")throw new Exception("Unknown archive schema.");
 var saved=archive.Campaign;CampaignSaveIntegrity.Validate(saved,saved.CampaignId);
 if(saved.Lifecycle!="season-complete"||saved.EventIndex!=1||saved.Calendar.Length!=2||archive.Replays.Length!=2||archive.Tables.Length!=2||saved.Contributions?.Length!=2)throw new Exception("Incomplete campaign.");
 var entrants=saved.Teams.SelectMany(t=>t.Drivers.Select(d=>(t.TeamId,d.PersonId))).Select((d,i)=>new ChampionshipEntrant(d.PersonId,d.TeamId,i)).ToArray();
 for(var i=0;i<2;i++)
 {
  var replay=archive.Replays[i];var verified=BrowserReplay.Verify(replay);
  if(!verified.Matches||!verified.Finished||replay.SessionId!=saved.Calendar[i].SessionId)throw new Exception("Weekend replay differs.");
  var contribution=saved.Contributions[i];CampaignResultIntegrity.Validate(saved);
  var engine=SimulationReplay.CreateEngine(replay.Replay);
  for(var operation=0;operation<replay.Phases.Count;operation++)
  {
   var plan=replay.Replay.Plan.Scheduled.Where(p=>p.Phase==operation).ToDictionary(p=>p.DriverId,p=>p.Decision);
   _=(replay.OperationKinds?[operation]??"Normal") switch
   {
    "Normal"=>engine.ResolvePhase(plan),"Weather"=>engine.ResolveWeatherReaction(plan),
    "SafetyCarStaging"=>engine.CommitSafetyCarChoices(plan.ToDictionary(p=>p.Key,p=>new SafetyCarDecision(p.Value.PitCompound))),
    "SafetyCar"=>engine.ResolveSafetyCar(plan.ToDictionary(p=>p.Key,p=>new SafetyCarDecision(p.Value.PitCompound))),
    _=>throw new Exception("Unknown operation.")
   };
  }
  var final=engine.GetState();var identities=saved.FrozenPresets[saved.Calendar[i].EventId].SelectMany(p=>p.Cars).ToDictionary(c=>c.EntryId);
  var raw=final.Classification.Select(r=>{var c=identities[r.DriverId];return new CommittedWeekendRow(r.DriverId,c.Driver.Id,c.TeamId,r.Position,r.Laps,r.Progress,r.GapSeconds,r.GapLaps,r.FinishTime,r.RetirementTime,final.Eligibility[r.DriverId],r.RetirementTime is not null?"retired":final.Eligibility[r.DriverId]?"finished":"ineligible");}).ToArray();
  Equal(contribution.Result.Classification,raw,"Raw result from replay");
  var awards=ChampionshipScoring.Score(saved.Points!,entrants,contribution.Result.Classification.Select(r=>new ChampionshipFinish(r.PersonId,r.Position,r.Eligible,r.Status=="retired")).ToArray());
  Equal(contribution.Awards,awards,"Scoring");Equal(archive.Tables[i],ChampionshipScoring.Rebuild(entrants,saved.Contributions.Take(i+1).Select(c=>(IReadOnlyList<ChampionshipAward>)c.Awards)),"Standings");
 }
}
record Archive(string Schema,CampaignRecord Campaign,BrowserReplayRecord[] Replays,ChampionshipTable[] Tables);
sealed class Clock:TimeProvider{private DateTimeOffset now=DateTimeOffset.Parse("2026-10-26T00:00:00Z");public override DateTimeOffset GetUtcNow()=>now;public void Advance()=>now=now.AddHours(1);}
sealed class Notifications:ISessionNotifications{public Task Publish(PublicSnapshot display,TeamSnapshot team)=>Task.CompletedTask;public Task PublishHousehold(PublicSnapshot display,IReadOnlyList<TeamSnapshot> teams,IReadOnlyList<OpenTeamSnapshot> openTeams)=>Task.CompletedTask;}
sealed class MeteredStore:ISessionStore
{
 private readonly VolatileSessionStore inner=new();public long ReadAttempts,WriteAttempts,SerializedWriteBytes,SerializedReadBytes,ListAttempts,PeakSerializedWriteBytes;
 public StoredSession? Read(string id){ReadAttempts++;var result=inner.Read(id);SerializedReadBytes+=result is null?0:Encoding.UTF8.GetByteCount(result.Payload);return result;}
 public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code);
 public IReadOnlyList<string> ListSessions(){ListAttempts++;return inner.ListSessions();}
 public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
 public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup){WriteAttempts++;var bytes=Encoding.UTF8.GetByteCount(payload);SerializedWriteBytes+=bytes;PeakSerializedWriteBytes=Math.Max(PeakSerializedWriteBytes,bytes);return inner.Write(id,version,payload,code,wakeup);}
}
