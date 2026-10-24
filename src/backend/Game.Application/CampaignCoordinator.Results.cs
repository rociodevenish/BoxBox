using Game.Domain;
namespace Game.Application;
public sealed partial class CampaignCoordinator
{
 private static ChampionshipEntrant[] Entrants(CampaignRecord record)=>record.Teams.SelectMany(t=>t.Drivers.Select(d=>(t.TeamId,d.PersonId))).Select((d,i)=>new ChampionshipEntrant(d.PersonId,d.TeamId,i)).ToArray();
 public CampaignSnapshot CommitResult(string id,AuthIdentity identity,string deviceId)
 {
  lock(gate)
  {
   var saved=Require(id);_=weekends.LobbyMembership(saved.Record.Calendar[saved.Record.EventIndex].SessionId,identity,deviceId);
   return Project(CommitFinished(saved).Record);
  }
 }
 private StoredCampaign CommitFinished(StoredCampaign saved)
 {
  for(var attempt=0;attempt<8;attempt++)
  {
   var record=saved.Record;var current=record.Calendar[record.EventIndex];
   if((record.Contributions??[]).Any(c=>c.EventId==current.EventId))return saved;
   var result=weekends.CommittedCampaignResult(current.SessionId);if(result is null)return saved;
   var entrants=Entrants(record);
   if(result.Classification.Length!=entrants.Length||result.Classification.Any(r=>!record.Teams.Any(t=>t.TeamId==r.TeamId&&t.Drivers.Any(d=>d.PersonId==r.PersonId&&d.EntryId==r.EntryId))))throw new SessionFault(SessionFailure.Conflict,"Weekend result does not match the frozen campaign roster.");
   var points=record.Points??ChampionshipScoring.Default;
   var awards=ChampionshipScoring.Score(points,entrants,result.Classification.Select(r=>new ChampionshipFinish(r.PersonId,r.Position,r.Eligible,r.Status=="retired")).ToArray());
   var contribution=new CampaignEventContribution(current.EventId,result,ChampionshipScoring.Version,points.Version,awards);
   var next=record with{Contributions=[..record.Contributions??[],contribution],Lifecycle="event-result-committed",Revision=record.Revision+1};
   try{Write(record.CampaignId,saved.Version,next);return Require(record.CampaignId);}
   catch(SessionFault fault) when(fault.Failure is SessionFailure.Conflict or SessionFailure.StorageUnavailable)
   {
    var refreshed=Require(record.CampaignId);
    if((refreshed.Record.Contributions??[]).Any(c=>c.EventId==current.EventId))return refreshed;
    if(fault.Failure==SessionFailure.StorageUnavailable)throw;
    saved=refreshed;
   }
  }
  throw new SessionFault(SessionFailure.Conflict,"Campaign changed concurrently. Refresh and retry.");
 }
}
