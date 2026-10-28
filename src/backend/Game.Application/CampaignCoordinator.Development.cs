using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public sealed partial class CampaignCoordinator
{
 private static DevelopmentOpportunity? CurrentDevelopment(CampaignRecord record)=>record.PreparationEventIndex is {} index?record.Development?.Opportunities.SingleOrDefault(o=>o.TargetEventId==record.Calendar[index].EventId):record.Development?.Opportunities.LastOrDefault(o=>o.TargetEventId==record.Calendar[record.EventIndex].EventId);
 private static bool DevelopmentPending(CampaignRecord record)=>CurrentDevelopment(record) is {Closed:false};
 private static DevelopmentPublicSnapshot? DevelopmentProjection(CampaignRecord record)
 {
  if(CurrentDevelopment(record) is not {} opportunity)return null;
  var ready=record.Teams.Select(t=>new DevelopmentReadiness(t.TeamId,opportunity.Decisions.Any(d=>d.TeamId==t.TeamId),opportunity.Decisions.SingleOrDefault(d=>d.TeamId==t.TeamId)?.Source??"awaiting-choice")).ToArray();
  return new(opportunity.Id,opportunity.FromEventId,opportunity.TargetEventId,opportunity.Closed?"closed":"open",ready,!opportunity.Closed&&ready.All(t=>t.Committed),opportunity.Revision);
 }
 private static DevelopmentPersonnel DevelopmentStaff(CampaignRecord record,string teamId,string? optionId=null)
 {
  if(record.Acquisition is not {} acquisition)return new("preset-development-neutral-v1",0,0,0,0);
  var people=acquisition.Hires.Where(h=>h.TeamId==teamId).Select(h=>acquisition.Pool.People.Single(p=>p.Id==h.PersonId)).ToArray();
  if(optionId is not null)return DevelopmentPersonnelMapping.Map(people,acquisition.Pool.Version,optionId);
  var manager=people.Single(p=>p.Role=="Manager");var technical=people.Where(p=>p.Role=="Technical Engineer").ToArray();
  return new(acquisition.Pool.Version+"/"+string.Join("/",people.Select(p=>p.Id).Order()),manager.Attributes!["Efficiency"],manager.Attributes["Development"],(int)technical.Average(p=>p.Attributes!["Development Efficiency"]),(int)technical.Average(p=>new[]{"Aerodynamics","Mechanical / Chassis","Power Systems","Reliability"}.Average(k=>p.Attributes![k])));
 }
 private DevelopmentOpportunity AuthoriseDevelopment(CampaignRecord record,AuthIdentity identity,string deviceId,string teamId)
 {
  var opportunity=CurrentDevelopment(record)??throw new SessionFault(SessionFailure.Conflict,"Open next-event development first.");
  var index=(record.PreparationEventIndex??record.EventIndex);
  weekends.AuthoriseCampaignPersonnel(record.Calendar[index].SessionId,identity,deviceId,teamId);
  return opportunity;
 }
 private DevelopmentTeamSnapshot ProjectDevelopment(CampaignRecord record,string teamId,long assignmentRevision)
 {
  var opportunity=CurrentDevelopment(record)??throw new SessionFault(SessionFailure.Conflict,"No development opportunity is open.");

  var projects=(record.Development!.Outcomes.IsDefault?[]:record.Development.Outcomes).Where(o=>o.TeamId==teamId).Select(o=>new DevelopmentProjectSummary(o.ProjectId,o.Kind,(record.Development.Modifiers.IsDefault?[]:record.Development.Modifiers).Any(m=>m.ProjectId==o.ProjectId)?"completed":"pending",o.CompletionEventId,o.CompletionEventId is null?"Delayed beyond the final preparation in this short season; no further payment or event is requested.":o.Kind=="delay"?"Delayed work completes at the following preparation close without another charge.":"Saved bounded outcome; completed gains apply to the next weekend.")).ToArray();
  return new(record.CampaignId,teamId,opportunity.Id,opportunity.Revision,assignmentRevision,CampaignEconomy.Ledger(record).Where(e=>e.TeamId==teamId).Sum(e=>e.Credits),DevelopmentQuotes(record,teamId),opportunity.Drafts[teamId],opportunity.Decisions.SingleOrDefault(d=>d.TeamId==teamId),Projects:projects,CurrentCar:EventPresets(record,record.Calendar[record.EventIndex].EventId).Single(p=>p.Cars[0].TeamId==teamId).Cars[0].Car.Ratings,NextCar:EventPresets(record,opportunity.TargetEventId).Single(p=>p.Cars[0].TeamId==teamId).Cars[0].Car.Ratings,Advice:opportunity.Closed||opportunity.Decisions.Any(d=>d.TeamId==teamId)?null:DevelopmentDecisionPolicy.Evaluate(ObserveDevelopment(record,teamId)),PolicyChoices:(record.DevelopmentChoices??[]).Where(c=>c.TeamId==teamId).ToArray(),CurrentEventId:record.Calendar[record.EventIndex].EventId,NextEventId:opportunity.TargetEventId);
 }
 public DevelopmentTeamSnapshot DevelopmentTeam(string id,AuthIdentity identity,string deviceId,string teamId)
 {
  lock(gate){var record=Require(id).Record;_=AuthoriseDevelopment(record,identity,deviceId,teamId);return ProjectDevelopment(record,teamId,weekends.DevelopmentAssignmentRevision(record.Calendar[(record.PreparationEventIndex??record.EventIndex)].SessionId,identity,deviceId,teamId));}
 }
 public DevelopmentTeamSnapshot EditDevelopment(string id,AuthIdentity identity,string deviceId,string teamId,DevelopmentEdit command)
 {
  lock(gate)
  {
   var saved=Require(id);var record=saved.Record;var opportunity=AuthoriseDevelopment(record,identity,deviceId,teamId);
   if(command is not null&&ReservedDevelopmentCommand(record,command.CommandId))throw new SessionFault(SessionFailure.Conflict,"Command identity already belongs to another operation.");
   if(command is null||!opportunity.Receipts.Any(r=>r.CommandId==command.CommandId)&&command.AssignmentRevision!=weekends.DevelopmentAssignmentRevision(record.Calendar[(record.PreparationEventIndex??record.EventIndex)].SessionId,identity,deviceId,teamId))throw new SessionFault(SessionFailure.Conflict,"Controller changed. Refresh development.");
   DevelopmentOpportunity next;
   try{next=PermanentDevelopment.Edit(opportunity,teamId,command,DevelopmentStaff(record,teamId,command.OptionId),developmentControls);}
   catch(ArgumentException error){throw new SessionFault(SessionFailure.InvalidInput,error.Message);}
   catch(InvalidOperationException error){throw new SessionFault(SessionFailure.Conflict,error.Message);}
   if(!ReferenceEquals(next,opportunity)){record=WithDevelopment(record,next);Write(id,saved.Version,record);}
   return ProjectDevelopment(record,teamId,weekends.DevelopmentAssignmentRevision(record.Calendar[(record.PreparationEventIndex??record.EventIndex)].SessionId,identity,deviceId,teamId));
  }
 }
 public DevelopmentTeamSnapshot CommitDevelopment(string id,AuthIdentity identity,string deviceId,string teamId,DevelopmentCommit command)
 {
  lock(gate)
  {
   var saved=Require(id);var record=saved.Record;var opportunity=AuthoriseDevelopment(record,identity,deviceId,teamId);
   if(command is not null&&ReservedDevelopmentCommand(record,command.CommandId))throw new SessionFault(SessionFailure.Conflict,"Command identity already belongs to another operation.");
   if(command is null||!opportunity.Decisions.Any(r=>r.CommandId==command.CommandId)&&command.AssignmentRevision!=weekends.DevelopmentAssignmentRevision(record.Calendar[(record.PreparationEventIndex??record.EventIndex)].SessionId,identity,deviceId,teamId))throw new SessionFault(SessionFailure.Conflict,"Controller changed. Refresh development.");
   DevelopmentCommitment next;
   try{next=PermanentDevelopment.Commit(opportunity,CampaignEconomy.Ledger(record),teamId,command,DevelopmentCommitStaff(record,teamId,opportunity.Drafts[teamId]));}
   catch(ArgumentException error){throw new SessionFault(SessionFailure.InvalidInput,error.Message);}
   catch(InvalidOperationException error){throw new SessionFault(SessionFailure.Conflict,error.Message);}
   if(!ReferenceEquals(next.State,opportunity))
   {
    record=WithDevelopment(record,next.State);
    record=record.Acquisition is {} acquisition?record with{Acquisition=acquisition with{Ledger=next.Ledger}}:record with{PresetLedger=next.Ledger.ToArray()};
    Write(id,saved.Version,record);
   }
   return ProjectDevelopment(record,teamId,weekends.DevelopmentAssignmentRevision(record.Calendar[(record.PreparationEventIndex??record.EventIndex)].SessionId,identity,deviceId,teamId));
  }
 }
 private static DevelopmentPersonnel DevelopmentCommitStaff(CampaignRecord record,string teamId,DevelopmentDraft draft)=>DevelopmentStaff(record,teamId,draft.Quote?.Personnel.RosterVersion.StartsWith(DevelopmentPersonnelMapping.Version+"/",StringComparison.Ordinal)==true?draft.OptionId:null);
 private DevelopmentQuote[] DevelopmentQuotes(CampaignRecord record,string teamId)=>PermanentDevelopment.Options.Select(o=>PermanentDevelopment.Quote(o.Id,DevelopmentStaff(record,teamId,o.Id),developmentControls)).ToArray();
 private static CampaignRecord WithDevelopment(CampaignRecord record,DevelopmentOpportunity opportunity)=>record with{Revision=record.Revision+1,Development=record.Development! with{Opportunities=record.Development.Opportunities.Select(o=>o.Id==opportunity.Id?opportunity:o).ToImmutableArray()}};
 private static bool ReservedDevelopmentCommand(CampaignRecord record,string id)=>Guid.TryParse(id,out var key)&&((record.DevelopmentAutomaticCommands??[]).Any(c=>c.CommandId==key.ToString("D"))||(record.DevelopmentCloses??[]).Any(c=>c.CommandId==key.ToString("D")));
}
public sealed partial class SessionCoordinator
{
 public long DevelopmentAssignmentRevision(string id,AuthIdentity identity,string deviceId,string teamId)
 {lock(gate){return AuthorisePreparationController(id,identity,deviceId,teamId).Assignment.Revision;}}
 public void CommitCampaignDevelopment(string sessionId,WeekendPreset[] frozen)
 {
  lock(gate)
  {
   RefreshCampaignWeekend(sessionId);var lobby=RequireLobby(sessionId);
   if(!lobby.ModuleState.ContainsKey("campaign-development-pending-v1")){if(System.Text.Json.JsonSerializer.Serialize(lobby.CampaignPresets)!=System.Text.Json.JsonSerializer.Serialize(frozen))throw new SessionFault(SessionFailure.Conflict,"This weekend already preserves different development inputs.");return;}
   if(lobby.Lifecycle!="lobby")throw new SessionFault(SessionFailure.Conflict,"Development handoff must precede preparation.");
   lobby.CampaignPresets=frozen;lobby.ModuleState.Remove("campaign-development-pending-v1");lobby.Revision++;Persist(sessionId);
  }
 }
}
