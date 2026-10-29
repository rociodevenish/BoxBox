using System.Collections.Immutable;
using Game.Domain;
namespace Game.Simulation;
/// <summary>Selection is free and materialised only by preparation; source car values remain unchanged.</summary>
public static class RacePackages
{
 public const string Version="race-package-v1";
 public static RacePackageVersion Baseline=>new("neutral-package-v1",1,"Neutral baseline",Version,
  ImmutableDictionary<string,double>.Empty,ImmutableDictionary<string,double>.Empty,[],
  "All circuits; no strengths or compromises.","explicit-neutral-preset-migration-v1",[]);
 public static WeekendPreset Migrate(WeekendPreset source)
 {
  if(!source.PackageLibrary.IsDefaultOrEmpty)
  {
   _=Selected(source);return source;
  }
  if(source.PackageId!="neutral-package-v1"||source.SelectedPackage is not null)
   throw new ArgumentException("Unknown legacy package context; explicitly select a completed baseline fallback.");
  return source with{PackageLibrary=[Baseline],SelectedPackage=new("neutral-package-v1",1)};
 }
 public static RacePackageVersion Selected(WeekendPreset source)
 {
  var selection=source.SelectedPackage??throw new ArgumentException("Select exactly one completed package version.");
  if(source.PackageLibrary.IsDefaultOrEmpty||source.PackageLibrary.GroupBy(p=>(p.PackageId,p.Version)).Any(g=>g.Count()!=1))
   throw new ArgumentException("A versioned package library is required without duplicate identities.");
  var entry=source.PackageLibrary.SingleOrDefault(p=>p.PackageId==selection.PackageId&&p.Version==selection.Version);
  if(entry is null||!entry.Completed||entry.ModelVersion!=Version||source.PackageId!=selection.PackageId)
   throw new ArgumentException("Select one available completed package version or explicitly select the neutral baseline.");
  if(entry.Version<1||string.IsNullOrWhiteSpace(entry.PackageId)||string.IsNullOrWhiteSpace(entry.Provenance)
   ||entry.Strengths.Any(p=>!WeekendPreparation.CarKeys.Contains(p.Key)||!double.IsFinite(p.Value)||p.Value is <0 or >6)
   ||entry.Compromises.Any(p=>!WeekendPreparation.CarKeys.Contains(p.Key)||!double.IsFinite(p.Value)||p.Value is >0 or < -6)
   ||(!entry.ApplicableCircuits.IsDefaultOrEmpty&&!entry.ApplicableCircuits.Contains(source.Circuit.Id)))
   throw new ArgumentException("Unsupported package attributes, bounds or circuit applicability.");
  return entry;
 }
 public static WeekendPreset Select(WeekendPreset source,RacePackageSelection selection)
 {
  if(source.PackageLibrary.IsDefaultOrEmpty)source=Migrate(source);
  var selected=source with{SelectedPackage=selection,PackageId=selection.PackageId};
  _=Selected(selected);return selected;
 }
 public static ImmutableDictionary<string,double> Apply(WeekendPreset source,ImmutableDictionary<string,double> ratings)
 {
  var entry=Selected(Migrate(source));
  return ratings.ToImmutableDictionary(p=>p.Key,p=>Math.Clamp(p.Value+entry.Strengths.GetValueOrDefault(p.Key)+entry.Compromises.GetValueOrDefault(p.Key),1,100));
 }
}
