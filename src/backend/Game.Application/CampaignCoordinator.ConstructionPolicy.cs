using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public sealed partial class CampaignCoordinator
{
 private static ConstructionPolicyObservation ObserveConstruction(CampaignRecord record,string teamId)=>new(record.Construction!.Catalogue,PersonnelAcquisition.Balance(record.Acquisition!,teamId),record.FrozenPresets[record.Calendar[0].EventId].Single(p=>p.Cars[0].TeamId==teamId).Cars.Select(c=>c.Driver).ToImmutableArray());
 public ConstructionTeamSnapshot RecommendConstruction(string id,AuthIdentity identity,string deviceId,string teamId,ConstructionRecommendationRequest input)
 {
  lock(gate)
  {
   var saved=Require(id);var record=saved.Record;
   weekends.AuthoriseCampaignPersonnel(record.Calendar[0].SessionId,identity,deviceId,teamId);
   if(input is null||input.TeamId!=teamId||!Guid.TryParse(input.CommandId,out var key)||key==Guid.Empty)throw new SessionFault(SessionFailure.InvalidInput,"Supply the authorised construction team and command identity.");
   var prior=(record.ConstructionChoices??[]).FirstOrDefault(c=>c.CommandId==key.ToString("D"));
   if(prior is not null)
   {
    if(prior.TeamId!=teamId||prior.Source!="assisted"||prior.Revision!=input.Revision||prior.Observation.Catalogue.Version!=input.CatalogueVersion)throw new SessionFault(SessionFailure.Conflict,"Policy command already belongs to different inputs.");
    return ProjectConstruction(record,teamId);
   }
   if(record.Lifecycle!="constructing"||record.Construction is null||record.Acquisition is null)throw new SessionFault(SessionFailure.Conflict,"Construction advice editing is closed.");
   var observation=ObserveConstruction(record,teamId);var plan=ConstructionDecisionPolicy.Evaluate(observation);
   ConstructionState next;
   try{next=CarConstruction.Edit(record.Construction,new(key.ToString("D"),input.Revision,teamId,input.CatalogueVersion,plan.OptionIds),observation.Balance);}
   catch(ArgumentException error){throw new SessionFault(SessionFailure.InvalidInput,error.Message);}
   catch(InvalidOperationException error){throw new SessionFault(SessionFailure.Conflict,error.Message);}
   // A command previously used for a manual edit cannot acquire a different policy receipt.
   if(ReferenceEquals(next,record.Construction))throw new SessionFault(SessionFailure.Conflict,"Command identity already belongs to another operation.");
   record=record with{Construction=next,ConstructionChoices=[..record.ConstructionChoices??[],new(teamId,"assisted",input.Revision,observation,plan,key.ToString("D"))],Revision=record.Revision+1};
   Write(id,saved.Version,record);return ProjectConstruction(record,teamId);
  }
 }
 public CampaignSnapshot BuildUnclaimedConstruction(string id,AuthIdentity identity,ConstructionAutomaticRequest input)
 {
  lock(gate)
  {
   var saved=RequireHost(id,identity);var record=saved.Record;
   if(input is null||!Guid.TryParse(input.CommandId,out var key)||key==Guid.Empty)throw new SessionFault(SessionFailure.InvalidInput,"Supply an automatic construction command identity.");
   var commandId=key.ToString("D");
   if((record.ConstructionAutomaticCommands??[]).Any(c=>c.CommandId==commandId))return Project(CompleteConstruction(saved).Record);
   if(record.Lifecycle!="constructing"||record.Construction is null||record.Acquisition is null)throw new SessionFault(SessionFailure.Conflict,"Automatic Base Car purchases are closed.");
   if((record.ConstructionChoices??[]).Any(c=>c.CommandId==commandId)||record.Construction.Drafts.Values.Any(d=>d.Commands.Any(c=>c.CommandId==commandId))||record.Acquisition.Commands.Any(c=>c.CommandId==commandId)||record.Construction.Confirmed?.Values.Any(c=>c.CommandId==commandId)==true)throw new SessionFault(SessionFailure.Conflict,"Command identity already belongs to another operation.");
   var built=new List<string>();
   foreach(var team in record.Teams.OrderBy(t=>t.TeamId,StringComparer.Ordinal))
   {
    var draft=record.Construction.Drafts[team.TeamId];
    if(record.Construction.Confirmed?.ContainsKey(team.TeamId)==true||draft.Revision!=1||weekends.CampaignPersonnelAssigned(record.Calendar[0].SessionId,team.TeamId))continue;
    var observation=ObserveConstruction(record,team.TeamId);var plan=ConstructionDecisionPolicy.Evaluate(observation);
    string Derived(string purpose)=>new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"{commandId}/{team.TeamId}/{purpose}"))[..16]).ToString("D");
    var edit=Derived("draft");var confirm=Derived("confirm");
    var next=CarConstruction.Edit(record.Construction,new(edit,draft.Revision,team.TeamId,record.Construction.Catalogue.Version,plan.OptionIds),observation.Balance);
    var bought=CarConstruction.Confirm(next,record.Acquisition,new(confirm,next.Drafts[team.TeamId].Revision,team.TeamId,next.Catalogue.Version));
    var car=bought.Construction.Confirmed![team.TeamId];
    record=record with{Construction=bought.Construction,Acquisition=bought.Acquisition,FrozenPresets=record.FrozenPresets.ToDictionary(p=>p.Key,p=>p.Value.Select(preset=>preset.Cars[0].TeamId==team.TeamId?ConstructedBaseCar.Apply(preset,car.Id,car.Catalogue.Version,car.Ratings,car.MechanicalAxis):preset).ToArray()),ConstructionChoices=[..record.ConstructionChoices??[],new(team.TeamId,"unclaimed-ai",draft.Revision,observation,plan,edit)]};
    built.Add(team.TeamId);
   }
   // One conditional write commits every policy observation, draft, exact existing-ledger debit and all event inputs.
   // There is no timer or partially saved per-team work: the same host command recovers before/after acknowledgement.
   record=record with{ConstructionAutomaticCommands=[..record.ConstructionAutomaticCommands??[],new(commandId,ConstructionDecisionPolicy.Version,built.ToArray())],Revision=record.Revision+1};
   Write(id,saved.Version,record);return Project(CompleteConstruction(Require(id)).Record);
  }
 }
}
