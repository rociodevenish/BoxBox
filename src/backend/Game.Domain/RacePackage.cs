using System.Collections.Immutable;
namespace Game.Domain;
public sealed record RacePackageSelection(string PackageId,int Version);
/// <summary>An immutable completed specification. Uncompleted work is never a library entry.</summary>
public sealed record RacePackageVersion(string PackageId,int Version,string Name,string ModelVersion,
 ImmutableDictionary<string,double> Strengths,ImmutableDictionary<string,double> Compromises,
 ImmutableArray<string> ApplicableCircuits,string Applicability,string Provenance,
 ImmutableArray<string> RefinementHistory,bool Completed=true);
