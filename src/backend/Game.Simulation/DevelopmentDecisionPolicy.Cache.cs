using System.Collections.Immutable;
using Game.Domain;
using System.Security.Cryptography;
using System.Text.Json;
namespace Game.Simulation;

public static partial class DevelopmentDecisionPolicy
{
 // Only pure results are retained. Saved record, ledger, roster, receipt and package-context
 // integrity checks remain the caller's responsibility and still run on every recovery.
 // A key covers serialized observation CONTENT, including every nested frozen quote/version.
 // The FIFO holds at most 1024 small digests/recommendations, never observation object graphs.
 private const int MaximumCachedRecommendations=1024;
 private static readonly object CacheGate=new();
 private static readonly Dictionary<string,DevelopmentRecommendation> Recommendations=new(StringComparer.Ordinal);
 private static readonly Queue<string> RecommendationOrder=new();
 public static DevelopmentRecommendation Evaluate(DevelopmentPolicyObservation observation)
 {
  string key;
  try
  {
   if(!HasSerializableSemantics(observation))return EvaluateUncached(observation);
   using var hash=SHA256.Create();
   using(var stream=new CryptoStream(Stream.Null,hash,CryptoStreamMode.Write))JsonSerializer.Serialize(stream,observation);
   key=Convert.ToHexString(hash.Hash!);
  }
  catch(Exception error) when(error is JsonException or ArgumentException or InvalidOperationException or NotSupportedException or NullReferenceException)
  {
   // Preserve the policy's historical validation/error behavior for unserializable invalid
   // inputs (for example nonfinite demands); errors are never entered into the cache.
   return EvaluateUncached(observation);
  }
  lock(CacheGate)if(Recommendations.TryGetValue(key,out var cached))return cached;
  var recommendation=EvaluateUncached(observation);
  lock(CacheGate)
  {
   if(Recommendations.TryAdd(key,recommendation))
   {
    RecommendationOrder.Enqueue(key);
    if(RecommendationOrder.Count>MaximumCachedRecommendations)Recommendations.Remove(RecommendationOrder.Dequeue());
   }
  }
  return recommendation;
 }
 // JSON does not preserve custom dictionary comparers. Such caller inputs retain the exact
 // uncached behavior; only ordinary ordinal/default dictionaries enter the content cache.
 private static bool HasSerializableSemantics(DevelopmentPolicyObservation observation)
 {
  static bool Plain(ImmutableDictionary<string,double>? values)=>values is null||
   ((ReferenceEquals(values.KeyComparer,EqualityComparer<string>.Default)||ReferenceEquals(values.KeyComparer,StringComparer.Ordinal))
    &&ReferenceEquals(values.ValueComparer,EqualityComparer<double>.Default));
  static bool Package(RacePackageVersion? package)=>package is null||Plain(package.Strengths)&&Plain(package.Compromises);
  return Plain(observation.CircuitRelevance)&&Plain(observation.GainCapacity)&&Plain(observation.CircuitDemands)&&Plain(observation.BaseRatings)
   &&(observation.UpcomingCircuits.IsDefault||observation.UpcomingCircuits.All(Plain))
   &&(observation.Library.IsDefault||observation.Library.All(Package))
   &&(observation.PackageOptions.IsDefault||observation.PackageOptions.All(q=>Plain(q.Template.Strengths)&&Plain(q.Template.Compromises)&&Package(q.SourcePackage)));
 }}
