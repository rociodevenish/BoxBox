using System.Text.Json;
using System.Security.Cryptography;
using Game.Application;
using Game.Domain;
using Game.Simulation;
namespace FullReleaseEvidence;
public static class FrozenVerification
{
 private static readonly JsonSerializerOptions json=BrowserReplay.JsonOptions;
 private static void Equal<T>(T a,T b,string context){if(JsonSerializer.Serialize(a,json)!=JsonSerializer.Serialize(b,json))throw new Exception(context+" differs.");}
 public static string InputHash(CampaignRecord record)
 {
  var value=JsonSerializer.SerializeToElement(new{record.ContentVersion,record.CalendarVersion,record.Calendar,record.Teams,record.FrozenPresets,record.EventConditions,record.ContentSnapshot,record.Points,record.Versions},json);
  using var stream=new MemoryStream();using(var writer=new Utf8JsonWriter(stream))Canonical(writer,value);return Convert.ToHexString(SHA256.HashData(stream.GetBuffer().AsSpan(0,checked((int)stream.Length))));
 }
 private static void Canonical(Utf8JsonWriter writer,JsonElement value)
 {
  if(value.ValueKind==JsonValueKind.Object){writer.WriteStartObject();foreach(var property in value.EnumerateObject().OrderBy(p=>p.Name,StringComparer.Ordinal)){writer.WritePropertyName(property.Name);Canonical(writer,property.Value);}writer.WriteEndObject();}
  else if(value.ValueKind==JsonValueKind.Array){writer.WriteStartArray();foreach(var item in value.EnumerateArray())Canonical(writer,item);writer.WriteEndArray();}
  else value.WriteTo(writer);
 }
public static void Verify(Archive archive,bool partial=false)
{
 if(archive.Schema!="full-release-campaign-evidence-v1")throw new Exception("Unknown archive schema.");
 var saved=archive.Campaign;CampaignSaveIntegrity.Validate(saved,saved.CampaignId);
 if(saved.Calendar.Length!=23||archive.Weekends.Length!=(saved.Contributions?.Length??0)||(!partial&&(saved.Lifecycle!="season-complete"||saved.EventIndex!=22||archive.Weekends.Length!=23||saved.DevelopmentChoices?.Length!=220||saved.Development?.Opportunities.Count(o=>o.Closed)!=22)))throw new Exception("Incomplete full-release campaign; partial checkpoints are not acceptance.");
 if(saved.Input.Laps!=6||saved.Input.Seed!=123||saved.Input.PersonnelMode!="preset")throw new Exception("Preset workload inputs differ");
 if(saved.ContentVersion!=archive.ContentVersion||saved.Teams.Length!=11||saved.Teams.SelectMany(t=>t.Drivers).Select(d=>d.PersonId).Distinct().Count()!=22)throw new Exception("Frozen identities differ");
 if((!partial||archive.FrozenInputHash.Length>0)&&archive.FrozenInputHash!=InputHash(saved))throw new Exception("Frozen content/conditions/source/assets hash differs");
 foreach(var contribution in saved.Contributions??[])foreach(var team in saved.Teams){var grants=CampaignEconomy.Ledger(saved).Where(e=>e.TeamId==team.TeamId&&e.EventId==contribution.EventId&&e.Reason=="event-income").ToArray();if(grants.Length!=1||grants[0].Credits!=6)throw new Exception("Missing or duplicated event grant");}
 foreach(var op in saved.Development?.Opportunities.Where(o=>o.Closed)??[]){var own=op.Decisions.Single(d=>d.TeamId=="team-01");if(own.ActionKind!="Save"||own.Source!="host-absent-save"||op.Decisions.Length!=11)throw new Exception("Claimed Save or automatic commitment coverage differs");}
 var entrants=saved.Teams.SelectMany(t=>t.Drivers.Select(d=>(t.TeamId,d.PersonId))).Select((d,i)=>new ChampionshipEntrant(d.PersonId,d.TeamId,i)).ToArray();
 for(var i=0;i<archive.Weekends.Length;i++)
 {
  if(archive.Weekends[i].AutomaticPreparation.Select(a=>a.TeamId).Distinct().Count()!=11)throw new Exception("Automatic/absent preparation coverage differs");
  var prepared=archive.Weekends[i].Preparation;
  if(prepared.Teams.Count!=11||prepared.Teams.SelectMany(t=>t.Preset.Cars).Select(c=>c.EntryId).Distinct().Count()!=22)throw new Exception("Frozen preparation coverage differs");
  var q=archive.Weekends[i].Qualifying;Equal(prepared.Teams.Select(t=>t.Effective).ToArray(),q.Attempts[0].Weekends.ToArray(),"Qualifying uses frozen effective preparation");if(q.SessionId!=saved.Calendar[i].SessionId||q.Attempts.Length!=3||q.Results.Length!=3)throw new Exception("Incomplete qualifying history.");
  var replayedQualifying=q.Attempts.Select(Qualifying.Resolve).ToArray();Equal(q.Results,replayedQualifying,"Qualifying replay");Equal(q.Grid,Qualifying.Grid(replayedQualifying),"Qualifying grid");
  var replay=archive.Weekends[i].Race;Equal(q.Grid.Select(g=>new StartGridSlot(g.Position,g.EntryId)).ToArray(),replay.Replay.Opening!.Input.Grid.ToArray(),"Opening grid from qualifying");var verified=BrowserReplay.Verify(replay);
  if(!verified.Matches||!verified.Finished||replay.SessionId!=saved.Calendar[i].SessionId)throw new Exception("Weekend replay differs.");
  var contribution=saved.Contributions![i];CampaignResultIntegrity.Validate(saved);
  var engine=SimulationReplay.CreateEngine(replay.Replay);
  for(var operation=0;operation<replay.Phases.Count;operation++)
  {
   var plan=replay.Replay.Plan.Scheduled.Where(p=>p.Phase==operation).ToDictionary(p=>p.DriverId,p=>p.Decision);
   _=(replay.OperationKinds?[operation]??"Normal") switch
   {
    "Normal"=>engine.ResolvePhase(plan),"Weather"=>engine.ResolveWeatherReaction(plan),
    "SafetyCarStaging"=>engine.CommitSafetyCarChoices(plan.ToDictionary(p=>p.Key,p=>new SafetyCarDecision(p.Value.PitCompound))),
    "SafetyCar"=>engine.ResolveSafetyCar(plan.ToDictionary(p=>p.Key,p=>new SafetyCarDecision(p.Value.PitCompound))),
    _=>throw new Exception("Unknown operation.")
   };
  }
  var final=engine.GetState();if(!double.IsFinite(final.Time)||final.Time<=0||final.Classification.Count!=22||final.Classification.Any(r=>r.Laps is <0 or >6||!double.IsFinite(r.Progress)||r.Progress<0))throw new Exception("Invalid finished raw race bounds");var identities=saved.FrozenPresets[saved.Calendar[i].EventId].SelectMany(p=>p.Cars).ToDictionary(c=>c.EntryId);
  var raw=final.Classification.Select(r=>{var c=identities[r.DriverId];return new CommittedWeekendRow(r.DriverId,c.Driver.Id,c.TeamId,r.Position,r.Laps,r.Progress,r.GapSeconds,r.GapLaps,r.FinishTime,r.RetirementTime,final.Eligibility[r.DriverId],r.RetirementTime is not null?"retired":final.Eligibility[r.DriverId]?"finished":"ineligible");}).ToArray();
  Equal(contribution.Result.Classification,raw,"Raw result from replay");
  var awards=ChampionshipScoring.Score(saved.Points!,entrants,contribution.Result.Classification.Select(r=>new ChampionshipFinish(r.PersonId,r.Position,r.Eligible,r.Status=="retired")).ToArray());
  Equal(contribution.Awards,awards,"Scoring");Equal(archive.Weekends[i].Standings,ChampionshipScoring.Rebuild(entrants,saved.Contributions!.Take(i+1).Select(c=>(IReadOnlyList<ChampionshipAward>)c.Awards)),"Standings");
 }
}
}
