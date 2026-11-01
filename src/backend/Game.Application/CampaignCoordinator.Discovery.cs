using Game.Domain;
namespace Game.Application;
public sealed partial class CampaignCoordinator
{
 public IReadOnlyList<CampaignSummary> Discover(AuthIdentity identity)
 {
  lock(gate)
  {
   try
   {
    var summaries=new List<CampaignSummary>();
    foreach(var id in store.FindByMember(identity.Uid))
    {
     var saved=ReadOptional(id);if(saved is null)continue;var record=saved.Record;var current=record.Calendar[record.EventIndex];
     if(!weekends.IsCampaignMember(record.Calendar[0].SessionId,current.SessionId,identity))continue;
     var lobby=weekends.LobbyPublic(current.SessionId);
     summaries.Add(new(id,record.Input.Name,record.HostUid==identity.Uid?"host":"member",CampaignRules.Lifecycle(record.Lifecycle,lobby.Lifecycle),current.Order,current.TrackName));
    }
    return summaries.OrderBy(s=>s.CampaignId,StringComparer.Ordinal).ToArray();
   }
   catch(SessionFault){throw;}
   catch(Exception){throw new SessionFault(SessionFailure.StorageUnavailable,"Your campaigns are unavailable. Retry after storage recovers.");}
  }
 }
}

