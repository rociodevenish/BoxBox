using System.Collections.Immutable;
namespace Game.Domain;
public sealed record SeasonIncomeConfiguration(string Version,int CreditsPerEvent,int PresetStartingCredit=100);
/// <summary>Flat, versioned event grants in the same credit ledger as preseason spending.</summary>
public static class SeasonIncome
{
 public static readonly SeasonIncomeConfiguration Default=new("flat-event-income-v1",6);
 public static void ValidateConfiguration(SeasonIncomeConfiguration configuration)
 {
  if(configuration.Version!=Default.Version||configuration.CreditsPerEvent is <0 or >20||configuration.PresetStartingCredit!=100)throw new ArgumentException("Unsupported season income configuration.");
 }
 public static ImmutableArray<CreditEntry> Grant(ImmutableArray<CreditEntry> ledger,IReadOnlyList<string> teams,string eventId,SeasonIncomeConfiguration configuration)
 {
  ValidateConfiguration(configuration);
  if(string.IsNullOrWhiteSpace(eventId)||teams.Count==0||teams.Distinct().Count()!=teams.Count)throw new ArgumentException("Supply a completed event and distinct teams.");
  foreach(var team in teams)
  {
   var entry=new CreditEntry("event-income/"+eventId,team,configuration.CreditsPerEvent,"event-income",Version:configuration.Version,EventId:eventId);
   var existing=ledger.Where(e=>e.TeamId==team&&e.CommandId==entry.CommandId).ToArray();
   if(existing.Length==1&&existing[0]==entry)continue;
   if(existing.Length!=0)throw new ArgumentException("Conflicting event income receipt.");
   ledger=ledger.Add(entry);
  }
  return ledger;
 }
 public static void ValidateEntries(IEnumerable<CreditEntry> ledger,IReadOnlyList<string> teams)
 {
  var income=ledger.Where(e=>e.Reason=="event-income").ToArray();
  if(income.Select(e=>(e.TeamId,e.CommandId)).Distinct().Count()!=income.Length||income.Any(e=>!teams.Contains(e.TeamId)||e.Version!=Default.Version||string.IsNullOrWhiteSpace(e.EventId)||e.CommandId!="event-income/"+e.EventId||e.PersonId is not null||e.Credits is <0 or >20))throw new ArgumentException("Invalid event income receipt.");
 }
}
