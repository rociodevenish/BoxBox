using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public sealed partial class CampaignCoordinator
{
 public AcquisitionTeamSnapshot AcquisitionTeam(string id,AuthIdentity identity,string deviceId,string teamId)
 {
  lock(gate)
  {
   var record=Require(id).Record;
   weekends.AuthoriseCampaignPersonnel(record.Calendar[record.EventIndex].SessionId,identity,deviceId,teamId);
   return ProjectAcquisition(record,teamId);
  }
 }
 public AcquisitionTeamSnapshot HirePersonnel(string id,AuthIdentity identity,string deviceId,string teamId,PersonnelHire command)
 {
  lock(gate)
  {
   var saved=Require(id);var record=saved.Record;
   weekends.AuthoriseCampaignPersonnel(record.Calendar[record.EventIndex].SessionId,identity,deviceId,teamId);
   if(command is null||command.TeamId!=teamId)throw new SessionFault(SessionFailure.InvalidInput,"Choose the authorised team.");
   if(record.Acquisition is null)throw new SessionFault(SessionFailure.Conflict,"Personnel acquisition is not open.");
   AcquisitionState next;
   try{next=PersonnelAcquisition.Hire(record.Acquisition,command);}
   catch(ArgumentException error){throw new SessionFault(SessionFailure.InvalidInput,error.Message);}
   catch(InvalidOperationException error){throw new SessionFault(SessionFailure.Conflict,error.Message);}
   if(!ReferenceEquals(next,record.Acquisition))
   {
    record=SchedulePersonnel(record with{Acquisition=next,Revision=record.Revision+1});
    Write(id,saved.Version,record);
   }
   record=CompletePersonnel(Require(id)).Record;
   return ProjectAcquisition(record,teamId);
  }
 }
 public AcquisitionTeamSnapshot ScoutPersonnel(string id,AuthIdentity identity,string deviceId,string teamId,PersonnelScout command)
 {
  lock(gate)
  {
   var saved=Require(id);var record=saved.Record;
   weekends.AuthoriseCampaignPersonnel(record.Calendar[record.EventIndex].SessionId,identity,deviceId,teamId);
   if(command is null||command.TeamId!=teamId)throw new SessionFault(SessionFailure.InvalidInput,"Choose the authorised team.");
   if(record.Acquisition is null)throw new SessionFault(SessionFailure.Conflict,"Personnel acquisition is not open.");
   AcquisitionState next;
   try{next=PersonnelAcquisition.Scout(record.Acquisition,command);}
   catch(ArgumentException error){throw new SessionFault(SessionFailure.InvalidInput,error.Message);}
   catch(InvalidOperationException error){throw new SessionFault(SessionFailure.Conflict,error.Message);}
   if(!ReferenceEquals(next,record.Acquisition))
   {
    record=SchedulePersonnel(record with{Acquisition=next,Revision=record.Revision+1});
    Write(id,saved.Version,record);
   }
   return ProjectAcquisition(record,teamId);
  }
 }
 private StoredCampaign CompletePersonnel(StoredCampaign saved)
 {
  var record=saved.Record;
  if(record.Acquisition is not {} acquisition||!acquisition.TeamIds.All(t=>PersonnelAcquisition.RosterReady(acquisition,t)))return saved;
  if(record.Lifecycle=="acquiring")
  {
   var frozen=record.FrozenPresets.ToDictionary(pair=>pair.Key,pair=>pair.Value.Select(p=>PersonnelWeekendMapping.Apply(acquisition,p.Cars[0].TeamId,p)).ToArray());
   var first=frozen[record.Calendar[0].EventId];
   var teams=record.Teams.Select((team,index)=>team with{Drivers=first[index].Cars.Select(car=>new CampaignDriver(car.Driver.Id,car.Driver.Name,car.Driver.Slug,car.EntryId,car.Driver.Version)).ToArray()}).ToArray();
   record=record with{FrozenPresets=frozen,Teams=teams,Lifecycle="created",Revision=record.Revision+1};
   Write(record.CampaignId,saved.Version,record);saved=Require(record.CampaignId);
  }
  // Campaign roster commit precedes the independently durable lobby update. Any read/retry repairs
  // an interrupted handoff; the lobby guard stays closed until its own roster save succeeds.
  weekends.CommitCampaignPersonnel(record.Calendar[0].SessionId,record.FrozenPresets[record.Calendar[0].EventId]);
  if(record.AcquisitionWakeup is not null)
  {
   record=record with{AcquisitionWakeup=null,Revision=record.Revision+1};Write(record.CampaignId,saved.Version,record);saved=Require(record.CampaignId);
  }
  return saved;
 }
 private static PersonnelPolicyObservation ObservePersonnel(AcquisitionState state,string teamId)=>new(state.Offer!.Slot,PersonnelAcquisition.Balance(state,teamId),PersonnelAcquisition.CompletionReserve(state,teamId),state.Offer.PersonIds.Select(id=>{var card=state.Pool.People.Single(p=>p.Id==id);return new PersonnelPolicyOffer(id,card.Name,card.Price,PersonnelAcquisition.HireUnavailable(state,teamId,id) is null,PersonnelAcquisition.ReserveAfterHire(state,teamId,id));}).ToImmutableArray());
 private static AcquisitionTeamSnapshot ProjectAcquisition(CampaignRecord record,string teamId)
 {
  var state=record.Acquisition??throw new SessionFault(SessionFailure.Conflict,"This campaign uses preset personnel.");
  if(!state.TeamIds.Contains(teamId))throw new SessionFault(SessionFailure.InvalidInput,"Choose a campaign team.");
  var ownOffer=state.Offer?.TeamId==teamId?state.Offer:null;
  return new(record.CampaignId,teamId,state.Revision,state.Pool.Version,state.Pool.PriceVersion,state.Pool.StartingCredit,PersonnelAcquisition.Balance(state,teamId),state.Pool.MinimumCarReserve,
   state.Offer?.TeamId,ownOffer?.Slot,ownOffer is null?[]:ownOffer.PersonIds.Select(id=>state.Pool.People.Single(p=>p.Id==id)).ToArray(),state.Hires.Where(h=>h.TeamId==teamId).ToArray(),state.Ledger.Where(e=>e.TeamId==teamId).ToArray(),PersonnelAcquisition.RequiredSlots,state.Hires.Where(h=>h.TeamId==teamId).Select(h=>state.Pool.People.Single(p=>p.Id==h.PersonId)).ToArray(),state.ScoutTokens![teamId],PersonnelAcquisition.ScoutUnavailable(state,teamId),state.Carousel!.Cursor,state.Carousel.Picks.Length,state.Draws.Length,state.Carousel.Version,state.ScoutAllocation!.Version,PersonnelAcquisition.CompletionReserve(state,teamId),PersonnelAcquisition.RemainingSlots(state,teamId),PersonnelAcquisition.RosterReady(state,teamId),ownOffer is null?[]:ownOffer.PersonIds.Select(id=>new PersonnelHireOption(id,PersonnelAcquisition.HireUnavailable(state,teamId,id) is null,PersonnelAcquisition.ReserveAfterHire(state,teamId,id),PersonnelAcquisition.HireUnavailable(state,teamId,id))).ToArray(),PersonnelAcquisition.ReserveVersion,state.Pool.ConstructionMapping!,ownOffer is null?null:PersonnelDecisionPolicy.Evaluate(ObservePersonnel(state,teamId)),(record.PersonnelChoices??[]).Where(c=>state.Hires.Any(h=>h.TeamId==teamId&&h.CommandId==c.CommandId)).ToArray());
 }
}
public sealed partial class SessionCoordinator
{
 public void AuthoriseCampaignPersonnel(string id,AuthIdentity identity,string deviceId,string teamId)
 {lock(gate){_=AuthorisePreparationController(id,identity,deviceId,teamId);}}
}
