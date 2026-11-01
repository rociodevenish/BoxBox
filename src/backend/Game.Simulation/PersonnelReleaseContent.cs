using System.Collections.Immutable;
using Game.Domain;
namespace Game.Simulation;

/// <summary>Designer provenance and truthful card meanings; never additional simulation inputs.</summary>
public sealed record ContentPersonnelDetail(string PersonId,string Role,string IdentityKind,string ReferenceRole,string Provenance,
 ImmutableArray<string> ImplementedAttributes,ImmutableArray<string> InactiveAttributes,string RatingScale,string PreferenceMeaning,string EffectScope);
public static partial class ReleaseContent
{
 public const string PersonnelVersion="release-2026-personnel-rc1";
 public static ReleaseContentPack PersonnelCandidate()
 {
  var original=Pilot();
  string[] reserves=["driver-yuki-tsunoda","driver-patricio-oward","driver-zhou-guanyu","driver-stoffel-vandoorne","driver-antonio-giovinazzi","driver-jack-doohan"];
  var pool=original.Personnel with{Version=original.Personnel.Version+"/s16-personnel-rc1",People=original.Personnel.People.Select(p=>p with{EffectLabel=p.Driver is null?"Fictional staff. "+p.EffectLabel:(reserves.Contains(p.Id)?"2026 reserve reference; available independently in Personnel Acquisition. ":"2026 contracted race-driver reference; available independently in Personnel Acquisition. ")+p.EffectLabel}).ToImmutableArray()};
  var details=pool.People.Select(p=>new ContentPersonnelDetail(p.Id,p.Role,p.Driver is null?"fictional-staff":"real-driver",p.Driver is null?p.Role:reserves.Contains(p.Id)?"2026 reserve":"2026 contracted race driver",
   p.Driver is null?"Authored fictional identity and game ratings; no real-world staff employment claim.":"Dated primary-source identity/role provenance in .scratch/full-calendar-content/research/provenance.md; ratings and preferences are game hypotheses, not official rankings.",
   p.Driver is not null?WeekendPreparation.DriverKeys:p.Role switch{"Manager"=>["Efficiency","Development"],"Race Engineer"=>["Setup"],_=>["Aerodynamics","Mechanical / Chassis","Power Systems","Reliability","Development Efficiency"]},
   p.Role switch{"Manager"=>["Leadership","Operations"],"Race Engineer"=>["Tyre Reading","Strategy","Communication"],_=>[]},"Detailed 1–100 game scores; higher is more capable. Prices use the retained provisional-prices-v1 fixed-credit model; no headline-star conversion.",
   p.Driver is null?"Not a driver compatibility trait.":"Preferences describe car-behaviour compatibility separately from quality; Adaptability moderates mismatch.",
   p.Driver is not null?"Implemented capabilities are phase/condition dependent: Starts applies to the automatic standing start, wet skill to wet running, feedback to practice, and Adaptability to compatibility. No unconditional raw pace bonus.":p.Role=="Race Engineer"?"Setup affects assigned-driver Practice Assessment uncertainty only; no race pace or error bonus.":"Efficiency and quality affect paid development contracts only; quality is bounded and technical specialties must match the project. No raw pace, extra income or development-speed bonus.")).ToImmutableArray();
  var staff=new DevelopmentPersonnel("preset-development-neutral-v1",0,0,0,0);
  var paid=PackageDevelopment.Templates.SelectMany(t=>{var q=PackageDevelopment.QuoteRich(t.Id,staff,DevelopmentRiskControls.Default);return new[]{q,PackageDevelopment.Rush(q)};}).ToImmutableArray();
  return original with{Version=PersonnelVersion,Personnel=pool,PersonnelDetails=details,PaidPackageOptions=paid,
   Assets=original.Assets.Concat(PackageDevelopment.Templates.Select(t=>new ContentAsset("asset-package-template-"+t.Id,t.Id,null,"Authored paid Race Package option; readable fallback, no image or rights claim."))).ToImmutableArray(),
   Provenance=original.Provenance+" Personnel candidate rc1 retains 28 real drivers and 99 uniquely identified fictional staff, existing fixed prices, 100 starting credits and 18-credit minimum car reserve. Paid package quotes are neutral-staff reference options, not initial ownership; actual quotes use frozen acquired personnel. Pit Crew remains a team attribute."};
 }
}
