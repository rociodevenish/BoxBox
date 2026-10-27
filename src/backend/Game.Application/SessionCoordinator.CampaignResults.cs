using System.Text.Json;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;

public sealed partial class SessionCoordinator
{
 /// <summary>Reads only an actually committed finished campaign weekend; never accepts a client classification.</summary>
 public CommittedWeekendResult? CommittedCampaignResult(string sessionId)
 {
  lock(gate)
  {
   StoredSession? saved;
   try{saved=persistence.Read(sessionId);}catch(Exception){throw new SessionFault(SessionFailure.StorageUnavailable,"Weekend result storage unavailable.");}
   if(saved is null)return null;
   var record=JsonSerializer.Deserialize<RecoveryRecord>(saved.Payload,BrowserReplay.JsonOptions);
   var race=record?.Lobby?.Race;
   if(race is null||race.Lifecycle!="finished")return null;
   if(record!.Lobby!.CampaignPresets is not {} frozen||race.PendingPlan is not null||race.PendingCommand is not null)
    throw new SessionFault(SessionFailure.Conflict,"Weekend result is not a committed campaign result.");
   // Restore validates replay/model versions and every recorded outcome against the durable source.
   lobbies.Remove(sessionId);if(active?.Id==sessionId)active=null;
   Restore(sessionId,saved);
   var session=lobbies[sessionId].Race!;var result=session.Engine.GetState();
   if(!result.Finished)throw new SessionFault(SessionFailure.Conflict,"Saved result has not finished.");
   var identities=frozen.SelectMany(p=>p.Cars).ToDictionary(c=>c.EntryId,StringComparer.Ordinal);
   var rows=result.Classification.Select(row=>
   {
    var car=identities[row.DriverId];
    return new CommittedWeekendRow(row.DriverId,car.Driver.Id,car.TeamId,row.Position,row.Laps,row.Progress,row.GapSeconds,row.GapLaps,row.FinishTime,row.RetirementTime,result.Eligibility[row.DriverId],row.RetirementTime is not null?"retired":result.Eligibility[row.DriverId]?"finished":"ineligible");
   }).ToArray();
   var projection=new CommittedWeekendResult(sessionId,saved.Version,session.Revision,SimulationReplay.Model(session.Configuration),SimulationReplay.Randomness(session.Configuration),session.Configuration.Laps,session.Configuration.Track.Segments?.Sum(s=>s.Length)??0,result.NormalTargetDistance,rows,"");
   return projection with{Hash=CampaignResultIntegrity.Hash(projection)};
  }
 }
}

