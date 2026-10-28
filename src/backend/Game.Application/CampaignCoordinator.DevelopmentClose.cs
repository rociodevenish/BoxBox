using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public sealed partial class CampaignCoordinator
{
 private static WeekendPreset[] EventPresets(CampaignRecord record,string eventId)=>record.EffectiveEventPresets?.GetValueOrDefault(eventId)??record.FrozenPresets[eventId];
 public CampaignSnapshot CloseDevelopment(string id,AuthIdentity identity,CloseDevelopmentInput input)
 {
  if(input is null||!Guid.TryParse(input.CommandId,out var key)||key==Guid.Empty)throw new SessionFault(SessionFailure.InvalidInput,"Supply a close command identity.");
  lock(gate)
  {
   var saved=RequireHost(id,identity);var record=saved.Record;
   var fingerprint=JsonSerializer.Serialize(input with{CommandId=key.ToString("D")});
   var prior=(record.DevelopmentCloses??[]).SingleOrDefault(c=>c.CommandId==key.ToString("D"));
   if(prior is not null)
   {
    if(prior.Fingerprint!=fingerprint)throw new SessionFault(SessionFailure.Conflict,"Close identity belongs to different inputs.");
    EnsureNext(record,identity);return Project(record);
   }
   var opportunity=CurrentDevelopment(record)??throw new SessionFault(SessionFailure.Conflict,"No next-event development is open.");
   if((record.DevelopmentAutomaticCommands??[]).Any(c=>c.CommandId==key.ToString("D"))||record.Development!.Opportunities.Any(o=>o.Receipts.Any(r=>r.CommandId==key.ToString("D"))||o.Decisions.Any(d=>d.CommandId==key.ToString("D"))))throw new SessionFault(SessionFailure.Conflict,"Command identity already belongs to another operation.");
   if(opportunity.Closed||opportunity.Id!=input.OpportunityId||opportunity.Revision!=input.Revision)throw new SessionFault(SessionFailure.Conflict,"Development changed. Refresh before closing.");
   var missing=record.Teams.Where(t=>!opportunity.Decisions.Any(d=>d.TeamId==t.TeamId)).ToArray();
   if(missing.Length>0&&!input.ApplyMissingSave)throw new SessionFault(SessionFailure.Conflict,"Wait for every team or explicitly apply the disclosed missing-team Save policy.");
   var ledger=CampaignEconomy.Ledger(record);
   foreach(var team in missing)
   {
    string Command(string purpose)=>new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(opportunity.Id+"/"+team.TeamId+"/host-save/"+purpose))[..16]).ToString("D");
    opportunity=PermanentDevelopment.Edit(opportunity,team.TeamId,new(Command("draft"),opportunity.Id,opportunity.Revision,"Save",null,null),DevelopmentStaff(record,team.TeamId));
    opportunity=PermanentDevelopment.Commit(opportunity,ledger,team.TeamId,new(Command("commit"),opportunity.Id,opportunity.Revision,opportunity.Drafts[team.TeamId].Revision,null),DevelopmentStaff(record,team.TeamId),"host-absent-save").State;
   }
   record=WithDevelopment(record,opportunity);
   var developed=PermanentDevelopment.Close(record.Development!,opportunity.Id,record.Calendar.Select(e=>e.EventId).ToArray());
   var effective=record.EffectiveEventPresets is null?new Dictionary<string,WeekendPreset[]>():new(record.EffectiveEventPresets);
   effective.Add(opportunity.TargetEventId,record.FrozenPresets[opportunity.TargetEventId].Select(p=>DevelopedCar.Apply(p,developed.Modifiers)).ToArray());
   record=record with{Development=developed,EffectiveEventPresets=effective,DevelopmentCloses=[..record.DevelopmentCloses??[],new(key.ToString("D"),fingerprint,opportunity.Id)]};
   Write(id,saved.Version,record);
   // The saved outcome/snapshot owns recovery. A failed independent lobby write is repaired by the same command.
   EnsureNext(record,identity);return Project(record);
  }
 }
}
