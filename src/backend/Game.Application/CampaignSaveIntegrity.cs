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
   if(record.Lifecycle is not("acquiring" or "constructing" or "created" or "event-preparation" or "active-weekend" or "event-result-committed" or "season-complete"))throw new ArgumentException();
   if(record.Acquisition is {} acquisition)PersonnelAcquisition.Validate(acquisition);
   if(record.Construction is {} construction)CarConstruction.Validate(construction,record.Teams.Select(t=>t.TeamId).ToArray());
   var confirmed=record.Construction?.Confirmed;
   var debited=record.Acquisition?.ConstructedCars;
   if((confirmed?.Count??0)!=(debited?.Count??0)||confirmed is not null&&confirmed.Any(p=>debited is null||!debited.TryGetValue(p.Key,out var car)||JsonSerializer.Serialize(p.Value)!=JsonSerializer.Serialize(car)))throw new ArgumentException();
   if(record.ConstructionRequired&&record.Lifecycle is not("acquiring" or "constructing")&&(confirmed?.Count??0)!=record.Teams.Length)throw new ArgumentException();
   if(record.ConstructionRequired&&(record.Acquisition is null||record.Lifecycle!="acquiring"&&record.Construction is null)||record.Lifecycle=="constructing"&&(!record.ConstructionRequired||record.Construction is null))throw new ArgumentException();
   if(record.AcquisitionWakeup is {} wakeup&&(record.Acquisition is null||!record.AcquisitionAutomatic||record.Lifecycle is not("acquiring" or "created")||wakeup.SessionId!="campaign-"+id||wakeup.Kind!="personnel"||!Guid.TryParse(wakeup.Generation,out _)))throw new ArgumentException();
   foreach(var choice in record.PersonnelChoices??[])
   {
    if(record.Acquisition is not {} personnel||choice.Source is not("unclaimed-ai" or "deadline-absence-ai")||choice.Recommendation.Version!=PersonnelDecisionPolicy.Version||PersonnelDecisionPolicy.Evaluate(choice.Observation)!=choice.Recommendation||!personnel.Commands.Any(c=>c.CommandId==choice.CommandId&&c.Fingerprint==$"hire/{choice.Revision}/{personnel.Hires.Single(h=>h.CommandId==choice.CommandId).TeamId}/{choice.Recommendation.PersonId}"))throw new ArgumentException();
   }
   foreach(var choice in record.ConstructionChoices??[])
   {
    if(record.Construction is not {} state||record.Acquisition is not {} personnel||choice.Source is not("assisted" or "unclaimed-ai")||!state.Drafts.TryGetValue(choice.TeamId,out var draft)||choice.Revision<1||choice.Revision>=draft.Revision||choice.Recommendation.Version!=ConstructionDecisionPolicy.Version||JsonSerializer.Serialize(choice.Observation.Catalogue)!=JsonSerializer.Serialize(state.Catalogue)||JsonSerializer.Serialize(ConstructionDecisionPolicy.Evaluate(choice.Observation))!=JsonSerializer.Serialize(choice.Recommendation))throw new ArgumentException();
    var ownDrivers=record.FrozenPresets[record.Calendar[0].EventId].Single(p=>p.Cars[0].TeamId==choice.TeamId).Cars.Select(c=>c.Driver).ToArray();
    if(JsonSerializer.Serialize(ownDrivers)!=JsonSerializer.Serialize(choice.Observation.Drivers)||choice.Observation.Balance!=personnel.Ledger.Where(e=>e.TeamId==choice.TeamId&&e.Reason is "starting-credit" or "personnel-hire" or "base-car-construction").Sum(e=>e.Credits)+(state.Confirmed?.GetValueOrDefault(choice.TeamId)?.Cost??0)||!draft.Commands.Any(c=>c.CommandId==choice.CommandId&&c.Fingerprint==$"{choice.Revision}/{state.Catalogue.Version}/{string.Join('/',choice.Recommendation.OptionIds)}"))throw new ArgumentException();
    if(choice.Source=="unclaimed-ai"&&(choice.Revision!=1||state.Confirmed?.GetValueOrDefault(choice.TeamId) is not {} automatic||!automatic.OptionIds.SequenceEqual(choice.Recommendation.OptionIds)))throw new ArgumentException();
   }
   var automaticCommands=record.ConstructionAutomaticCommands??[];
   if(automaticCommands.Select(c=>c.CommandId).Distinct().Count()!=automaticCommands.Length||(record.ConstructionChoices??[]).Select(c=>c.CommandId).Distinct().Count()!=(record.ConstructionChoices??[]).Length)throw new ArgumentException();
   foreach(var command in automaticCommands)
    if(!Guid.TryParse(command.CommandId,out var key)||key==Guid.Empty||command.CommandId!=key.ToString("D")||command.Version!=ConstructionDecisionPolicy.Version||command.TeamIds.Distinct().Count()!=command.TeamIds.Length||command.TeamIds.Any(team=>!(record.ConstructionChoices??[]).Any(c=>c.TeamId==team&&c.Source=="unclaimed-ai")))throw new ArgumentException();
   if(record.Input.PersonnelMode is not("preset" or "acquisition"))throw new ArgumentException();
   if(record.Input.PersonnelMode=="acquisition"&&record.Acquisition is null||record.Input.PersonnelMode=="preset"&&record.Acquisition is not null)throw new ArgumentException();
   CampaignDevelopmentIntegrity.Validate(record);
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
     if(confirmed?.GetValueOrDefault(team.TeamId) is {} car)
     {
      var materialised=ConstructedBaseCar.Apply(preset,car.Id,car.Catalogue.Version,car.Ratings,car.MechanicalAxis);
      if(JsonSerializer.Serialize(preset)!=JsonSerializer.Serialize(materialised))throw new ArgumentException();
     }
     else if(record.ConstructionRequired&&preset.BaseCarId!="neutral-base-v1")throw new ArgumentException();
     if(record.Acquisition is {} acquired&&record.Lifecycle!="acquiring")
     {
      if(!PersonnelAcquisition.RosterReady(acquired,team.TeamId))throw new ArgumentException();
      for(var slot=0;slot<2;slot++)
       if(preset.Cars[slot].Driver.Id!=acquired.Hires.Single(h=>h.TeamId==team.TeamId&&h.Slot==$"driver-{slot+1}").PersonId||preset.Cars[slot].Engineer?.Id!=acquired.Hires.Single(h=>h.TeamId==team.TeamId&&h.Slot==$"race-engineer-{slot+1}").PersonId)throw new ArgumentException();
     }
     if(preset.Circuit.Id!=e.TrackId||preset.Circuit.Version!=e.TrackVersion||preset.Cars.Length!=2||preset.Cars.Any(c=>c.TeamId!=team.TeamId||!team.Drivers.Any(d=>d.EntryId==c.EntryId&&d.PersonId==c.Driver.Id&&d.SourceVersion==c.Driver.Version)))throw new ArgumentException();
     _=WeekendPreparation.Prepare(preset,record.Input.Laps,new());
    }
   }
   var contributions=record.Contributions??[];
   if(contributions.Length>record.EventIndex+1||contributions.Where((c,i)=>c.EventId!=record.Calendar[i].EventId).Any()||record.PreparationEventIndex is {} next&&(next!=record.EventIndex+1||next>=record.Calendar.Length||contributions.Length!=record.EventIndex+1))throw new ArgumentException();
   if(record.Lifecycle=="season-complete"&&(contributions.Length!=record.Calendar.Length||record.EventIndex!=record.Calendar.Length-1||record.PreparationEventIndex is not null))throw new ArgumentException();
   CampaignResultIntegrity.Validate(record);
   CampaignEconomy.Validate(record);
  }
  catch(Exception error) when(error is ArgumentException or InvalidOperationException or NullReferenceException or KeyNotFoundException or IndexOutOfRangeException or JsonException or OverflowException){throw Incompatible();}
 }
}
