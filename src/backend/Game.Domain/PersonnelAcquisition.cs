using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
namespace Game.Domain;

public record PersonnelCard(string Id,string Name,string Role,int Price,string SourceVersion,string EffectLabel,DriverPreset? Driver=null,ImmutableDictionary<string,int>? Attributes=null);
public record ConstructionCreditMapping(string Version,string SourceVersion,int MinimumBudgetUnits,int CreditsPerBudgetUnit);
public record PersonnelPool(string Version,string PriceVersion,int StartingCredit,int MinimumCarReserve,ImmutableArray<PersonnelCard> People,ConstructionCreditMapping? ConstructionMapping=null);
public record PersonnelOffer(string TeamId,string Slot,ImmutableArray<string> PersonIds);
public record PersonnelAssignment(string TeamId,string Slot,string PersonId,string CommandId);
public record CreditEntry(string CommandId,string TeamId,int Credits,string Reason,string? PersonId=null,string? Version=null,string? EventId=null);
public record PersonnelHire(string CommandId,long Revision,string TeamId,string PersonId);
public record PersonnelScout(string CommandId,long Revision,string TeamId);
public record PersonnelDraw(int Number,int Cursor,string TeamId,string Slot,bool Scout,ImmutableArray<string> PersonIds);
public record ScoutAllocation(string Version,int BaseTokens=2,int LaterPriorityBonus=1,int MaximumTokens=3);
public record PersonnelReceipt(string CommandId,string Fingerprint);
public record PersonnelPick(string TeamId,string Slot);
public record PersonnelCarousel(string Version,int Seed,ImmutableArray<string> InitialOrder,ImmutableArray<PersonnelPick> Picks,int Cursor);
public record AcquisitionState(string Version,long Revision,PersonnelPool Pool,ImmutableArray<string> TeamIds,PersonnelOffer? Offer,
 ImmutableArray<PersonnelAssignment> Hires,ImmutableArray<CreditEntry> Ledger,ImmutableArray<PersonnelReceipt> Commands,PersonnelCarousel? Carousel=null,ScoutAllocation? ScoutAllocation=null,ImmutableDictionary<string,int>? ScoutTokens=null,ImmutableArray<PersonnelDraw> Draws=default,ImmutableDictionary<string,ConfirmedBaseCar>? ConstructedCars=null);

