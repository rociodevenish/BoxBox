using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using Game.Api;
using Game.Application;
using Game.Domain;
using Game.Simulation;
using FullReleaseEvidence;
using Archive=FullReleaseEvidence.Archive;

var json=BrowserReplay.JsonOptions;
if(args.Length<2)throw new ArgumentException("capture|resume|verify|bench OUTPUT [--version VERSION --source COMMIT --stop-after N --partial]");
var mode=args[0];var output=Path.GetFullPath(args[1]);
string? Option(string key){var i=Array.IndexOf(args,key);return i<0?null:i+1<args.Length?args[i+1]:throw new ArgumentException("Missing "+key);}
var partial=args.Contains("--partial");
T Read<T>(string path)=>JsonSerializer.Deserialize<T>(File.ReadAllText(path),json)??throw new InvalidDataException(path);
void Write<T>(string path,T value){Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path+".tmp",JsonSerializer.Serialize(value,json));File.Move(path+".tmp",path,true);}
Archive LoadArchive(){using var file=File.OpenRead(Path.Combine(output,"campaign.json.gz"));using var gzip=new GZipStream(file,CompressionMode.Decompress);return JsonSerializer.Deserialize<Archive>(gzip,json)??throw new InvalidDataException("Missing archive");}
void SaveArchive(Archive value){var name=Path.Combine(output,"campaign.json.gz");using(var file=File.Create(name+".tmp"))using(var gzip=new GZipStream(file,CompressionLevel.Optimal))JsonSerializer.Serialize(gzip,value,json);File.Move(name+".tmp",name,true);}
if(mode=="verify"){
 var timer=Stopwatch.StartNew();var archive=LoadArchive();FrozenVerification.Verify(archive,partial);
 foreach(var asset in archive.Assets??[]){var path=Path.Combine(output,"private","assets",asset.Sha256+".bin");var bytes=File.ReadAllBytes(path);if(bytes.LongLength!=asset.Bytes||Convert.ToHexString(SHA256.HashData(bytes))!=asset.Sha256)throw new Exception("Frozen asset bundle differs: "+asset.Id);}
 Write(Path.Combine(output,partial?"partial-verification.json":"verification.json"),new{schema="full-release-verification-v1",complete=!partial,archive.SourceCommit,archive.ContentVersion,events=archive.Weekends.Length,elapsedMilliseconds=timer.Elapsed.TotalMilliseconds,catalogAccess="none; frozen archive and versioned runtime only"});
 Console.WriteLine($"Verified {archive.Weekends.Length} frozen weekends; partial={partial}; no current catalog constructed.");return;
}
if(mode is not("capture" or "resume" or "bench"))throw new ArgumentException("Unknown mode");
var controlPath=Path.Combine(output,"private","control.json");Control control;PrivateCheckpoint? imported=null;
if(mode=="capture"){
 if(Directory.Exists(output)&&Directory.EnumerateFileSystemEntries(output).Any())throw new ArgumentException("Capture output must be new or empty; use resume for a checkpoint.");
 var source=Option("--source")??throw new ArgumentException("Explicit clean source commit required");if(source.Length!=40||!source.All(Uri.IsHexDigit))throw new ArgumentException("Use full source SHA");
 var version=Option("--version")??throw new ArgumentException("Explicit release version required");if(version!=ReleaseContent.CardVersion)throw new ArgumentException("Use final combined full-release cards version");
 if((Option("--stop-after")??"23")=="23"){
  string Git(params string[] values){var info=new ProcessStartInfo("git"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true};foreach(var value in values)info.ArgumentList.Add(value);using var process=Process.Start(info)!;var result=process.StandardOutput.ReadToEnd().Trim();process.WaitForExit();if(process.ExitCode!=0)throw new Exception("Cannot establish clean source pin");return result;}
  if(Git("rev-parse","HEAD")!=source||Git("status","--porcelain").Length!=0)throw new Exception("Full capture requires exact clean HEAD source pin; commit/merge the tool and final #24 source first");
 }
 control=new(){SourceCommit=source,ContentVersion=version,HostUid="evidence-"+Guid.NewGuid().ToString("N")};Directory.CreateDirectory(output);
}else if(mode=="resume"&&Option("--checkpoint") is {} checkpoint){
 if(!args.Contains("--restore-store"))throw new ArgumentException("A specific historical checkpoint requires --restore-store into an empty owned emulator");
 if(Directory.Exists(output)&&Directory.EnumerateFileSystemEntries(output).Any())throw new ArgumentException("Restored checkpoint output must be new/empty; existing output is preserved");
 var path=Path.GetFullPath(checkpoint);imported=Read<PrivateCheckpoint>(path);control=imported.Control;
 Directory.CreateDirectory(Path.Combine(output,"private","assets"));var oldPrivate=Path.GetDirectoryName(path)!;
 File.Copy(Path.Combine(oldPrivate,$"archive-{control.CheckpointSequence:D3}.json.gz"),Path.Combine(output,"campaign.json.gz"));
 foreach(var asset in control.Assets)File.Copy(Path.Combine(oldPrivate,"assets",asset.Sha256+".bin"),Path.Combine(output,"private","assets",asset.Sha256+".bin"),true);
}else control=Read<Control>(controlPath);
if(control.Schema!="full-release-private-checkpoint-v1")throw new InvalidDataException("Unsupported checkpoint");
void SaveControl()=>Write(controlPath,control);
SaveControl();var host=new AuthIdentity(control.HostUid,false);var clock=new Clock(control);
var endpoint=Environment.GetEnvironmentVariable("F1_RECOVERY_HOST")??throw new ArgumentException("Set the owned Firestore emulator endpoint F1_RECOVERY_HOST");
if(!endpoint.StartsWith("127.0.0.1:",StringComparison.Ordinal)&&!endpoint.StartsWith("localhost:",StringComparison.Ordinal))throw new ArgumentException("Evidence requires an owned loopback emulator, not cloud credentials");
using var firestore=new FirestoreSessionStore("demo-f1game",endpoint);
if(imported is not null){
 // All existence checks precede the first write. Never rewind a live saved campaign.
 foreach(var record in imported.Records)if(firestore.Read(record.Id) is not null)throw new Exception("Checkpoint import requires an empty owned target; existing saved data was preserved: "+record.Id);
 foreach(var record in imported.Records)firestore.Write(record.Id,null,record.Payload,null);
}
var store=new MeteredStore(firestore,control,SaveControl);var campaignStore=new CampaignStore(store);
SessionCoordinator weekends=null!;CampaignCoordinator campaigns=null!;
void Rebuild(){weekends=new(new Notifications(),new DryRacePresets(),store,clock);campaigns=new(weekends,campaignStore,clock,contentCatalog:new CampaignContentCatalog([]));}
Rebuild();
T Input<T>(string key,Func<T> create){if(control.Journal.TryGetValue(key,out var saved))return saved.Deserialize<T>(json)!;var value=create();control.Journal[key]=JsonSerializer.SerializeToElement(value,json);SaveControl();return value;}
string Command()=>Guid.NewGuid().ToString("D");
if(mode=="bench"){
 var times=new List<double>();CampaignRecord record=null!;
 for(var i=0;i<4;i++){var t=Stopwatch.StartNew();record=campaignStore.Read(control.CampaignId)!.Record;times.Add(t.Elapsed.TotalMilliseconds);}
 var sessionTimer=Stopwatch.StartNew();var preparation=weekends.FrozenPreparation(record.Calendar[record.EventIndex].SessionId,host);var coldWeekendRecoveryMilliseconds=sessionTimer.Elapsed.TotalMilliseconds;
 var replay=weekends.DesignerRecord(record.Calendar[record.EventIndex].SessionId);if(!BrowserReplay.Verify(replay).Matches)throw new Exception("Recovered weekend replay differs");
 var expected=JsonSerializer.Serialize(record,json);if(expected!=JsonSerializer.Serialize(LoadArchive().Campaign,json))throw new Exception("Durable checkpoint differs from saved frozen archive");var restored=campaigns.Snapshot(control.CampaignId,host,control.Device);
 if(restored.ContentVersion!=control.ContentVersion)throw new Exception("Content rebound on empty catalog restore");
 if(expected!=JsonSerializer.Serialize(campaignStore.Read(control.CampaignId)!.Record,json))throw new Exception("Catalog-absent read changed campaign");
 var other=new CampaignCoordinator(new SessionCoordinator(new Notifications(),new DryRacePresets(),store,clock),campaignStore,clock,contentCatalog:new CampaignContentCatalog([ReleaseContent.PersonnelCandidate()]));
 _=other.Snapshot(control.CampaignId,host,control.Device);
 if(expected!=JsonSerializer.Serialize(campaignStore.Read(control.CampaignId)!.Record,json))throw new Exception("Different-deployment catalog rebound inputs");
 var copy=control.CampaignId+"-incompatible-probe";var bad=record with{CampaignId=copy,ContentVersion="unsupported-evidence-v999",Input=record.Input with{ContentVersion="unsupported-evidence-v999"}};
 var existing=store.Read("campaign-"+copy);campaignStore.Write(copy,existing?.Version,bad);var before=store.Read("campaign-"+copy)!;
 try{campaignStore.Read(copy);throw new Exception("Unsupported content accepted");}catch(SessionFault fault)when(fault.Failure==SessionFailure.Conflict){}
 if(before!=store.Read("campaign-"+copy))throw new Exception("Rejected read overwrote save");
 var corrupted=JsonNode.Parse(before.Payload)!;corrupted["storageFormat"]="future-codec-v999";store.Write("campaign-"+copy,before.Version,corrupted.ToJsonString(),null);var damaged=store.Read("campaign-"+copy)!;
 try{campaignStore.Read(copy);throw new Exception("Corrupt envelope accepted");}catch(SessionFault fault)when(fault.Failure==SessionFailure.Conflict){}
 if(damaged!=store.Read("campaign-"+copy))throw new Exception("Corrupt read overwrote save");
 if(expected!=JsonSerializer.Serialize(campaignStore.Read(control.CampaignId)!.Record,json))throw new Exception("Original last-good save changed");
 foreach(var asset in control.Assets){var path=Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),"src","frontend","public",asset.Url.TrimStart('/')));if(!File.Exists(path)||Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))!=asset.Sha256)throw new Exception("Retained runtime asset incompatible: "+asset.Id);}
 Write(Path.Combine(output,"bench.json"),new{schema="full-release-recovery-bench-v1",coldTypedReadMilliseconds=times[0],coldWeekendRecoveryMilliseconds,warmTypedReadMilliseconds=times.Skip(1),catalogAbsent=true,differentDeploymentCatalog=true,retainedAssetCount=control.Assets.Length,unsupportedPreserved=true,corruptPreserved=true,originalPreserved=true,peakWorkingSet=Process.GetCurrentProcess().PeakWorkingSet64,store.Metrics,scope="Fresh-process durable application recovery; alternative deployment factory read only in update probe, never in frozen verify"});return;
}
var stop=int.Parse(Option("--stop-after")??"23");if(stop is <1 or >23)throw new ArgumentException("Stop bound is1..23");
var segment=Guid.NewGuid().ToString("N");var whole=Stopwatch.StartNew();var collected=mode=="resume"&&File.Exists(Path.Combine(output,"campaign.json.gz"))?LoadArchive().Weekends.ToList():new List<WeekendEvidence>();
async Task Pump(string sid){
 string? previous=null;var noProgress=0;
 for(var step=0;step<1024;step++){
  if(whole.Elapsed>TimeSpan.FromMinutes(45))throw new TimeoutException("45-minute capture guard");control.LifecycleActions++;
  if(weekends.HasQualifying(sid)){
   var q=weekends.QualifyingPublic(sid);var key=$"q/{q.Session}/{q.Lifecycle}/{q.Revision}/{q.WindowRevision}/{q.Paused}";
   noProgress=key==previous?noProgress+1:0;previous=key;if(noProgress>=3)throw new Exception("Qualifying no progress: "+key);
   if(q.Paused)await weekends.SetHouseholdPaused(sid,host,false);
   else if(q.Lifecycle=="session-result"&&q.Session!="Q3")await weekends.ContinueQualifying(sid,host,Input("continue/"+sid+"/"+key,()=>new DueTransitionInput(Command(),int.Parse(q.Session[1..]),q.WindowRevision)));
   else{clock.Advance(q.DecisionDeadlineUtc??q.PresentationDeadlineUtc);SaveControl();await weekends.SweepWakeups();}
  }else{
   var r=weekends.Public(sid);if(r.Lifecycle=="finished")return;
   var key=$"r/{r.Phase}/{r.Lifecycle}/{r.Revision}/{r.WindowRevision}/{r.Paused}";noProgress=key==previous?noProgress+1:0;previous=key;if(noProgress>=3)throw new Exception("Race no progress: "+key);
   if(r.Paused)await weekends.SetHouseholdPaused(sid,host,false);
   else if(r.Lifecycle=="phase-result")await weekends.ContinueHousehold(sid,host,Input("continue/"+sid+"/"+key,()=>new DueTransitionInput(Command(),r.Phase,r.WindowRevision)));
   else{clock.Advance(r.DecisionDeadlineUtc??r.PresentationDeadlineUtc);SaveControl();await weekends.SweepWakeups();}
  }
 }
 throw new Exception("1024 lifecycle actions exceeded for "+sid);
}
void Checkpoint(string stage){
 var record=campaignStore.Read(control.CampaignId)!.Record;var timer=Stopwatch.StartNew();var raw=JsonSerializer.SerializeToUtf8Bytes(record,json);var serialization=timer.Elapsed.TotalMilliseconds;
 SaveArchive(new("full-release-campaign-evidence-v1",control.SourceCommit,control.ContentVersion,record,collected.ToArray(),control.FrozenInputHash,control.Assets));
 control.CheckpointSequence++;SaveControl();File.Copy(Path.Combine(output,"campaign.json.gz"),Path.Combine(output,"private",$"archive-{control.CheckpointSequence:D3}.json.gz"),false);var records=control.Ids.Order().Select(id=>{var value=store.Read(id)??throw new Exception("Missing own durable record "+id);return new PrivateStoredRecord(id,value.Version,value.Payload);}).ToArray();
 Write(Path.Combine(output,"private",$"checkpoint-{control.CheckpointSequence:D3}-{stage}.json"),new PrivateCheckpoint(control,records));
 Write(Path.Combine(output,"progress.json"),new{schema="full-release-progress-v1",stage,events=collected.Count,record.Lifecycle,record.EventIndex,rawCampaignBytes=raw.Length,developmentHistoryBytes=JsonSerializer.SerializeToUtf8Bytes(record.DevelopmentChoices,json).Length,rawSerializationMilliseconds=serialization,choices=record.DevelopmentChoices?.Length??0,control.LifecycleActions,control.RaceOperations,elapsedMilliseconds=whole.Elapsed.TotalMilliseconds,store.Metrics,store.Lists,store.InjectedFailures,complete=false,actualTransport="Firestore emulator; logical adapter operations are not billing units",source=control.SourceCommit});
 Console.WriteLine($"Checkpoint {stage}: events={collected.Count}, campaignRaw={raw.Length}, elapsed={whole.Elapsed.TotalSeconds:F1}s");
}
try{
 if(string.IsNullOrEmpty(control.CampaignId)){
  var input=Input("create",()=>new CreateCampaignInput(Command(),"Full release evidence",Calendar:"full",Seed:123,Laps:6,ContentVersion:control.ContentVersion));
  var creation=new CampaignCoordinator(weekends,campaignStore,clock);var joined=creation.Create(host,input);control.CampaignId=joined.Campaign.CampaignId;control.Device=joined.Weekend.Membership.DeviceId;control.ActiveSession=joined.Campaign.WeekendId;SaveControl();
  if(joined.Campaign.Calendar.Count!=23||joined.Campaign.Teams.Count!=11)throw new Exception("Full release shape differs");
  var frozenRecord=campaignStore.Read(control.CampaignId)!.Record;control.FrozenInputHash=FrozenVerification.InputHash(frozenRecord);
  var publicRoot=Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),"src","frontend","public"));
  control.Assets=(frozenRecord.ContentSnapshot?.Assets??[]).Where(a=>a.RuntimeUrl is not null).Select(a=>{
   var path=Path.GetFullPath(Path.Combine(publicRoot,a.RuntimeUrl!.TrimStart('/').Replace('/',Path.DirectorySeparatorChar)));if(!path.StartsWith(publicRoot+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Asset escapes runtime root");
   var bytes=File.ReadAllBytes(path);var hash=Convert.ToHexString(SHA256.HashData(bytes));var destination=Path.Combine(output,"private","assets",hash+".bin");Directory.CreateDirectory(Path.GetDirectoryName(destination)!);if(!File.Exists(destination))File.WriteAllBytes(destination,bytes);return new FrozenAsset(a.Id,a.RuntimeUrl,hash,bytes.LongLength);
  }).ToArray();SaveControl();
  weekends.ClaimTeam(control.ActiveSession,host,new("team-01",control.Device));Checkpoint("created");
 }
 var initial=campaignStore.Read(control.CampaignId)!.Record;
 if(initial.StartCommandId is null&&initial.EventIndex==0){campaigns.Prepare(control.CampaignId,host);campaigns.PrepareAbsent(control.CampaignId,host,new("team-01"));campaigns.Start(control.CampaignId,host,Input("start",()=>new CampaignStartInput(Command())));}
 while(true){
  var record=campaignStore.Read(control.CampaignId)!.Record;var index=record.EventIndex;var sid=record.Calendar[index].SessionId;control.ActiveSession=sid;SaveControl();
  if(!(record.Contributions??[]).Any(c=>c.EventId==record.Calendar[index].EventId)){
   await Pump(sid);if(weekends.CommittedCampaignResult(sid) is null)throw new Exception("Finished public race lacks committed durable outcome");
  }
  var result=campaigns.CommitResult(control.CampaignId,host,control.Device);
  if(collected.Count<=index){
   var race=weekends.DesignerRecord(sid) with{Timing=null};if(race.Phases.Count>512)throw new Exception("512 race operation bound exceeded");control.RaceOperations+=race.Phases.Count;
   collected.Add(new(weekends.DesignerQualifyingRecord(sid),race,weekends.FrozenPreparation(sid,host),weekends.PreparationAutomaticRecords(sid),result.Standings!));
   Rebuild();var retry=campaigns.CommitResult(control.CampaignId,host,control.Device);if(JsonSerializer.Serialize(result.Results,json)!=JsonSerializer.Serialize(retry.Results,json))throw new Exception("Duplicate result changed contributions");Checkpoint("result-"+(index+1));
  }
  if(index==22||collected.Count>=stop)break;
  var next=campaigns.PrepareNext(control.CampaignId,host);var d=next.Development!;
  if(d.Status!="closed"){
   next=campaigns.DevelopUnclaimed(control.CampaignId,host,Input("auto/"+index,()=>new DevelopmentAutomaticInput(Command(),d.OpportunityId,d.Revision)));d=next.Development!;
   var close=Input("close/"+index,()=>new CloseDevelopmentInput(Command(),d.OpportunityId,d.Revision,true));
   var faultProbe=index is 10 or 19;if(faultProbe){store.Armed=true;store.CommitFirst=index==19;}
   try{campaigns.CloseDevelopment(control.CampaignId,host,close);}catch(SessionFault fault)when(faultProbe&&fault.Failure==SessionFailure.StorageUnavailable){Rebuild();campaigns.CloseDevelopment(control.CampaignId,host,close);}
   if(faultProbe){Rebuild();campaigns.CloseDevelopment(control.CampaignId,host,close);}
   Checkpoint("development-"+(index+1));
  }
  campaigns.PrepareAbsent(control.CampaignId,host,new("team-01",true));var returned=campaigns.Return(control.CampaignId,host,new(true,control.Device));control.Device=returned.Membership.DeviceId;SaveControl();
  campaigns.StartNext(control.CampaignId,host,Input("next/"+index,()=>new CampaignStartInput(Command())));control.ActiveSession=campaignStore.Read(control.CampaignId)!.Record.Calendar[index+1].SessionId;SaveControl();
 }
 Checkpoint("stopped");var complete=collected.Count==23;
 Write(Path.Combine(output,"usage",segment+".json"),new{schema="full-release-usage-segment-v1",segment,mode,store.Metrics,store.Lists,store.InjectedFailures,elapsedMilliseconds=whole.Elapsed.TotalMilliseconds});
 Write(Path.Combine(output,"capture.json"),new{schema="full-release-capture-v1",complete,events=collected.Count,control.ContentVersion,control.SourceCommit,elapsedMilliseconds=whole.Elapsed.TotalMilliseconds,store.Metrics,store.InjectedFailures,scope="Actual Firestore-backed preset campaign; metrics are this process segment (all segments retained in usage); final acceptance additionally requires independent verify and final #24 coverage"});
}catch(Exception error){try{if(!string.IsNullOrEmpty(control.CampaignId))Checkpoint("failed");}catch(Exception checkpointError){Write(Path.Combine(output,"checkpoint-failure.json"),new{error=checkpointError.ToString()});}Write(Path.Combine(output,"failure.json"),new{error=error.ToString(),elapsedMilliseconds=whole.Elapsed.TotalMilliseconds});throw;}
