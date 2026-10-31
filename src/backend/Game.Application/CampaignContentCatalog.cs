using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;

/// <summary>Deployment content selection. Campaigns retain materialised inputs rather than reopening this catalogue on recovery.</summary>
public sealed class CampaignContentCatalog
{
 private readonly ImmutableDictionary<string,ReleaseContentPack> packs;
 public CampaignContentCatalog():this([ReleaseContent.Pilot(),ReleaseContent.PersonnelCandidate()]){}
 public CampaignContentCatalog(IEnumerable<ReleaseContentPack> candidates)
 {
  // Copy the only collection with a mutable interface; all other nested pack data use immutable collections.
  packs=candidates.Select(p=>p with{Circuits=p.Circuits.Select(c=>c with{Track=c.Track with{Segments=c.Track.Segments is null?null:Array.AsReadOnly(c.Track.Segments.ToArray())}}).ToImmutableArray()}).ToImmutableDictionary(p=>p.Version);
 }
 public ReleaseContentPack Resolve(string version,int laps)
 {
  if(!packs.TryGetValue(version,out var pack))throw new SessionFault(SessionFailure.InvalidInput,"Unsupported content version. Choose a compatible reviewed content pack.");
  try{Validate(pack,laps);return pack;}catch(Exception e) when(e is ArgumentException or InvalidOperationException or KeyNotFoundException or NullReferenceException or IndexOutOfRangeException)
  {throw new SessionFault(SessionFailure.InvalidInput,"Content pack is invalid; no campaign was created. Review references, supported consumers, units and bounds.");}
 }
 private static void Validate(ReleaseContentPack pack,int laps)
 {
  static void Require(bool valid){if(!valid)throw new ArgumentException("Invalid release content.");}
  Require((pack.Version is ReleaseContent.PilotVersion or ReleaseContent.PersonnelVersion)&&pack.Season==2026&&!string.IsNullOrWhiteSpace(pack.Provenance)&&!string.IsNullOrWhiteSpace(pack.CalendarVersion)&&pack.LengthUnit=="metres"&&pack.TimeUnit=="seconds");
  Require(pack.Constructors.Length==11&&pack.Constructors.Select(t=>t.TeamId).SequenceEqual(Enumerable.Range(1,11).Select(i=>$"team-{i:00}"))&&pack.Constructors.Select(t=>t.Id).Distinct().Count()==11);
  Require(SessionCoordinator.SupportsCampaignConstructors(pack.Constructors));
  Require(pack.Constructors.All(t=>!string.IsNullOrWhiteSpace(t.Id)&&!string.IsNullOrWhiteSpace(t.Name)&&t.DriverIds.Length==2)&&pack.Constructors.SelectMany(t=>t.DriverIds).Distinct().Count()==22);
  CampaignRules.ValidateShortCalendar(pack.Calendar);
  Require(pack.Circuits.Select(c=>c.Id).Distinct().Count()==pack.Circuits.Length&&pack.Calendar.All(id=>pack.Circuits.Any(c=>c.Id==id)));
  Require(pack.Sources.Length==4&&pack.Sources.Select(s=>s.Id).ToHashSet().SetEquals(["drivers","tracks","car-attributes","construction"])&&pack.Sources.All(s=>s.SchemaVersion=="1.0.0"&&!string.IsNullOrWhiteSpace(s.DataVersion)));
  Require(pack.ActiveConsumers.Distinct().Count()==pack.ActiveConsumers.Length&&pack.ActiveConsumers.ToHashSet().SetEquals(ReleaseContent.SupportedConsumers));
  var sourceVersions=pack.Sources.ToDictionary(s=>s.Id,s=>s.SchemaVersion+"/"+s.DataVersion);
  Require(sourceVersions["car-attributes"]==WeekendPreparation.CarVersion["car-spec/".Length..]&&sourceVersions["construction"]==pack.Construction.SourceVersion&&pack.Circuits.All(c=>c.Version==sourceVersions["tracks"])&&pack.Personnel.People.Where(p=>p.Driver is not null).All(p=>p.Driver!.Version==sourceVersions["drivers"]));
  if(pack.Version==ReleaseContent.PersonnelVersion||!pack.PersonnelDetails.IsDefault||!pack.PaidPackageOptions.IsDefault)ReleasePersonnel.Validate(pack.PersonnelDetails,pack.PaidPackageOptions,pack.Packages,pack.Personnel);
  PersonnelAcquisition.ValidatePool(pack.Personnel,11);CarConstruction.ValidateCatalogue(pack.Construction);
  var groupPreview=ConstructionPreview.Preview(pack.Construction,pack.Construction.Categories.Select(c=>c.Options[0].Id).ToImmutableArray(),WeekendPresetCatalog.Open("track-monza"),pack.Personnel.StartingCredit);
  Require(!pack.AttributeGroups.IsDefaultOrEmpty&&pack.AttributeGroups.Distinct().Count()==pack.AttributeGroups.Length&&pack.AttributeGroups.ToHashSet().SetEquals(groupPreview.Groups.Keys));
  foreach(var person in pack.Personnel.People)
  {
   if(person.Driver is {} driver)
   {
    var neutral=WeekendPresetCatalog.Open("track-monza") with{Circuit=pack.Circuits[0]};
    _=WeekendPreparation.Prepare(neutral with{Cars=neutral.Cars.SetItem(0,neutral.Cars[0] with{Driver=driver})},laps,new());
    Require(person.SourceVersion==driver.Version&&driver.Preferences.All(p=>!string.IsNullOrWhiteSpace(p.Key)&&!string.IsNullOrWhiteSpace(p.Label)));
   }
   else
   {
    string[] supported=person.Role switch
    {
     "Manager"=>["Leadership","Operations","Development","Efficiency"],
     "Race Engineer"=>["Setup","Tyre Reading","Strategy","Communication"],
     "Technical Engineer"=>["Aerodynamics","Mechanical / Chassis","Power Systems","Reliability","Development Efficiency"],
     _=>throw new ArgumentException("Unsupported personnel role.")
    };
    Require(person.Attributes!.Keys.ToHashSet().SetEquals(supported));
   }
  }
  Require(pack.Personnel.ConstructionMapping!.SourceVersion==pack.Construction.SourceVersion&&pack.Packages.Length>0&&pack.Packages.Select(p=>(p.PackageId,p.Version)).Distinct().Count()==pack.Packages.Length);
  foreach(var package in pack.Packages)
  {
   Require(package.ApplicableCircuits.IsDefaultOrEmpty||package.ApplicableCircuits.All(id=>pack.Circuits.Any(c=>c.Id==id)));
   var circuit=pack.Circuits.First(c=>package.ApplicableCircuits.IsDefaultOrEmpty||package.ApplicableCircuits.Contains(c.Id));
   var neutral=WeekendPresetCatalog.Open("track-monza") with{Circuit=circuit,PackageLibrary=pack.Packages};
   _=WeekendPreparation.Prepare(RacePackages.Select(neutral,new(package.PackageId,package.Version)),laps,new());
  }
  var entities=pack.Circuits.Select(c=>c.Id).Concat(pack.Personnel.People.Select(p=>p.Id)).Concat(pack.Constructors.Select(t=>t.Id)).Concat(pack.Construction.Categories.SelectMany(c=>c.Options).Select(o=>o.Id)).Concat(pack.Packages.Select(p=>p.PackageId)).Concat(pack.PaidPackageOptions.IsDefault?[]:pack.PaidPackageOptions.Select(p=>p.Template.Id)).ToHashSet();
  Require(pack.Assets.Select(a=>a.Id).Distinct().Count()==pack.Assets.Length&&pack.Assets.All(a=>entities.Contains(a.EntityId)&&!string.IsNullOrWhiteSpace(a.Id)&&!string.IsNullOrWhiteSpace(a.Provenance)&&(a.RuntimeUrl is null||a.RuntimeUrl.StartsWith('/')&&!a.RuntimeUrl.Contains(".."))));
  foreach(var circuit in pack.Circuits)
  {
   Require(!string.IsNullOrWhiteSpace(circuit.Id)&&!string.IsNullOrWhiteSpace(circuit.Name)&&!string.IsNullOrWhiteSpace(circuit.Slug));
   var track=circuit.Track;var segments=track.Segments;
   Require(segments is {Count:>0}&&segments.Select(s=>s.Id).Distinct().Count()==segments.Count);
   foreach(var segment in segments!)segment.Validate();
   Require(Math.Abs(segments.Sum(s=>s.BaselineTime)-track.BaseLapTime)<.000001);
   // Supplied circuit lengths are kilometres; the packaged domain catalogue converts them once to metres.
   // A positive kilometre-valued number labelled metres must not pass the units check.
   var sourceLength=WeekendPresetCatalog.Open(circuit.Id).Circuit.Track.Segments!.Sum(s=>s.Length);
   Require(Math.Abs(segments.Sum(s=>s.Length)-sourceLength)<.001);
   foreach(var team in pack.Constructors)
   {
    var cars=team.DriverIds.Select((id,index)=>new WeekendCar($"{team.TeamId}-car-{index+1}",team.TeamId,
     new("neutral-car-v1",WeekendPreparation.CarVersion,WeekendPreparation.CarKeys.ToImmutableDictionary(k=>k,_=>82d)),
     pack.Personnel.People.Single(p=>p.Id==id&&p.Role=="Driver").Driver!)).ToImmutableArray();
    var preset=new WeekendPreset(WeekendPreparation.ModelVersion,circuit,cars,new("engineer-neutral-v1","Preset Race Engineer","authored-engineer-v1",82),PackageLibrary:pack.Packages,SelectedPackage:new("neutral-package-v1",1));
    var weekend=WeekendPreparation.Prepare(preset,laps,new());
    var engine=WeekendPreparation.CreateRace(weekend,0);engine.ResolvePhase();
    Require(engine.GetState().Time>0&&double.IsFinite(engine.GetState().Time));
   }
  }
 }
}
