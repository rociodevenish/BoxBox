using System.Collections.Immutable;
namespace Game.Simulation;
// Deliberately incapable of holding a future pool, opponent choices, private budgets or seed.
public record PersonnelPolicyOffer(string PersonId,string Name,int Price,bool CanHire,int ReserveAfterHire);
public record PersonnelPolicyObservation(string Slot,int Balance,int CompletionReserve,ImmutableArray<PersonnelPolicyOffer> Offers);
public record PersonnelRecommendation(string Version,string PersonId,string Reason);
public static class PersonnelDecisionPolicy
{
 public const string Version="offer-only-low-cost-v1";
 public static PersonnelRecommendation Evaluate(PersonnelPolicyObservation observation)
 {
  var choice=observation.Offers.Where(p=>p.CanHire&&observation.Balance-p.Price>=p.ReserveAfterHire).OrderBy(p=>p.Price).ThenBy(p=>p.PersonId,StringComparer.Ordinal).FirstOrDefault()
   ??throw new ArgumentException("No legal offered hire preserves the completion reserve.");
  return new(Version,choice.PersonId,$"{choice.Name} is the lowest priced legal offer, keeping at least {choice.ReserveAfterHire} credits for the completion reserve. Prices and ratings are provisional; compare preferences separately.");
 }
}
