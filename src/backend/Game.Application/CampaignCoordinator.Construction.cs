using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public sealed partial class CampaignCoordinator
{
 public ConstructionTeamSnapshot ConstructionTeam(string id,AuthIdentity identity,string deviceId,string teamId)
 {
  lock(gate)
  {
   var record=CompletePersonnel(Require(id)).Record;
   weekends.AuthoriseCampaignPersonnel(record.Calendar[0].SessionId,identity,deviceId,teamId);
   return ProjectConstruction(record,teamId);
  }
 }
 public ConstructionTeamSnapshot EditConstruction(string id,AuthIdentity identity,string deviceId,string teamId,ConstructionEdit command)
 {
  lock(gate)
  {
   var saved=CompletePersonnel(Require(id));var record=saved.Record;
   weekends.AuthoriseCampaignPersonnel(record.Calendar[0].SessionId,identity,deviceId,teamId);
   if(record.Lifecycle!="constructing"||record.Construction is null||record.Acquisition is null)throw new SessionFault(SessionFailure.Conflict,"Base Car draft editing is not open.");
   if(command is null||command.TeamId!=teamId)throw new SessionFault(SessionFailure.InvalidInput,"Choose the authorised construction team.");
   ConstructionState next;
   try{next=CarConstruction.Edit(record.Construction,command,PersonnelAcquisition.Balance(record.Acquisition,teamId));}
   catch(ArgumentException error){throw new SessionFault(SessionFailure.InvalidInput,error.Message);}
   catch(InvalidOperationException error){throw new SessionFault(SessionFailure.Conflict,error.Message);}
   if(!ReferenceEquals(next,record.Construction)){record=record with{Construction=next,Revision=record.Revision+1};Write(id,saved.Version,record);}
   return ProjectConstruction(record,teamId);
  }
 }
 private static ConstructionTeamSnapshot ProjectConstruction(CampaignRecord record,string teamId)
 {
  var state=record.Construction??throw new SessionFault(SessionFailure.Conflict,"Complete personnel acquisition before construction previews.");
  if(!state.Drafts.TryGetValue(teamId,out var draft))throw new SessionFault(SessionFailure.InvalidInput,"Choose a campaign construction team.");
  var preset=record.FrozenPresets[record.Calendar[0].EventId].Single(p=>p.Cars[0].TeamId==teamId);
  return new(record.CampaignId,teamId,state.Catalogue,draft,ConstructionPreview.Preview(state.Catalogue,draft.OptionIds,preset,PersonnelAcquisition.Balance(record.Acquisition!,teamId)));
 }
}
