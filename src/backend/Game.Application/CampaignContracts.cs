using Game.Domain;
namespace Game.Application;

public record CreateCampaignInput(string CommandId,string Name,string Mode="Competitive",string Calendar="short",int Seed=123,int Laps=6,ChampionshipPoints? Points=null);
public record CampaignEvent(string EventId,int Order,string TrackId,string TrackName,string TrackSlug,string TrackVersion,string SessionId);
public record CampaignDriver(string PersonId,string Name,string Slug,string EntryId,string SourceVersion);
public record CampaignTeam(string TeamId,string ConstructorId,string ConstructorName,IReadOnlyList<CampaignDriver> Drivers);
public record CampaignSnapshot(string CampaignId,long Revision,string Lifecycle,string ContentVersion,string CalendarVersion,
    string Mode,IReadOnlyList<CampaignEvent> Calendar,IReadOnlyList<CampaignTeam> Teams,int EventIndex,string WeekendId,
    IReadOnlyList<PreparationReadiness> Readiness,bool CanStart,bool FullCalendarAvailable=false,ChampionshipPoints? Points=null,ChampionshipTable? Standings=null,IReadOnlyList<CampaignEventContribution>? Results=null);
public record CampaignJoined(CampaignSnapshot Campaign,JoinedLobby Weekend);
public record CampaignStartInput(string CommandId);

// Only typed application contracts cross this persistence boundary. The adapter owns serialization and namespacing.
public record CampaignRecord(string Schema,string CampaignId,string HostUid,string CreationFingerprint,CreateCampaignInput Input,
    long Revision,string Lifecycle,string ContentVersion,string CalendarVersion,CampaignEvent[] Calendar,CampaignTeam[] Teams,
    Dictionary<string,WeekendPreset[]> FrozenPresets,Dictionary<string,RaceConfiguration> EventConditions,int EventIndex=0,string? StartCommandId=null,ChampionshipPoints? Points=null,CampaignEventContribution[]? Contributions=null);
public record StoredCampaign(string Version,CampaignRecord Record);
public interface ICampaignStore
{
    StoredCampaign? Read(string id);
    string Write(string id,string? expectedVersion,CampaignRecord record);
}

public record CommittedWeekendRow(string EntryId,string PersonId,string TeamId,int Position,int Laps,double Progress,double GapSeconds,int GapLaps,double? FinishTime,double? RetirementTime,bool Eligible,string Status);
public record CommittedWeekendResult(string SessionId,string StorageVersion,long Revision,string ModelVersion,string RandomnessVersion,int RaceLaps,double CircuitLength,double? TargetDistance,CommittedWeekendRow[] Classification,string Hash);
public record CampaignEventContribution(string EventId,CommittedWeekendResult Result,string ScoringVersion,string PointsVersion,ChampionshipAward[] Awards);
