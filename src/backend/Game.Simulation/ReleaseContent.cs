using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Game.Domain;

namespace Game.Simulation;

public sealed record ContentConstructor(string TeamId,string Id,string Name,ImmutableArray<string> DriverIds);
public sealed record ContentAsset(string Id,string EntityId,string? RuntimeUrl,string Provenance,string? OriginalPath=null);
public sealed record ContentSource(string Id,string SchemaVersion,string DataVersion);
/// <summary>Materialised existing domain inputs. Identity/source metadata never supplies performance coefficients.</summary>
public sealed record ReleaseContentPack(string Version,string CalendarVersion,int Season,string Provenance,
 ImmutableArray<string> Calendar,ImmutableArray<ContentConstructor> Constructors,ImmutableArray<CircuitPreset> Circuits,
 PersonnelPool Personnel,ConstructionCatalogue Construction,ImmutableArray<RacePackageVersion> Packages,
 ImmutableArray<ContentAsset> Assets,ImmutableArray<ContentSource> Sources,ImmutableArray<string> ActiveConsumers,
 string LengthUnit="metres",string TimeUnit="seconds",ImmutableArray<string> AttributeGroups=default,
 [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] ImmutableArray<PackageDevelopmentQuote> PaidPackageOptions=default,
 [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] ImmutableArray<ContentPersonnelDetail> PersonnelDetails=default);

public static partial class ReleaseContent
{
 public const string PilotVersion="release-2026-pilot-v1";
 public static ReleaseContentPack Pilot()
 {
  string[][] teams=[
   ["mclaren","McLaren","lando-norris","oscar-piastri"],["ferrari","Ferrari","charles-leclerc","lewis-hamilton"],
   ["red-bull-racing","Red Bull Racing","max-verstappen","isack-hadjar"],["mercedes","Mercedes","george-russell","andrea-kimi-antonelli"],
   ["aston-martin","Aston Martin","fernando-alonso","lance-stroll"],["alpine","Alpine","pierre-gasly","franco-colapinto"],
   ["haas","Haas","esteban-ocon","oliver-bearman"],["racing-bulls","Racing Bulls","liam-lawson","arvid-lindblad"],
   ["williams","Williams","alexander-albon","carlos-sainz"],["audi","Audi","nico-hulkenberg","gabriel-bortoleto"],
   ["cadillac","Cadillac","sergio-perez","valtteri-bottas"]];
  var constructors=teams.Select((t,i)=>new ContentConstructor($"team-{i+1:00}",t[0],t[1],["driver-"+t[2],"driver-"+t[3]])).ToImmutableArray();
  var circuits=new[]{"track-monza","track-monaco"}.Select(id=>WeekendPresetCatalog.Open(id).Circuit).ToImmutableArray();
  var pool=PersonnelCatalog.Open();
  var assets=circuits.Select(c=>new ContentAsset("asset-"+c.Id,c.Id,"/tracks/"+c.Slug+"-960.webp","User-supplied track asset; no geographic or rights claim.","assets/track images/"+c.Slug+".png"))
   .Concat(pool.People.Select(p=>new ContentAsset("asset-"+p.Id,p.Id,p.Driver is {} driver?"/portraits/"+driver.Slug+"-384.webp":null,"User-supplied portrait mapped by reviewed portrait manifest; fictional staff use readable neutral fallback.",p.Driver is {} portrait?"assets/driver images/"+portrait.Slug+".png":null)))
   .Concat(constructors.Select(c=>new ContentAsset("asset-constructor-"+c.Id,c.Id,null,"Readable constructor identity; no crest implied.")))
   .Concat(ConstructionCatalog.Open().Categories.SelectMany(c=>c.Options).Select(o=>new ContentAsset("asset-"+o.Id,o.Id,null,"Readable authored construction option; neutral fallback.")))
   .Append(new ContentAsset("asset-neutral-package-v1","neutral-package-v1",null,"Readable neutral package; no strengths or compromises.")).ToImmutableArray();
  return new(PilotVersion,"reviewed-representative-monza-monaco-v1",2026,
   "2026 season-opening contracted roster; official calendar/identity snapshot accessed 2026-10-30. See .scratch/full-calendar-content/research/provenance.md. Two-event representative order is authored, not championship order; all ratings/prices/geometry remain game hypotheses.",
   circuits.Select(c=>c.Id).ToImmutableArray(),constructors,circuits,pool,ConstructionCatalog.Open(),[RacePackages.Baseline],assets,
   [new("drivers","1.0.0","2026.10.08-initial-balance"),new("tracks","1.0.0","2026.10.08-initial-balance"),new("car-attributes","1.0.0","2026.10.08-initial-model"),new("construction","1.0.0","2026.10.09-preseason-car-build-v1")],
   SupportedConsumers,AttributeGroups:["Speed","Cornering","Mechanical Grip","Tyres","Reliability"]);
 }
 public static ImmutableArray<string> SupportedConsumers=>WeekendPreparation.CarKeys.Select(k=>"car."+k)
  .Concat(WeekendPreparation.DriverKeys.Select(k=>"driver."+k)).Concat(WeekendPreparation.DemandKeys.Select(k=>"circuit."+k))
  .Concat(["engineer.Setup","manager.Efficiency","manager.Development","technical.Development Efficiency","technical.Aerodynamics","technical.Power Systems","technical.Mechanical / Chassis","technical.Reliability"]).ToImmutableArray();
}
