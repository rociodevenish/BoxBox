using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public sealed partial class CampaignCoordinator
{
 private DevelopmentPolicyObservation ObserveDevelopment(CampaignRecord record,string teamId)
 {
  var opportunity=CurrentDevelopment(record)!;var target=Array.FindIndex(record.Calendar,e=>e.EventId==opportunity.TargetEventId);
  var preset=record.FrozenPresets[opportunity.TargetEventId].Single(p=>p.Cars[0].TeamId==teamId);var demand=preset.Circuit.Demands;
  var relevance=new Dictionary<string,double>{{"speed",(demand["topSpeed"]+demand["accelerationDeployment"])/2},{"handling",demand["mechanicalGrip"]},{"tyres",demand["tyreDegradation"]},{"reliability",50}}.ToImmutableDictionary();
  var modifiers=(record.Development!.Modifiers.IsDefault?[]:record.Development.Modifiers).Where(m=>m.TeamId==teamId).ToArray();
  var current=DevelopedCar.Apply(preset,modifiers).Cars[0].Car.Ratings;
  var capacity=PermanentDevelopment.Options.ToImmutableDictionary(o=>o.Id,o=>Math.Max(0,Math.Min(100-current[o.Attribute],o.StackingCap-modifiers.Sum(m=>Math.Max(0,m.Changes.GetValueOrDefault(o.Attribute))))));
  return new(CampaignEconomy.Ledger(record).Where(e=>e.TeamId==teamId).Sum(e=>e.Credits),record.Calendar.Length-target-1,DevelopmentQuotes(record,teamId).ToImmutableArray(),relevance,capacity);
 }
 public DevelopmentTeamSnapshot RecommendDevelopment(string id,AuthIdentity identity,string deviceId,string teamId,DevelopmentRecommendationInput input)
 {
  lock(gate)
  {
   var saved=Require(id);var record=saved.Record;var opportunity=AuthoriseDevelopment(record,identity,deviceId,teamId);
   if(input is null||!Guid.TryParse(input.CommandId,out var key)||key==Guid.Empty)throw new SessionFault(SessionFailure.InvalidInput,"Supply a recommendation command identity.");
   if(weekends.CampaignDevelopmentHelp(record.Calendar[(record.PreparationEventIndex??record.EventIndex)].SessionId,teamId).Settings.HelpLevel=="Standard")throw new SessionFault(SessionFailure.Forbidden,"Standard Help Privileges do not include strategic recommendations.");
   var commandId=key.ToString("D");var prior=(record.DevelopmentChoices??[]).SingleOrDefault(c=>c.DraftCommandId==commandId);
   if(prior is not null){if(prior.Source!="assisted"||prior.TeamId!=teamId||prior.OpportunityId!=input.OpportunityId||prior.Revision!=input.Revision||opportunity.Receipts.Single(r=>r.CommandId==commandId).Fingerprint!=JsonSerializer.Serialize(new{team=teamId,command=new DevelopmentEdit(commandId,input.OpportunityId,input.Revision,prior.Recommendation.ActionKind,prior.Recommendation.OptionId,prior.Recommendation.QuoteId,input.AssignmentRevision)}))throw new SessionFault(SessionFailure.Conflict,"Recommendation identity belongs to different inputs.");return ProjectDevelopment(record,teamId,weekends.DevelopmentAssignmentRevision(record.Calendar[(record.PreparationEventIndex??record.EventIndex)].SessionId,identity,deviceId,teamId),identity);}
   if((record.DevelopmentAutomaticCommands??[]).Any(c=>c.CommandId==commandId)||(record.DevelopmentCloses??[]).Any(c=>c.CommandId==commandId))throw new SessionFault(SessionFailure.Conflict,"Command identity already belongs to another operation.");
   var assignment=weekends.DevelopmentAssignmentRevision(record.Calendar[(record.PreparationEventIndex??record.EventIndex)].SessionId,identity,deviceId,teamId);
   if(input.AssignmentRevision!=assignment)throw new SessionFault(SessionFailure.Conflict,"Controller changed. Refresh development.");
   var observation=ObserveDevelopment(record,teamId);var plan=DevelopmentDecisionPolicy.Evaluate(observation);
   DevelopmentOpportunity next;
   try{next=PermanentDevelopment.Edit(opportunity,teamId,new(commandId,input.OpportunityId,input.Revision,plan.ActionKind,plan.OptionId,plan.QuoteId,assignment),DevelopmentStaff(record,teamId,plan.OptionId),developmentControls,NewRisk);}
   catch(ArgumentException error){throw new SessionFault(SessionFailure.InvalidInput,error.Message);}
   catch(InvalidOperationException error){throw new SessionFault(SessionFailure.Conflict,error.Message);}
   if(ReferenceEquals(next,opportunity))throw new SessionFault(SessionFailure.Conflict,"Command identity already belongs to another operation.");
   record=WithDevelopment(record,next) with{DevelopmentChoices=[..record.DevelopmentChoices??[],new(teamId,opportunity.Id,"assisted",input.Revision,observation,plan,commandId)]};Write(id,saved.Version,record);
   return ProjectDevelopment(record,teamId,assignment,identity);
  }
 }
 public CampaignSnapshot DevelopUnclaimed(string id,AuthIdentity identity,DevelopmentAutomaticInput input)
 {
  lock(gate)
  {
   var saved=RequireHost(id,identity);var record=saved.Record;
   if(input is null||!Guid.TryParse(input.CommandId,out var key)||key==Guid.Empty)throw new SessionFault(SessionFailure.InvalidInput,"Supply an automatic development identity.");
   var commandId=key.ToString("D");var prior=(record.DevelopmentAutomaticCommands??[]).SingleOrDefault(c=>c.CommandId==commandId);
   if(prior is not null){if(prior.OpportunityId!=input.OpportunityId||prior.Revision!=input.Revision)throw new SessionFault(SessionFailure.Conflict,"Automatic command belongs to different inputs.");return Project(record);}
   var opportunity=CurrentDevelopment(record)??throw new SessionFault(SessionFailure.Conflict,"Open development first.");
   if(opportunity.Closed||opportunity.Id!=input.OpportunityId||opportunity.Revision!=input.Revision)throw new SessionFault(SessionFailure.Conflict,"Development changed. Refresh before automatic choices.");
   if((record.DevelopmentCloses??[]).Any(c=>c.CommandId==commandId)||(record.DevelopmentChoices??[]).Any(c=>c.DraftCommandId==commandId||c.CommitCommandId==commandId)||opportunity.Receipts.Any(r=>r.CommandId==commandId)||opportunity.Decisions.Any(d=>d.CommandId==commandId))throw new SessionFault(SessionFailure.Conflict,"Command identity already belongs to another operation.");
   var completed=new List<string>();var ledger=CampaignEconomy.Ledger(record);
   foreach(var team in record.Teams.OrderBy(t=>t.TeamId,StringComparer.Ordinal))
   {
    if(opportunity.Drafts[team.TeamId].Revision!=0||opportunity.Decisions.Any(d=>d.TeamId==team.TeamId)||weekends.CampaignPersonnelAssigned(record.Calendar[record.PreparationEventIndex!.Value].SessionId,team.TeamId))continue;
    var observation=ObserveDevelopment(record,team.TeamId);var plan=DevelopmentDecisionPolicy.Evaluate(observation);var revision=opportunity.Revision;
    string Command(string purpose)=>new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(commandId+"/"+team.TeamId+"/"+purpose))[..16]).ToString("D");
    var draft=Command("draft");var commit=Command("commit");
    opportunity=PermanentDevelopment.Edit(opportunity,team.TeamId,new(draft,opportunity.Id,revision,plan.ActionKind,plan.OptionId,plan.QuoteId),DevelopmentStaff(record,team.TeamId,plan.OptionId),developmentControls,NewRisk);
    var accepted=PermanentDevelopment.Commit(opportunity,ledger,team.TeamId,new(commit,opportunity.Id,opportunity.Revision,opportunity.Drafts[team.TeamId].Revision,plan.QuoteId),DevelopmentStaff(record,team.TeamId,plan.OptionId),"unclaimed-ai");
    opportunity=accepted.State;ledger=accepted.Ledger;
    record=record with{DevelopmentChoices=[..record.DevelopmentChoices??[],new(team.TeamId,opportunity.Id,"unclaimed-ai",revision,observation,plan,draft,commit)]};completed.Add(team.TeamId);
   }
   record=WithDevelopment(record,opportunity) with{DevelopmentAutomaticCommands=[..record.DevelopmentAutomaticCommands??[],new(commandId,input.OpportunityId,input.Revision,completed.ToArray())]};
   record=record.Acquisition is {} acquisition?record with{Acquisition=acquisition with{Ledger=ledger}}:record with{PresetLedger=ledger.ToArray()};Write(id,saved.Version,record);return Project(record);
  }
 }
}
