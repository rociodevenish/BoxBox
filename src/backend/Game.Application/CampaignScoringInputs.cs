using Game.Domain;
namespace Game.Application;

// Keep write-time scoring and recovered contribution validation on the same frozen mapping.
internal static class CampaignScoringInputs
{
 internal static ChampionshipEntrant[] Entrants(CampaignRecord record)=>record.Teams.SelectMany(t=>t.Drivers.Select(d=>(t.TeamId,d.PersonId))).Select((d,i)=>new ChampionshipEntrant(d.PersonId,d.TeamId,i)).ToArray();
 internal static ChampionshipFinish[] Finishes(CommittedWeekendResult result)=>result.Classification.Select(r=>new ChampionshipFinish(r.PersonId,r.Position,r.Eligible,r.Status=="retired")).ToArray();
}
