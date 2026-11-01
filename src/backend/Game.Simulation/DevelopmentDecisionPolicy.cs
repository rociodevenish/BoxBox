using System.Collections.Immutable;
using Game.Domain;
using System.Text.Json.Serialization;
namespace Game.Simulation;

// Own saved quotes include frozen roster inputs. No seed, entropy, opponent or future draw can enter this observation.
public sealed record DevelopmentPolicyObservation(int Balance,int RemainingOpportunities,ImmutableArray<DevelopmentQuote> Options,ImmutableDictionary<string,double> CircuitRelevance,ImmutableDictionary<string,double> GainCapacity,
 [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] ImmutableArray<PackageDevelopmentQuote> PackageOptions=default,
 [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] ImmutableArray<RacePackageVersion> Library=default,
 [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] ImmutableDictionary<string,double>? CircuitDemands=null,
 [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] ImmutableDictionary<string,double>? BaseRatings=null,
 [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] ImmutableArray<ImmutableDictionary<string,double>> UpcomingCircuits=default,
 [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] bool IncidentsEnabled=false);
public sealed record DevelopmentRecommendation(string Version,string ActionKind,string? OptionId,string? QuoteId,int Cost,int RemainingCredits,int FutureReserve,int AffordableAlternatives,string Explanation,
 [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] RacePackageSelection? Package=null,
 [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string? Refinement=null,
 [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] bool Rush=false);
public static partial class DevelopmentDecisionPolicy
{
 public const string Version="public-demand-credit-reserve-v1";
 public static DevelopmentRecommendation Evaluate(DevelopmentPolicyObservation observation)
 {
  if(observation.Balance<0||observation.RemainingOpportunities<0||observation.Options.IsDefaultOrEmpty||observation.Options.Select(q=>q.Option.Id).Distinct().Count()!=observation.Options.Length)throw new ArgumentException("Observe the team's actual balance, remaining opportunities and distinct frozen quotes.");
  foreach(var quote in observation.Options)
   if(PermanentDevelopment.Quote(quote.Option.Id,quote.Personnel,quote.Controls,quote.Risk)!=quote||!observation.CircuitRelevance.TryGetValue(quote.Option.Id,out var demand)||!double.IsFinite(demand)||demand is <0 or >100||!observation.GainCapacity.TryGetValue(quote.Option.Id,out var capacity)||!double.IsFinite(capacity)||capacity is <0 or >6)throw new ArgumentException("Observe valid public circuit demands and bounded own gain capacity.");
  if(!observation.PackageOptions.IsDefault)return EvaluateAlternatives(observation);
  var useful=observation.Options.Where(q=>observation.GainCapacity[q.Option.Id]>0).ToArray();
  var reserve=observation.RemainingOpportunities>0&&useful.Length>0?useful.Min(q=>q.Cost):0;
  var affordable=useful.Where(q=>q.Cost<=observation.Balance).ToArray();
  var chosen=affordable.Where(q=>observation.Balance-q.Cost>=reserve).OrderByDescending(q=>observation.CircuitRelevance[q.Option.Id]).ThenBy(q=>q.Cost).ThenBy(q=>q.Option.Id,StringComparer.Ordinal).FirstOrDefault();
  var reserveReason=observation.RemainingOpportunities>0?$"retaining {reserve} credits for another usable opportunity":"with no later development opportunity in this short calendar";
  if(chosen is null)return new(Version,"Save",null,null,0,observation.Balance,reserve,affordable.Length,$"Save retains all {observation.Balance} credits. No useful refinement fits the available balance {reserveReason}. Saving is a complete decision; no project or gain is promised.");
  return new(Version,"PermanentRefinement",chosen.Option.Id,chosen.Id,chosen.Cost,observation.Balance-chosen.Cost,reserve,affordable.Length,$"Consider {chosen.Option.Name.ToLowerInvariant()} for {chosen.Cost} credits, retaining {observation.Balance-chosen.Cost}. It has the highest public next-circuit relevance among useful options {reserveReason}; equal relevance prefers lower cost, then stable option identity. Save keeps all {observation.Balance} credits instead. Outcomes and the quoted compromise remain uncertain; this is not a lap-time prediction.");
 }
}
