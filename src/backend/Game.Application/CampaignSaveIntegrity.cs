using System.Text.Json;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;

/// <summary>Validates recorded formats and materialised inputs, never the mutable source catalogue.</summary>
public static class CampaignSaveIntegrity
{
 public static SessionFault Incompatible()=>new(SessionFailure.Conflict,"Campaign save is unsupported or damaged. No saved data was changed. Preserve this campaign and restore a compatible deployment or backup; do not recreate it over this identity.");
 public static CampaignVersions Versions(Dictionary<string,RaceConfiguration> conditions)=>new(CampaignRules.Version,ChampionshipScoring.Version,BrowserReplay.DisruptionFormatVersion,WeekendPreparation.ModelVersion,WeekendPreparation.PracticeModelVersion,conditions.ToDictionary(p=>p.Key,p=>new CampaignEventVersions(SimulationReplay.Model(p.Value),SimulationReplay.Randomness(p.Value))));
 public static void Validate(CampaignRecord record,string id)
 {
  try
  {
   if(record.Schema!=CampaignCoordinator.Schema||record.CampaignId!=id||string.IsNullOrWhiteSpace(record.HostUid)||record.Revision<1||record.Input is null||string.IsNullOrWhiteSpace(record.ContentVersion)||string.IsNullOrWhiteSpace(record.CalendarVersion))throw new ArgumentException();
   if(record.Calendar is null||record.Teams is null||record.FrozenPresets is null||record.EventConditions is null||record.Calendar.Length<2||record.Teams.Length==0||record.EventIndex<0||record.EventIndex>=record.Calendar.Length)throw new ArgumentException();
   if(record.Lifecycle is not("created" or "event-preparation" or "active-weekend" or "event-result-committed" or "season-complete"))throw new ArgumentException();
   CampaignRules.ValidateShortCalendar(record.Calendar.Select(e=>e.TrackId).ToArray());
   if(record.Calendar.Select(e=>e.EventId).Distinct().Count()!=record.Calendar.Length||record.Calendar.Select(e=>e.SessionId).Distinct().Count()!=record.Calendar.Length||record.Teams.Select(t=>t.TeamId).Distinct().Count()!=record.Teams.Length)throw new ArgumentException();
   var drivers=record.Teams.SelectMany(t=>t.Drivers).ToArray();
   if(drivers.Length!=record.Teams.Length*2||drivers.Select(d=>d.PersonId).Distinct().Count()!=drivers.Length||drivers.Select(d=>d.EntryId).Distinct().Count()!=drivers.Length)throw new ArgumentException();
   // Version-less v1 records remain supported with the v1 contracts used to write them.
   var expected=Versions(record.EventConditions);var versions=record.Versions??expected;
   if(versions.Lifecycle!=expected.Lifecycle||versions.Scoring!=expected.Scoring||versions.Replay!=expected.Replay||versions.Preparation!=expected.Preparation||versions.Practice!=expected.Practice||versions.Events.Count!=record.Calendar.Length)throw new ArgumentException();
   foreach(var (e,index) in record.Calendar.Select((e,index)=>(e,index)))
   {
    if(e.Order!=index+1||e.EventId!=$"{id}-event-{index+1:00}"||e.SessionId!=$"{e.EventId}-weekend"||string.IsNullOrWhiteSpace(e.TrackVersion)||versions.Events[e.EventId]!=expected.Events[e.EventId])throw new ArgumentException();
    var presets=record.FrozenPresets[e.EventId];var conditions=record.EventConditions[e.EventId];
    if(presets.Length!=record.Teams.Length||conditions.Laps!=record.Input.Laps)throw new ArgumentException();
    foreach(var (team,teamIndex) in record.Teams.Select((t,i)=>(t,i)))
    {
     var preset=presets[teamIndex];
     if(preset.Circuit.Id!=e.TrackId||preset.Circuit.Version!=e.TrackVersion||preset.Cars.Length!=2||preset.Cars.Any(c=>c.TeamId!=team.TeamId||!team.Drivers.Any(d=>d.EntryId==c.EntryId&&d.PersonId==c.Driver.Id&&d.SourceVersion==c.Driver.Version)))throw new ArgumentException();
     _=WeekendPreparation.Prepare(preset,record.Input.Laps,new());
    }
   }
   var contributions=record.Contributions??[];
   if(contributions.Length>record.EventIndex+1||contributions.Where((c,i)=>c.EventId!=record.Calendar[i].EventId).Any()||record.PreparationEventIndex is {} next&&(next!=record.EventIndex+1||next>=record.Calendar.Length||contributions.Length!=record.EventIndex+1))throw new ArgumentException();
   if(record.Lifecycle=="season-complete"&&(contributions.Length!=record.Calendar.Length||record.EventIndex!=record.Calendar.Length-1||record.PreparationEventIndex is not null))throw new ArgumentException();
   CampaignResultIntegrity.Validate(record);
  }
  catch(Exception error) when(error is ArgumentException or InvalidOperationException or NullReferenceException or KeyNotFoundException or IndexOutOfRangeException or JsonException){throw Incompatible();}
 }
}