/// <summary>Frozen content and integer-credit ownership transactions. No persistence or identity services.</summary>
public static class PersonnelAcquisition
{
 public const string Version="personnel-acquisition-v3";
 public const string ReserveVersion="global-demand-envelope-v1";
 public static readonly ImmutableArray<string> RequiredSlots=["manager","driver-1","driver-2","race-engineer-1","race-engineer-2","technical-engineer-1","technical-engineer-2"];
 public static string Role(string slot)=>slot.StartsWith("driver-")?"Driver":slot.StartsWith("race-engineer-")?"Race Engineer":slot.StartsWith("technical-engineer-")?"Technical Engineer":slot=="manager"?"Manager":throw new ArgumentException("Unknown mandatory slot.");
 public static AcquisitionState Open(PersonnelPool pool,IReadOnlyList<string> teams,int seed=123,ScoutAllocation? scouts=null)
 {
  ValidatePool(pool,teams.Count);
  if(teams.Count==0||teams.Distinct().Count()!=teams.Count||teams.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Choose distinct teams.");
  scouts??=new("bounded-scouts-v1");
  if(!ValidScoutAllocation(scouts))throw new ArgumentException("Scout allocation must remain bounded between zero and three tokens.");
  var order=teams.OrderBy(t=>Rank(seed,"initial-order",t),StringComparer.Ordinal).ThenBy(t=>t,StringComparer.Ordinal).ToImmutableArray();
  var picks=ImmutableArray.CreateBuilder<PersonnelPick>();
  string[][] categories=[["manager"],["driver-1","driver-2"],["race-engineer-1","race-engineer-2"],["technical-engineer-1","technical-engineer-2"]];
  foreach(var (slots,category) in categories.Select((slots,category)=>(slots,category)))
  {
   var rotated=Enumerable.Range(0,order.Length).Select(i=>order[(i+category)%order.Length]).ToArray();
   foreach(var (slot,index) in slots.Select((slot,index)=>(slot,index)))
    foreach(var team in index%2==0?rotated:rotated.Reverse())picks.Add(new(team,slot));
  }
  var state=new AcquisitionState(Version,1,pool,teams.Order(StringComparer.Ordinal).ToImmutableArray(),null,[],teams.Select(t=>new CreditEntry("starting-credit",t,pool.StartingCredit,"starting-credit")).ToImmutableArray(),[],new("carousel-rotation-v1",seed,order,picks.ToImmutable(),0),scouts,order.Select((team,index)=>(team,tokens:AllocatedScoutTokens(scouts,order.Length,index))).ToImmutableDictionary(x=>x.team,x=>x.tokens),[]);
  return NextOffer(state);
 }
 private static bool ValidScoutAllocation(ScoutAllocation allocation)=>allocation.Version=="bounded-scouts-v1"&&allocation.BaseTokens is >=0 and <=3&&allocation.LaterPriorityBonus is >=0 and <=1&&allocation.MaximumTokens is >=0 and <=3&&allocation.BaseTokens<=allocation.MaximumTokens;
 private static int AllocatedScoutTokens(ScoutAllocation allocation,int teamCount,int index)=>Math.Min(allocation.MaximumTokens,allocation.BaseTokens+(teamCount>1&&index>=(teamCount+1)/2?allocation.LaterPriorityBonus:0));
 private static string Rank(int seed,string purpose,string id)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{seed.ToString(CultureInfo.InvariantCulture)}/{purpose}/{id}")));
 private static AcquisitionState NextOffer(AcquisitionState state)
 {
  var carousel=state.Carousel!;
  if(carousel.Cursor==carousel.Picks.Length)return state with{Offer=null};
  var pick=carousel.Picks[carousel.Cursor];
  return DrawOffer(state,pick,false);
 }
 private static AcquisitionState DrawOffer(AcquisitionState state,PersonnelPick pick,bool scout)
 {
  var excluded=scout?state.Offer!.PersonIds:ImmutableArray<string>.Empty;
  var candidates=Available(state,pick.Slot).Where(p=>!excluded.Contains(p.Id))
   .OrderBy(p=>Rank(state.Carousel!.Seed,$"draw/{state.Draws.Length}",p.Id),StringComparer.Ordinal).ThenBy(p=>p.Id,StringComparer.Ordinal)
   .ToArray();
  var selected=candidates.Take(scout?state.Offer!.PersonIds.Length:3).ToArray();
  if(!selected.Any(p=>CandidateUnavailable(state,pick.TeamId,pick.Slot,p) is null))
  {
   var affordable=candidates.FirstOrDefault(p=>CandidateUnavailable(state,pick.TeamId,pick.Slot,p) is null)??throw new InvalidOperationException("No affordable completion offer is available. Preserve the campaign and review its frozen pool.");
   selected[^1]=affordable;
  }
  var people=selected.Select(p=>p.Id).ToImmutableArray();
  return state with{Offer=new(pick.TeamId,pick.Slot,people),Draws=state.Draws.Add(new(state.Draws.Length,state.Carousel!.Cursor,pick.TeamId,pick.Slot,scout,people))};
 }
 private static IEnumerable<PersonnelCard> Available(AcquisitionState state,string slot)=>state.Pool.People.Where(p=>p.Role==Role(slot)&&!state.Hires.Any(h=>h.PersonId==p.Id));
 public static string? ScoutUnavailable(AcquisitionState state,string teamId)
 {
  if(state.Offer is not {} offer||offer.TeamId!=teamId)return "Wait for your scheduled pick.";
  if(state.ScoutTokens![teamId]<=0)return "No Scout tokens remaining.";
  if(Available(state,offer.Slot).Count(p=>!offer.PersonIds.Contains(p.Id))<offer.PersonIds.Length)return "Insufficient alternatives to replace this entire offer. No token will be spent.";
  if(!Available(state,offer.Slot).Any(p=>!offer.PersonIds.Contains(p.Id)&&CandidateUnavailable(state,teamId,offer.Slot,p) is null))return "No affordable alternatives preserve the completion reserve. No token will be spent.";
  return null;
 }
 public static AcquisitionState Scout(AcquisitionState state,PersonnelScout command)
 {
  if(command is null||!Guid.TryParse(command.CommandId,out var key)||key==Guid.Empty)throw new ArgumentException("Supply a command identity.");
  var fingerprint=$"scout/{command.Revision}/{command.TeamId}";
  if(state.Commands.FirstOrDefault(c=>c.CommandId==key.ToString("D")) is {} receipt)
   return receipt.Fingerprint==fingerprint?state:throw new InvalidOperationException("Command identity already accepted with different inputs.");
  if(command.Revision!=state.Revision)throw new InvalidOperationException("Offers changed. Refresh the saved revision.");
  if(ScoutUnavailable(state,command.TeamId) is {} unavailable)throw new InvalidOperationException(unavailable);
  var offer=state.Offer!;
  return DrawOffer(state with{Revision=state.Revision+1,ScoutTokens=state.ScoutTokens!.SetItem(command.TeamId,state.ScoutTokens[command.TeamId]-1),Commands=state.Commands.Add(new(key.ToString("D"),fingerprint))},new(offer.TeamId,offer.Slot),true);
 }
 public static int Balance(AcquisitionState state,string team)=>checked(state.Ledger.Where(e=>e.TeamId==team).Sum(e=>e.Credits));
 public static ImmutableArray<string> RemainingSlots(AcquisitionState state,string team)=>RequiredSlots.Where(slot=>!state.Hires.Any(h=>h.TeamId==team&&h.Slot==slot)).ToImmutableArray();
 public static bool RosterReady(AcquisitionState state,string team)=>RemainingSlots(state,team).IsEmpty&&Balance(state,team)>=(state.ConstructedCars?.ContainsKey(team)==true?0:state.Pool.MinimumCarReserve);
 // The Nth cheapest unowned person backs N remaining places globally. Removing any
 // person while reducing demand by one can never raise that bound. No cheap identity
 // is promised twice; every remaining place can be filled at or below its role bound.
 private static Dictionary<string,int> RoleBounds(PersonnelPool pool,int teamCount,ImmutableArray<PersonnelAssignment> hires)
 {
  var bounds=new Dictionary<string,int>();
  foreach(var role in RequiredSlots.Select(Role).Distinct())
  {
   var needed=teamCount*RequiredSlots.Count(s=>Role(s)==role)-hires.Count(h=>Role(h.Slot)==role);
   var candidates=pool.People.Where(p=>p.Role==role&&!hires.Any(h=>h.PersonId==p.Id)).OrderBy(p=>p.Price).Take(needed).ToArray();
   if(needed<0||candidates.Length<needed)throw new ArgumentException($"Insufficient unique affordable {role} supply for remaining mandatory places.");
   bounds[role]=needed==0?0:candidates[^1].Price;
  }
  return bounds;
 }
 public static int CompletionReserve(AcquisitionState state,string team)
 {
  var bounds=RoleBounds(state.Pool,state.TeamIds.Length,state.Hires);
  return checked((state.ConstructedCars?.ContainsKey(team)==true?0:state.Pool.MinimumCarReserve)+RemainingSlots(state,team).Sum(slot=>bounds[Role(slot)]));
 }
 public static int ReserveAfterHire(AcquisitionState state,string team,string personId)
 {
  var offer=state.Offer;
  if(offer is null||offer.TeamId!=team||!offer.PersonIds.Contains(personId))throw new InvalidOperationException("Choose a reserved offer for the active team.");
  return CompletionReserve(state with{Hires=state.Hires.Add(new(team,offer.Slot,personId,"reserve-preview"))},team);
 }
 public static string? HireUnavailable(AcquisitionState state,string team,string personId)
 {
  if(state.Offer is not {} offer||offer.TeamId!=team||!offer.PersonIds.Contains(personId))return "Choose a reserved offer for the active team.";
  var card=state.Pool.People.Single(p=>p.Id==personId);
  return CandidateUnavailable(state,team,offer.Slot,card);
 }
 private static string? CandidateUnavailable(AcquisitionState state,string team,string slot,PersonnelCard card)
 {
  var reserve=CompletionReserve(state with{Hires=state.Hires.Add(new(team,slot,card.Id,"reserve-preview"))},team);
  return Balance(state,team)-card.Price<reserve?$"Completion reserve requires {reserve} credits after this hire for remaining personnel and the minimum car. No hire was made.":null;
 }
 public static AcquisitionState Hire(AcquisitionState state,PersonnelHire command)
 {
  if(command is null||!Guid.TryParse(command.CommandId,out var key)||key==Guid.Empty)throw new ArgumentException("Supply a command identity.");
  var fingerprint=$"hire/{command.Revision}/{command.TeamId}/{command.PersonId}";
  if(state.Commands.FirstOrDefault(c=>c.CommandId==key.ToString("D")) is {} receipt)
   return receipt.Fingerprint==fingerprint?state:throw new InvalidOperationException("Command identity already accepted with different inputs.");
  if(command.Revision!=state.Revision)throw new InvalidOperationException("Offers changed. Refresh the saved revision.");
  var offer=state.Offer;
  if(offer is null||offer.TeamId!=command.TeamId||!offer.PersonIds.Contains(command.PersonId))throw new InvalidOperationException("Choose a reserved offer for the active team.");
  var card=state.Pool.People.Single(p=>p.Id==command.PersonId);
  if(HireUnavailable(state,command.TeamId,card.Id) is {} unavailable)throw new InvalidOperationException(unavailable);
  if(state.Hires.Any(h=>h.PersonId==card.Id||h.TeamId==command.TeamId&&h.Slot==offer.Slot))throw new InvalidOperationException("Person or slot already assigned.");
  return NextOffer(state with{Revision=state.Revision+1,Offer=null,Carousel=state.Carousel! with{Cursor=state.Carousel.Cursor+1},Hires=state.Hires.Add(new(command.TeamId,offer.Slot,card.Id,key.ToString("D"))),
   Ledger=state.Ledger.Add(new(key.ToString("D"),command.TeamId,-card.Price,"personnel-hire",card.Id)),Commands=state.Commands.Add(new(key.ToString("D"),fingerprint))});
 }
 public static void ValidatePool(PersonnelPool pool,int teamCount)
 {
  if(pool.Version is null||!pool.Version.StartsWith("personnel-pool-v1/",StringComparison.Ordinal)||pool.PriceVersion!="provisional-prices-v1"||pool.StartingCredit<=0||pool.MinimumCarReserve<0||pool.People.IsDefaultOrEmpty||pool.People.Select(p=>p.Id).Distinct().Count()!=pool.People.Length)throw new ArgumentException("Invalid frozen personnel pool.");
  if(pool.ConstructionMapping is not {Version:"budgetUnits-to-credits-v1",SourceVersion:"1.0.0/2026.10.09-preseason-car-build-v1",MinimumBudgetUnits:18,CreditsPerBudgetUnit:1}||pool.MinimumCarReserve!=18)throw new ArgumentException("Unsupported construction credit mapping. Preserve the frozen pool and restore a compatible version.");
  foreach(var person in pool.People)
   if(string.IsNullOrWhiteSpace(person.Id)||string.IsNullOrWhiteSpace(person.Name)||string.IsNullOrWhiteSpace(person.SourceVersion)||string.IsNullOrWhiteSpace(person.EffectLabel)||person.Price<=0||person.Price>pool.StartingCredit||!RequiredSlots.Select(Role).Contains(person.Role)||person.Role=="Driver"&&(person.Driver is null||person.Driver.Id!=person.Id))throw new ArgumentException("Invalid personnel identity, role or fixed price.");
  foreach(var person in pool.People)
  {
   if(person.Driver is {} driver&&(driver.Ratings.Count==0||driver.Ratings.Values.Any(v=>!double.IsFinite(v)||v is <1 or >100)||string.IsNullOrWhiteSpace(driver.Version)))throw new ArgumentException("Driver inputs must retain valid detailed ratings and source versions.");
   if(person.Role!="Driver"&&(person.Driver is not null||person.Attributes is null||person.Attributes.Count==0||person.Attributes.Values.Any(v=>v is <1 or >100)))throw new ArgumentException("Provisional staff attributes must use the printed 1–100 scale.");
  }
  foreach(var role in RequiredSlots.Select(Role).Distinct())
   if(pool.People.Count(p=>p.Role==role)<Math.Max(3,teamCount*RequiredSlots.Count(s=>Role(s)==role)+1))throw new ArgumentException($"Insufficient unique {role} content for every team.");
  var bounds=RoleBounds(pool,teamCount,[]);
  if((long)pool.MinimumCarReserve+RequiredSlots.Sum(slot=>(long)bounds[Role(slot)])>pool.StartingCredit)throw new ArgumentException("Insufficient unique affordable personnel supply: conservative mandatory completion and minimum car exceed starting credit. No credit was granted.");
 }
 public static void Validate(AcquisitionState state)
 {
  if(state.Version!=Version||state.Revision<1||state.TeamIds.IsDefaultOrEmpty||state.TeamIds.Distinct().Count()!=state.TeamIds.Length)throw new ArgumentException("Unsupported acquisition save.");
  ValidatePool(state.Pool,state.TeamIds.Length);
  var carousel=state.Carousel;
  if(carousel is null||carousel.Version!="carousel-rotation-v1"||carousel.InitialOrder.Length!=state.TeamIds.Length||!carousel.InitialOrder.Order().SequenceEqual(state.TeamIds.Order())||carousel.Picks.Length!=state.TeamIds.Length*RequiredSlots.Length||carousel.Cursor!=state.Hires.Length||carousel.Cursor>carousel.Picks.Length)throw new ArgumentException("Unsupported carousel save. No saved data was changed.");
  if(carousel.Picks.Select(p=>(p.TeamId,p.Slot)).Distinct().Count()!=carousel.Picks.Length||carousel.Picks.Any(p=>!state.TeamIds.Contains(p.TeamId)||!RequiredSlots.Contains(p.Slot)))throw new ArgumentException("Invalid carousel schedule.");
  if(state.Hires.Where((h,i)=>h.TeamId!=carousel.Picks[i].TeamId||h.Slot!=carousel.Picks[i].Slot).Any())throw new ArgumentException("Hires must follow the saved schedule.");
  if(carousel.Cursor==carousel.Picks.Length?state.Offer is not null:state.Offer is null||state.Offer.TeamId!=carousel.Picks[carousel.Cursor].TeamId||state.Offer.Slot!=carousel.Picks[carousel.Cursor].Slot)throw new ArgumentException("Offer must match the saved cursor.");
  if(state.ScoutAllocation is not {} allocation||!ValidScoutAllocation(allocation)||state.ScoutTokens is null||state.ScoutTokens.Count!=state.TeamIds.Length||state.Draws.IsDefaultOrEmpty)throw new ArgumentException("Unsupported Scout save.");
  foreach(var (team,index) in carousel.InitialOrder.Select((team,index)=>(team,index)))
  {
   var allocated=AllocatedScoutTokens(allocation,carousel.InitialOrder.Length,index);
   if(!state.ScoutTokens.TryGetValue(team,out var remaining)||remaining<0||remaining!=allocated-state.Draws.Count(d=>d.TeamId==team&&d.Scout))throw new ArgumentException("Invalid Scout token conservation.");
  }
  foreach(var (draw,index) in state.Draws.Select((draw,index)=>(draw,index)))
   if(draw.Number!=index||draw.Cursor<0||draw.Cursor>=carousel.Picks.Length||draw.TeamId!=carousel.Picks[draw.Cursor].TeamId||draw.Slot!=carousel.Picks[draw.Cursor].Slot||draw.PersonIds.Length is <2 or >3||draw.PersonIds.Distinct().Count()!=draw.PersonIds.Length||draw.PersonIds.Any(id=>!state.Pool.People.Any(p=>p.Id==id&&p.Role==Role(draw.Slot))))throw new ArgumentException("Invalid saved offer draw.");
  if(state.Draws.Count(d=>!d.Scout)!=Math.Min(carousel.Cursor+1,carousel.Picks.Length)||state.Commands.Length!=state.Hires.Length+state.Draws.Count(d=>d.Scout)||state.Revision!=state.Commands.Length+1)throw new ArgumentException("Invalid acquisition command history.");
  if(state.Offer is {} current&&!current.PersonIds.SequenceEqual(state.Draws[^1].PersonIds))throw new ArgumentException("Current offers must match the saved draw.");
  if(state.Hires.Select(h=>h.PersonId).Distinct().Count()!=state.Hires.Length||state.Hires.Select(h=>(h.TeamId,h.Slot)).Distinct().Count()!=state.Hires.Length||state.Commands.Select(c=>c.CommandId).Distinct().Count()!=state.Commands.Length)throw new ArgumentException("Duplicate ownership or command.");
  if(state.Ledger.Any(e=>e.Credits>state.Pool.StartingCredit||e.Credits< -state.Pool.StartingCredit))throw new ArgumentException("Invalid credit amount.");
  foreach(var team in state.TeamIds)
  {
   var initial=state.Ledger.Where(e=>e.TeamId==team&&e.Reason=="starting-credit").ToArray();
   if(initial.Length!=1||initial[0].Credits!=state.Pool.StartingCredit||Balance(state,team)<CompletionReserve(state,team))throw new ArgumentException("Invalid starting credit, debit or affordable completion reserve.");
  }
  var cars=state.ConstructedCars??ImmutableDictionary<string,ConfirmedBaseCar>.Empty;
  SeasonIncome.ValidateEntries(state.Ledger,state.TeamIds);
  if(state.Ledger.Any(e=>e.Reason=="permanent-development"&&(e.Credits is > -8 or < -16||e.Version!=PermanentDevelopment.Version||string.IsNullOrWhiteSpace(e.PersonId)||string.IsNullOrWhiteSpace(e.EventId)||!Guid.TryParse(e.CommandId,out var developmentKey)||developmentKey==Guid.Empty)))throw new ArgumentException("Invalid development debit.");
  if(state.Ledger.Any(e=>e.Reason=="race-package-development"&&(e.Credits is > -8 or < -24||e.Version is not(PackageDevelopment.Version or PackageDevelopment.RichVersion or PackageDevelopment.RushVersion)||string.IsNullOrWhiteSpace(e.PersonId)||string.IsNullOrWhiteSpace(e.EventId)||!Guid.TryParse(e.CommandId,out var packageKey)||packageKey==Guid.Empty)))throw new ArgumentException("Invalid package debit.");
  if(state.Ledger.Length!=state.TeamIds.Length+state.Hires.Length+cars.Count+state.Ledger.Count(e=>e.Reason is "event-income" or "permanent-development" or "race-package-development")||state.Ledger.Any(e=>!state.TeamIds.Contains(e.TeamId)))throw new ArgumentException("Unrecognised ledger entries.");
  foreach(var (team,car) in cars)
  {
   CarConstruction.ValidateConfirmed(car);
   if(team!=car.TeamId||!state.TeamIds.Contains(team)||!RemainingSlots(state,team).IsEmpty||state.Commands.Any(c=>c.CommandId==car.CommandId)||state.Ledger.Count(e=>e.TeamId==team&&e.CommandId==car.CommandId&&e.Credits==-car.Cost&&e.Reason=="base-car-construction"&&e.PersonId==car.Id)!=1)throw new ArgumentException("Construction and its exact accepted debit must agree.");
  }
  foreach(var hire in state.Hires)
  {
   var card=state.Pool.People.Single(p=>p.Id==hire.PersonId);
   if(!state.TeamIds.Contains(hire.TeamId)||!RequiredSlots.Contains(hire.Slot)||Role(hire.Slot)!=card.Role||!state.Commands.Any(c=>c.CommandId==hire.CommandId)||state.Ledger.Count(e=>e.TeamId==hire.TeamId&&e.CommandId==hire.CommandId&&e.PersonId==card.Id&&e.Credits==-card.Price&&e.Reason=="personnel-hire")!=1)throw new ArgumentException("Hire and debit must agree.");
  }
  if(state.Offer is {} offer&&(offer.PersonIds.Length is <2 or >3||!state.TeamIds.Contains(offer.TeamId)||offer.PersonIds.Distinct().Count()!=offer.PersonIds.Length||offer.PersonIds.Any(id=>!state.Pool.People.Any(p=>p.Id==id&&p.Role==Role(offer.Slot))||state.Hires.Any(h=>h.PersonId==id))))throw new ArgumentException("Invalid reserved offers.");
  if(state.Offer is {} affordableOffer&&!affordableOffer.PersonIds.Any(id=>HireUnavailable(state,affordableOffer.TeamId,id) is null))throw new ArgumentException("Saved offers contain no affordable completion choice. Preserve the frozen campaign.");
 }
}
