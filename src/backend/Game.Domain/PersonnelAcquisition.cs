using System.Collections.Immutable;
namespace Game.Domain;

public record PersonnelCard(string Id,string Name,string Role,int Price,string SourceVersion,string EffectLabel,DriverPreset? Driver=null,ImmutableDictionary<string,int>? Attributes=null);
public record PersonnelPool(string Version,string PriceVersion,int StartingCredit,int MinimumCarReserve,ImmutableArray<PersonnelCard> People);
public record PersonnelOffer(string TeamId,string Slot,ImmutableArray<string> PersonIds);
public record PersonnelAssignment(string TeamId,string Slot,string PersonId,string CommandId);
public record CreditEntry(string CommandId,string TeamId,int Credits,string Reason,string? PersonId=null);
public record PersonnelHire(string CommandId,long Revision,string TeamId,string PersonId);
public record PersonnelReceipt(string CommandId,string Fingerprint);
public record AcquisitionState(string Version,long Revision,PersonnelPool Pool,ImmutableArray<string> TeamIds,PersonnelOffer? Offer,
 ImmutableArray<PersonnelAssignment> Hires,ImmutableArray<CreditEntry> Ledger,ImmutableArray<PersonnelReceipt> Commands);

/// <summary>Frozen content and integer-credit ownership transactions. No persistence or identity services.</summary>
public static class PersonnelAcquisition
{
 public const string Version="personnel-acquisition-v1";
 public static readonly ImmutableArray<string> RequiredSlots=["manager","driver-1","driver-2","race-engineer-1","race-engineer-2","technical-engineer-1","technical-engineer-2"];
 public static string Role(string slot)=>slot.StartsWith("driver-")?"Driver":slot.StartsWith("race-engineer-")?"Race Engineer":slot.StartsWith("technical-engineer-")?"Technical Engineer":slot=="manager"?"Manager":throw new ArgumentException("Unknown mandatory slot.");
 public static AcquisitionState Open(PersonnelPool pool,IReadOnlyList<string> teams)
 {
  ValidatePool(pool,teams.Count);
  if(teams.Count==0||teams.Distinct().Count()!=teams.Count||teams.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Choose distinct teams.");
  var offers=pool.People.Where(p=>p.Role=="Manager").Take(3).Select(p=>p.Id).ToImmutableArray();
  return new(Version,1,pool,teams.ToImmutableArray(),new(teams[0],"manager",offers),[],teams.Select(t=>new CreditEntry("starting-credit",t,pool.StartingCredit,"starting-credit")).ToImmutableArray(),[]);
 }
 public static int Balance(AcquisitionState state,string team)=>checked(state.Ledger.Where(e=>e.TeamId==team).Sum(e=>e.Credits));
 public static AcquisitionState Hire(AcquisitionState state,PersonnelHire command)
 {
  if(command is null||!Guid.TryParse(command.CommandId,out var key)||key==Guid.Empty)throw new ArgumentException("Supply a command identity.");
  var fingerprint=$"{command.Revision}/{command.TeamId}/{command.PersonId}";
  if(state.Commands.FirstOrDefault(c=>c.CommandId==key.ToString("D")) is {} receipt)
   return receipt.Fingerprint==fingerprint?state:throw new InvalidOperationException("Command identity already accepted with different inputs.");
  if(command.Revision!=state.Revision)throw new InvalidOperationException("Offers changed. Refresh the saved revision.");
  var offer=state.Offer;
  if(offer is null||offer.TeamId!=command.TeamId||!offer.PersonIds.Contains(command.PersonId))throw new InvalidOperationException("Choose a reserved offer for the active team.");
  var card=state.Pool.People.Single(p=>p.Id==command.PersonId);
  if(card.Price>Balance(state,command.TeamId))throw new InvalidOperationException("Insufficient credits. No hire was made.");
  if(state.Hires.Any(h=>h.PersonId==card.Id||h.TeamId==command.TeamId&&h.Slot==offer.Slot))throw new InvalidOperationException("Person or slot already assigned.");
  return state with{Revision=state.Revision+1,Offer=null,Hires=state.Hires.Add(new(command.TeamId,offer.Slot,card.Id,key.ToString("D"))),
   Ledger=state.Ledger.Add(new(key.ToString("D"),command.TeamId,-card.Price,"personnel-hire",card.Id)),Commands=state.Commands.Add(new(key.ToString("D"),fingerprint))};
 }
 public static void ValidatePool(PersonnelPool pool,int teamCount)
 {
  if(pool.Version is null||!pool.Version.StartsWith("personnel-pool-v1/",StringComparison.Ordinal)||pool.PriceVersion!="provisional-prices-v1"||pool.StartingCredit<=0||pool.MinimumCarReserve<0||pool.People.IsDefaultOrEmpty||pool.People.Select(p=>p.Id).Distinct().Count()!=pool.People.Length)throw new ArgumentException("Invalid frozen personnel pool.");
  foreach(var person in pool.People)
   if(string.IsNullOrWhiteSpace(person.Id)||string.IsNullOrWhiteSpace(person.Name)||string.IsNullOrWhiteSpace(person.SourceVersion)||string.IsNullOrWhiteSpace(person.EffectLabel)||person.Price<=0||person.Price>pool.StartingCredit||!RequiredSlots.Select(Role).Contains(person.Role)||person.Role=="Driver"&&(person.Driver is null||person.Driver.Id!=person.Id))throw new ArgumentException("Invalid personnel identity, role or fixed price.");
  foreach(var person in pool.People)
  {
   if(person.Driver is {} driver&&(driver.Ratings.Count==0||driver.Ratings.Values.Any(v=>!double.IsFinite(v)||v is <1 or >100)||string.IsNullOrWhiteSpace(driver.Version)))throw new ArgumentException("Driver inputs must retain valid detailed ratings and source versions.");
   if(person.Role!="Driver"&&(person.Driver is not null||person.Attributes is null||person.Attributes.Count==0||person.Attributes.Values.Any(v=>v is <1 or >100)))throw new ArgumentException("Provisional staff attributes must use the printed 1–100 scale.");
  }
  foreach(var role in RequiredSlots.Select(Role).Distinct())
   if(pool.People.Count(p=>p.Role==role)<Math.Max(3,teamCount*RequiredSlots.Count(s=>Role(s)==role)))throw new ArgumentException($"Insufficient unique {role} content for every team.");
 }
 public static void Validate(AcquisitionState state)
 {
  if(state.Version!=Version||state.Revision<1||state.TeamIds.IsDefaultOrEmpty||state.TeamIds.Distinct().Count()!=state.TeamIds.Length)throw new ArgumentException("Unsupported acquisition save.");
  ValidatePool(state.Pool,state.TeamIds.Length);
  if(state.Hires.Select(h=>h.PersonId).Distinct().Count()!=state.Hires.Length||state.Hires.Select(h=>(h.TeamId,h.Slot)).Distinct().Count()!=state.Hires.Length||state.Commands.Select(c=>c.CommandId).Distinct().Count()!=state.Commands.Length)throw new ArgumentException("Duplicate ownership or command.");
  if(state.Ledger.Any(e=>e.Credits>state.Pool.StartingCredit||e.Credits< -state.Pool.StartingCredit))throw new ArgumentException("Invalid credit amount.");
  foreach(var team in state.TeamIds)
  {
   var initial=state.Ledger.Where(e=>e.TeamId==team&&e.Reason=="starting-credit").ToArray();
   if(initial.Length!=1||initial[0].Credits!=state.Pool.StartingCredit||Balance(state,team)<0)throw new ArgumentException("Invalid starting credit or debit.");
  }
  if(state.Ledger.Length!=state.TeamIds.Length+state.Hires.Length||state.Ledger.Any(e=>!state.TeamIds.Contains(e.TeamId)))throw new ArgumentException("Unrecognised ledger entries.");
  foreach(var hire in state.Hires)
  {
   var card=state.Pool.People.Single(p=>p.Id==hire.PersonId);
   if(!state.TeamIds.Contains(hire.TeamId)||!RequiredSlots.Contains(hire.Slot)||Role(hire.Slot)!=card.Role||!state.Commands.Any(c=>c.CommandId==hire.CommandId)||state.Ledger.Count(e=>e.TeamId==hire.TeamId&&e.CommandId==hire.CommandId&&e.PersonId==card.Id&&e.Credits==-card.Price&&e.Reason=="personnel-hire")!=1)throw new ArgumentException("Hire and debit must agree.");
  }
  if(state.Offer is {} offer&&(offer.PersonIds.Length is <2 or >3||!state.TeamIds.Contains(offer.TeamId)||offer.PersonIds.Distinct().Count()!=offer.PersonIds.Length||offer.PersonIds.Any(id=>!state.Pool.People.Any(p=>p.Id==id&&p.Role==Role(offer.Slot))||state.Hires.Any(h=>h.PersonId==id))))throw new ArgumentException("Invalid reserved offers.");
 }
}
