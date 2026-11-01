using Game.Application;
using Game.Domain;
using Game.Simulation;
using System.Text.Json;
namespace FullReleaseEvidence;
public sealed record WeekendEvidence(QualifyingReplayRecord Qualifying,BrowserReplayRecord Race,FrozenPreparationContext Preparation,IReadOnlyList<PreparationAutomaticRecord> AutomaticPreparation,ChampionshipTable Standings);
public sealed record FrozenAsset(string Id,string Url,string Sha256,long Bytes);
public sealed record Archive(string Schema,string SourceCommit,string ContentVersion,CampaignRecord Campaign,WeekendEvidence[] Weekends,string FrozenInputHash="",FrozenAsset[]? Assets=null);
public sealed class Control
{
 public string Schema{get;set;}="full-release-private-checkpoint-v1";
 public string SourceCommit{get;set;}="";public string ContentVersion{get;set;}="";
 public string CampaignId{get;set;}="";public string HostUid{get;set;}="";public string Device{get;set;}="";
 public DateTimeOffset Utc{get;set;}=DateTimeOffset.Parse("2026-11-01T00:00:00Z");
 public string FrozenInputHash{get;set;}="";public FrozenAsset[] Assets{get;set;}=[];
 public string ActiveSession{get;set;}="";public HashSet<string> Ids{get;set;}=[];
 public Dictionary<string,JsonElement> Journal{get;set;}=[];public long LifecycleActions{get;set;}
 public long RaceOperations{get;set;}public int CheckpointSequence{get;set;}
}
public sealed record PrivateStoredRecord(string Id,string Version,string Payload);
public sealed record PrivateCheckpoint(Control Control,PrivateStoredRecord[] Records);
public sealed class Clock(Control control):TimeProvider
{
 public override DateTimeOffset GetUtcNow()=>control.Utc;
 public void Advance(DateTimeOffset? due){var next=due??control.Utc.AddHours(1);control.Utc=next>control.Utc?next.AddMilliseconds(1):control.Utc.AddSeconds(1);}
}
public sealed class Notifications:ISessionNotifications
{
 public Task Publish(PublicSnapshot display,TeamSnapshot team)=>Task.CompletedTask;
 public Task PublishHousehold(PublicSnapshot display,IReadOnlyList<TeamSnapshot> teams,IReadOnlyList<OpenTeamSnapshot> open)=>Task.CompletedTask;
}
