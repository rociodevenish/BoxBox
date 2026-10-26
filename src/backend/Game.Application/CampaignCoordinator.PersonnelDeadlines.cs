using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public sealed partial class CampaignCoordinator
{
 public CampaignSnapshot ActivatePersonnel(string id,AuthIdentity identity)
 {
  lock(gate)
  {
   var saved=RequireHost(id,identity);var record=saved.Record;
   if(record.Lifecycle!="acquiring")return Project(CompletePersonnel(saved).Record);
   if(!record.AcquisitionAutomatic){record=SchedulePersonnel(record with{AcquisitionAutomatic=true,Revision=record.Revision+1});Write(id,saved.Version,record);}
   return Project(record);
  }
 }
 private CampaignRecord SchedulePersonnel(CampaignRecord record)
 {
  if(!record.AcquisitionAutomatic)return record with{AcquisitionWakeup=null};
  if(record.Acquisition?.Offer is not {} offer)return record;
  var assigned=weekends.CampaignPersonnelAssigned(record.Calendar[0].SessionId,offer.TeamId);
  return record with{AcquisitionWakeup=new("campaign-"+record.CampaignId,Guid.NewGuid().ToString("N"),"personnel",record.Acquisition.Carousel!.Cursor,record.Acquisition.Revision,acquisitionClock.GetUtcNow().AddSeconds(assigned?120:1))};
 }
 public object DeliverPersonnelWakeup(WakeupInput input)
 {
  if(input is null||!input.SessionId.StartsWith("campaign-",StringComparison.Ordinal)||!Guid.TryParse(input.Generation,out _))throw new SessionFault(SessionFailure.InvalidInput,"Supply the saved campaign wakeup identity.");
  lock(gate)
  {
   var id=input.SessionId[9..];var saved=CompletePersonnel(Require(id));var record=saved.Record;var wakeup=record.AcquisitionWakeup;
   if(wakeup is null||wakeup.Generation!=input.Generation||wakeup.Phase!=input.Phase||wakeup.WindowRevision!=input.WindowRevision||acquisitionClock.GetUtcNow()<wakeup.DueUtc)return new{advanced=false};
   if(record.Lifecycle!="acquiring"||record.Acquisition?.Offer is not {} offer)return new{advanced=false};
   var observation=ObservePersonnel(record.Acquisition,offer.TeamId);var recommendation=PersonnelDecisionPolicy.Evaluate(observation);
   var command=new PersonnelHire(Guid.Parse(wakeup.Generation).ToString("D"),record.Acquisition.Revision,offer.TeamId,recommendation.PersonId);
   var next=PersonnelAcquisition.Hire(record.Acquisition,command);
   var source=weekends.CampaignPersonnelAssigned(record.Calendar[0].SessionId,offer.TeamId)?"deadline-absence-ai":"unclaimed-ai";
   record=SchedulePersonnel(record with{Acquisition=next,Revision=record.Revision+1,PersonnelChoices=[..record.PersonnelChoices??[],new(source,record.Acquisition.Revision,observation,recommendation,command.CommandId)]});
   Write(id,saved.Version,record);CompletePersonnel(Require(id));return new{advanced=true};
  }
 }
 public int SweepPersonnelWakeups()
 {
  var advanced=0;
  foreach(var id in store.ListScheduled())
  {
   lock(gate)
   {
    var saved=CompletePersonnel(Require(id));var wakeup=saved.Record.AcquisitionWakeup;if(wakeup is null)continue;
    try{scheduledWakeups?.Enqueue(wakeup);}catch(Exception){/* Next sweep retries the saved intent. */}
    if(acquisitionClock.GetUtcNow()>=wakeup.DueUtc){DeliverPersonnelWakeup(new(wakeup.SessionId,wakeup.Generation,wakeup.Phase,wakeup.WindowRevision));advanced++;}
   }
  }
  return advanced;
 }
}
public sealed partial class SessionCoordinator
{
 public bool CampaignPersonnelAssigned(string id,string teamId)
 {lock(gate){RefreshCampaignWeekend(id);return RequireLobby(id).Assignments.ContainsKey(teamId);}}
}


