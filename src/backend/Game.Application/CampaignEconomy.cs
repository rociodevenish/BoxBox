using System.Collections.Immutable;
using Game.Domain;
namespace Game.Application;
public static class CampaignEconomy
{
 public static ImmutableArray<CreditEntry> Ledger(CampaignRecord record)=>record.Acquisition?.Ledger??(record.PresetLedger??record.Teams.Select(t=>new CreditEntry("starting-credit",t.TeamId,100,"starting-credit",Version:"preset-starting-credit-v1")).ToArray()).ToImmutableArray();
 public static CampaignRecord Grant(CampaignRecord record,string eventId)
 {
  var configuration=record.Income??SeasonIncome.Default;
  var teams=record.Teams.Select(t=>t.TeamId).ToArray();var ledger=Ledger(record);
  // A compatible pre-economy save is upgraded atomically with its next contribution.
  // Historical completed events receive the same immutable receipts on this one transition.
  if(record.Income is null)foreach(var contribution in record.Contributions??[])ledger=SeasonIncome.Grant(ledger,teams,contribution.EventId,configuration);
  ledger=SeasonIncome.Grant(ledger,teams,eventId,configuration);
  return record.Acquisition is {} acquisition?record with{Income=configuration,Acquisition=acquisition with{Ledger=ledger}}:record with{Income=configuration,PresetLedger=ledger.ToArray()};
 }
 public static void Validate(CampaignRecord record)
 {
  if(record.Income is null){if(record.PresetLedger is not null||Ledger(record).Any(e=>e.Reason=="event-income"))throw new ArgumentException();return;}
  SeasonIncome.ValidateConfiguration(record.Income);
  if(record.Acquisition is null&&record.PresetLedger is null)throw new ArgumentException("Missing saved credit ledger.");
  if(record.Acquisition is not null&&record.PresetLedger is not null)throw new ArgumentException("Only one ledger is permitted.");
  var teams=record.Teams.Select(t=>t.TeamId).ToArray();var ledger=Ledger(record);
  SeasonIncome.ValidateEntries(ledger,teams);
  var expected=(record.Contributions??[]).SelectMany(c=>teams.Select(t=>new CreditEntry("event-income/"+c.EventId,t,record.Income.CreditsPerEvent,"event-income",Version:record.Income.Version,EventId:c.EventId))).ToArray();
  if(!ledger.Where(e=>e.Reason=="event-income").OrderBy(e=>e.EventId).ThenBy(e=>e.TeamId).SequenceEqual(expected.OrderBy(e=>e.EventId).ThenBy(e=>e.TeamId)))throw new ArgumentException("Income must exactly match committed events.");
  if(record.Acquisition is null&&ledger.Length!=teams.Length+expected.Length+ledger.Count(e=>e.Reason is "permanent-development" or "race-package-development"))throw new ArgumentException("Unexpected preset transaction.");
  foreach(var team in teams)
  {
   var running=0;foreach(var entry in ledger.Where(e=>e.TeamId==team)){running=checked(running+entry.Credits);if(running<0)throw new ArgumentException("Negative credit balance.");}
   if(record.Acquisition is null&&(ledger.Count(e=>e.TeamId==team&&e.Reason=="starting-credit"&&e.Credits==100&&e.CommandId=="starting-credit"&&e.Version=="preset-starting-credit-v1")!=1||ledger.Any(e=>e.TeamId==team&&e.Reason is not("starting-credit" or "event-income" or "permanent-development" or "race-package-development"))))throw new ArgumentException("Invalid preset ledger.");
  }
  if(ledger.Any(e=>!teams.Contains(e.TeamId)))throw new ArgumentException();
 }
}
public sealed partial class CampaignCoordinator
{
 public EconomyTeamSnapshot EconomyTeam(string id,AuthIdentity identity,string deviceId,string teamId)
 {
  lock(gate)
  {
   var record=Require(id).Record;
   weekends.AuthoriseCampaignPersonnel(record.Calendar[record.EventIndex].SessionId,identity,deviceId,teamId);
   var own=CampaignEconomy.Ledger(record).Where(e=>e.TeamId==teamId).ToArray();
   var names=(record.Acquisition?.Hires.Where(h=>h.TeamId==teamId)??[]).ToDictionary(h=>h.PersonId,h=>record.Acquisition!.Pool.People.Single(p=>p.Id==h.PersonId).Name);
   return new(id,teamId,checked(own.Sum(e=>e.Credits)),record.Income??SeasonIncome.Default,own,names);
  }
 }
}
