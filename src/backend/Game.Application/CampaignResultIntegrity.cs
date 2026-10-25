using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public static class CampaignResultIntegrity
{
 public static string Hash(CommittedWeekendResult result)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result with{Hash=""},BrowserReplay.JsonOptions))));
 public static void Validate(CampaignRecord record)
 {
  var contributions=record.Contributions??[];
  if(contributions.Select(c=>c.EventId).Distinct(StringComparer.Ordinal).Count()!=contributions.Length)throw new ArgumentException("Duplicate committed event.");
  var entrants=CampaignScoringInputs.Entrants(record);
  var points=record.Points??ChampionshipScoring.Default;
  foreach(var contribution in contributions)
  {
   var result=contribution.Result;var calendar=record.Calendar.Single(e=>e.EventId==contribution.EventId);
   var conditions=record.EventConditions[calendar.EventId];
   if(result.ModelVersion!=SimulationReplay.Model(conditions)||result.RandomnessVersion!=SimulationReplay.Randomness(conditions)||result.RaceLaps!=conditions.Laps)throw new ArgumentException("Incompatible result model or distance.");
   if(result.SessionId!=calendar.SessionId||result.Hash!=Hash(result)||contribution.ScoringVersion!=ChampionshipScoring.Version||contribution.PointsVersion!=points.Version||string.IsNullOrWhiteSpace(result.StorageVersion)||result.Revision<1)throw new ArgumentException("Incompatible committed result reference, hash or scoring version.");
   if(result.Classification.Any(r=>!record.Teams.Any(t=>t.TeamId==r.TeamId&&t.Drivers.Any(d=>d.PersonId==r.PersonId&&d.EntryId==r.EntryId))))throw new ArgumentException("Unknown frozen result identity.");
   var awards=ChampionshipScoring.Score(points,entrants,CampaignScoringInputs.Finishes(result));
   if(!awards.SequenceEqual(contribution.Awards))throw new ArgumentException("Committed scoring contribution changed.");
  }
 }
}

