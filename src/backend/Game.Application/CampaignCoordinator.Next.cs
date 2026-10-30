namespace Game.Application;
public sealed partial class CampaignCoordinator
{
 public CampaignSnapshot PrepareAbsent(string id,AuthIdentity identity,CampaignAbsentInput input)
 {
  lock(gate)
  {
   var record=RequireHost(id,identity).Record;
   if(record.Lifecycle=="season-complete")throw new SessionFault(SessionFailure.Conflict,"Season complete.");
   var index=input.NextEvent?record.PreparationEventIndex??throw new SessionFault(SessionFailure.Conflict,"Open next-event preparation first."):record.EventIndex;
   if(!record.Teams.Any(t=>t.TeamId==input.TeamId))throw new SessionFault(SessionFailure.InvalidInput,"Choose a team from this campaign.");
   weekends.PrepareAbsentCampaignTeam(record.Calendar[index].SessionId,identity,input.TeamId);
   return Project(record);
  }
 }
 public CampaignSnapshot PrepareNext(string id,AuthIdentity identity)
 {
  lock(gate)
  {
   var saved=CommitFinished(RequireHost(id,identity));var record=saved.Record;
   if(!(record.Contributions??[]).Any(c=>c.EventId==record.Calendar[record.EventIndex].EventId)||record.EventIndex+1>=record.Calendar.Length)
    throw new SessionFault(SessionFailure.Conflict,"Commit this event before preparing the next circuit.");
   if(record.PreparationEventIndex is null)
   {
    var development=record.Development; if(development is not null){var target=record.Calendar[record.EventIndex+1];development=development with{Opportunities=development.Opportunities.Add(Game.Domain.PermanentDevelopment.Open(record.CampaignId+"/development/"+target.EventId,record.Calendar[record.EventIndex].EventId,target.EventId,record.Teams.Select(t=>t.TeamId)))};}
    record=record with{PreparationEventIndex=record.EventIndex+1,Development=development,Revision=record.Revision+1};
    try{Write(id,saved.Version,record);}catch(SessionFault fault) when(fault.Failure==SessionFailure.Conflict){}
    record=RequireHost(id,identity).Record;
   }
   EnsureNext(record,identity);return Project(record);
  }
 }
 private void EnsureNext(CampaignRecord record,AuthIdentity identity)
 {
  if(record.PreparationEventIndex is not {} index)return;
  var next=record.Calendar[index];
  weekends.EnsureNextCampaignLobby(record.Calendar[index-1].SessionId,next.SessionId,identity,
   EventPresets(record,next.EventId),record.EventConditions[next.EventId],DevelopmentPending(record));
  if(DevelopmentPending(record))return;
  if(CurrentDevelopment(record) is {Closed:true})weekends.CommitCampaignDevelopment(next.SessionId,EventPresets(record,next.EventId));
  weekends.BeginCampaignPreparation(next.SessionId,identity,EventPresets(record,next.EventId));
 }
 public JoinedLobby Return(string id,AuthIdentity identity,CampaignReturnInput input)
 {
  lock(gate)
  {
   var record=Require(id).Record;
   var index=input.NextEvent?record.PreparationEventIndex??throw new SessionFault(SessionFailure.Conflict,"Next-event preparation has not opened."):record.EventIndex;
   return weekends.ReturnCampaignMember(record.Calendar[0].SessionId,record.Calendar[index].SessionId,identity,input.DeviceId);
  }
 }
 public CampaignSnapshot StartNext(string id,AuthIdentity identity,CampaignStartInput input)
 {
  if(input is null||!Guid.TryParse(input.CommandId,out var command)||command==Guid.Empty)throw new SessionFault(SessionFailure.InvalidInput,"Supply a start command identity.");
  lock(gate)
  {
   var saved=RequireHost(id,identity);var record=saved.Record;
   if(record.Lifecycle=="season-complete")throw new SessionFault(SessionFailure.Conflict,"Season complete. No further weekend can be opened.");
   if(record.PreparationEventIndex is {} index)
   {
    if(DevelopmentPending(record))throw new SessionFault(SessionFailure.Conflict,"Close development before preparing or starting the next weekend.");
    EnsureNext(record,identity);weekends.RefreshCampaignWeekend(record.Calendar[index].SessionId);var readiness=weekends.PreparationPublic(record.Calendar[index].SessionId);
    if(!readiness.Readiness.All(t=>t.FinalReady))throw new SessionFault(SessionFailure.Conflict,"Every assigned team must be final-ready before opening the next weekend.");
    record=record with{EventIndex=index,PreparationEventIndex=null,StartCommandId=null,AdvanceCommandId=command.ToString("D"),Lifecycle="event-preparation",Revision=record.Revision+1};
    try{Write(id,saved.Version,record);}catch(SessionFault fault) when(fault.Failure==SessionFailure.Conflict){record=RequireHost(id,identity).Record;if(record.EventIndex!=index)throw;}
   }
   else if(record.AdvanceCommandId is null)throw new SessionFault(SessionFailure.Conflict,"Open next-event preparation first.");
   for(var attempt=0;attempt<8;attempt++)
   {
    try{weekends.ActivateCampaignWeekend(record.Calendar[record.EventIndex].SessionId,identity);return Start(id,identity,new(record.AdvanceCommandId!));}
    catch(SessionFault fault) when(fault.Failure==SessionFailure.Conflict&&attempt<7){record=RequireHost(id,identity).Record;weekends.RefreshCampaignWeekend(record.Calendar[record.EventIndex].SessionId);}
   }
   throw new SessionFault(SessionFailure.Conflict,"Campaign changed concurrently. Refresh and retry.");
  }
 }
 private CampaignNextPreparation? NextProjection(CampaignRecord record)
 {
  if(record.PreparationEventIndex is not {} index)return null;
  var next=record.Calendar[index];
  // A reserved intent may outlive an interrupted creation. Host PrepareNext retries it.
  try{weekends.RefreshCampaignWeekend(next.SessionId);var ready=weekends.PreparationPublic(next.SessionId).Readiness;return new(next.EventId,next.SessionId,next.TrackName,ready,ready.All(t=>t.FinalReady));}
  catch(SessionFault fault) when(fault.Failure is SessionFailure.Unavailable or SessionFailure.Conflict){return new(next.EventId,next.SessionId,next.TrackName,[],false);}
 }
}
