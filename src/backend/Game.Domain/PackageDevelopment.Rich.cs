using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
namespace Game.Domain;
public static partial class PackageDevelopment
{
 private static readonly ImmutableHashSet<string> SupportedAttributes=ImmutableHashSet.Create("topSpeed","accelerationDeployment","highSpeedAero","lowSpeedAero","mechanicalGrip","braking","traction","tyreWear","tyreTemperature","reliability");
 public const string RichVersion="package-development-v2";
 public const string RichOutcomeVersion="package-rich-outcome-v1";
 public static PackageDevelopmentQuote QuoteRich(string templateId,DevelopmentPersonnel personnel,DevelopmentRiskControls? risk=null)
 {
  var original=Quote(templateId,personnel);risk??=DevelopmentRiskControls.Default;var chances=risk.WithPersonnel(personnel);
  var range=chances.Range+" Normal: printed strengths/compromises; partial: half strength (minimum +1); breakthrough: +1 strength per attribute (cap +6); compromised: -1 strength (minimum +1) and Reliability -2, usually usable. Reliability affects only the existing incident policy and is inactive when that policy is disabled. Delay: normal specification available at the following preparation close, without repayment; beyond the calendar it stays pending. Quality transfers at most 10 percentage points from partial/compromised into normal; no extra hazard draw. All composed ratings remain 1–100.";
  var basis=JsonSerializer.SerializeToUtf8Bytes(new{RichVersion,RichOutcomeVersion,Base=original.Id,Risk=risk,Distribution=chances,Range=range});
  return original with{Id=Convert.ToHexString(SHA256.HashData(basis)),Version=RichVersion,OutcomeVersion=RichOutcomeVersion,OutcomeRange=range,Risk=risk,Distribution=chances,Timing="Resolves at preparation close; delay completes at the following preparation close without a second payment."};
 }
 public static PackageDevelopmentQuote Requote(PackageDevelopmentQuote quote,DevelopmentPersonnel personnel)
 {
  var standard=quote.Risk is null?Quote(quote.Template.Id,personnel):quote.SourcePackage is {} source?SavedRefinementQuote(source,quote.Refinement!,personnel,quote.Risk,quote.TargetVersion):QuoteRich(quote.Template.Id,personnel,quote.Risk);
  return quote.Rush is null?standard:Rush(standard);
 }
 private static void ValidateRichQuote(PackageDevelopmentQuote quote)
 {
  var expected=Requote(quote,quote.Personnel);
  // Template vocabulary and numeric contract are validated independently of dictionary reference equality.
  ValidateQuote(Quote(quote.Template.Id,quote.Personnel) with{Template=quote.Template});
  if(quote.Id!=expected.Id||quote.Version!=expected.Version||quote.OutcomeVersion!=expected.OutcomeVersion||quote.Cost!=expected.Cost||quote.Timing!=expected.Timing||quote.OutcomeRange!=expected.OutcomeRange||quote.Distribution!=expected.Distribution||quote.Refinement!=expected.Refinement||quote.TargetVersion!=expected.TargetVersion||quote.ProjectBaseCost!=expected.ProjectBaseCost||quote.Rush!=expected.Rush)throw new ArgumentException("Saved rich package quote is incompatible.");
 }
 private static RacePackageVersion Materialise(PackageDevelopmentQuote quote,string project,string kind)
 {
  if(quote.SourcePackage is not null)return Refine(quote,project,kind);
  var strengths=quote.Template.Strengths.ToImmutableDictionary(p=>p.Key,p=>kind switch{"partial"=>Math.Max(1,Math.Ceiling(p.Value/2)),"breakthrough"=>Math.Min(6,p.Value+1),"compromised"=>Math.Max(1,p.Value-1),_=>p.Value});
  var compromises=quote.Template.Compromises;
  if(kind=="compromised")compromises=compromises.SetItem("reliability",Math.Max(-6,compromises.GetValueOrDefault("reliability")-2));
  return new(project+"/package",1,quote.Template.Name,"race-package-v1",strengths,compromises,[],quote.Template.Applicability,quote.Version+"/"+quote.Template.Version+"/"+project,[]);
 }
 public static RacePackageVersion PreviewOutcome(PackageDevelopmentQuote quote,string kind)
 {
  ValidateQuote(quote);
  if(kind is not("normal" or "partial" or "breakthrough" or "compromised"))throw new ArgumentException("Preview a concrete completed outcome category.");
  return quote.Risk is null?new("preview",1,quote.Template.Name,"race-package-v1",quote.Template.Strengths,quote.Template.Compromises,[],quote.Template.Applicability,quote.Version,[]):Materialise(quote,"preview",kind);
 }
 public static PackageDevelopmentQuote QuoteRefinement(RacePackageVersion source,string refinement,DevelopmentPersonnel personnel,DevelopmentRiskControls? risk=null,int? targetVersion=null)
 {
  var quote=SavedRefinementQuote(source,refinement,personnel,risk,targetVersion);
  if(refinement=="broaden-usefulness")
  {
   var primary=source.Strengths.OrderByDescending(p=>p.Value).ThenBy(p=>p.Key,StringComparer.Ordinal).First().Key;
   var weakness=source.Compromises.Where(p=>p.Value<0).OrderBy(p=>p.Value).ThenBy(p=>p.Key,StringComparer.Ordinal).First().Key;
   if(primary==weakness)throw new ArgumentException("Broadening needs a different weakness from the strongest attribute; choose another refinement for this version.");
  }
  return quote;
 }
 // Reconstruct accepted v2/v3 contracts verbatim, including their original outcome semantics.
 // New-draft eligibility is deliberately separate so older paid projects still replay and retry.
 private static PackageDevelopmentQuote SavedRefinementQuote(RacePackageVersion source,string refinement,DevelopmentPersonnel personnel,DevelopmentRiskControls? risk,int? targetVersion)
 {
  if(!source.Completed||source.PackageId=="neutral-package-v1"||source.ModelVersion!="race-package-v1"||source.Version<1||source.Strengths.Count==0||source.Strengths.Any(p=>!SupportedAttributes.Contains(p.Key)||!double.IsFinite(p.Value)||p.Value is <=0 or >6)||source.Compromises.Any(p=>!SupportedAttributes.Contains(p.Key)||!double.IsFinite(p.Value)||p.Value is >0 or < -6))throw new ArgumentException("Refine a completed specialised package with supported bounded attributes.");
  if(refinement is not("strengthen" or "reduce-compromise" or "broaden-usefulness"))throw new ArgumentException("Choose a bounded refinement option.");
  var template=Templates.SingleOrDefault(t=>t.Name==source.Name)??throw new ArgumentException("Unsupported package source template.");
  var original=QuoteRich(template.Id,personnel,risk);var version=targetVersion??source.Version+1;
  if(version<=source.Version)throw new ArgumentException("Refinement requires a new immutable version.");
  const int baseCost=10;var cost=Math.Max(8,baseCost-Math.Min(3,(personnel.ManagerEfficiency+personnel.TechnicalEfficiency)/80));
  var description=refinement switch{"strengthen"=>"Increase the strongest attribute by +1 (+0.5 from source version 3), capped at +6.","reduce-compromise"=>"Reduce the largest negative compromise by 1 toward zero.",_=>"Broaden usefulness: add +1 against the largest weakness (strength cap +2 there) while surrendering 0.5 of the strongest attribute. All-circuit availability stays unchanged."};
  var range=original.Distribution!.Range+" "+description+" Partial and compromised use half the stated change; breakthrough doubles it within caps; compromised adds Reliability -1, bounded at -6; Reliability affects only the existing incident policy and is inactive when that policy is disabled. Delay retains the normal change for the following preparation close without repayment. This creates a new immutable version and never edits a frozen weekend.";
  var basis=JsonSerializer.SerializeToUtf8Bytes(new{Base=original.Id,source.PackageId,source.Version,source.Name,source.ModelVersion,Strengths=source.Strengths.OrderBy(p=>p.Key,StringComparer.Ordinal).ToArray(),Compromises=source.Compromises.OrderBy(p=>p.Key,StringComparer.Ordinal).ToArray(),source.Applicability,source.ApplicableCircuits,source.Provenance,source.RefinementHistory,Refinement=refinement,TargetVersion=version,Cost=cost,Range=range});
  var quote=original with{Id=Convert.ToHexString(SHA256.HashData(basis)),SourcePackage=source,Refinement=refinement,TargetVersion=version,ProjectBaseCost=baseCost,Cost=cost,OutcomeRange=range};
  _=Refine(quote,"preview","normal");return quote;
 }
 private static RacePackageVersion Refine(PackageDevelopmentQuote quote,string project,string kind)
 {
  var source=quote.SourcePackage!;var strengths=source.Strengths;var compromises=source.Compromises;
  var primary=strengths.OrderByDescending(p=>p.Value).ThenBy(p=>p.Key,StringComparer.Ordinal).First().Key;
  var factor=kind is "partial" or "compromised"?.5:kind=="breakthrough"?2:1;
  if(quote.Refinement=="strengthen")
  {
   if(strengths[primary]>=6)throw new ArgumentException("This source has reached its strength cap.");
   strengths=strengths.SetItem(primary,Math.Min(6,strengths[primary]+factor*(source.Version>=3?.5:1)));
  }
  else
  {
   var weakness=compromises.Where(p=>p.Value<0).OrderBy(p=>p.Value).ThenBy(p=>p.Key,StringComparer.Ordinal).FirstOrDefault();
   if(weakness.Key is null)throw new ArgumentException("This source has no compromise to reduce or broaden.");
   if(quote.Refinement=="reduce-compromise")compromises=compromises.SetItem(weakness.Key,Math.Min(0,weakness.Value+factor));
   else
   {
    if(strengths.GetValueOrDefault(weakness.Key)>=2||strengths[primary]<=1)throw new ArgumentException("This source has reached its broadening bounds.");
    strengths=strengths.SetItem(weakness.Key,Math.Min(2,strengths.GetValueOrDefault(weakness.Key)+factor)).SetItem(primary,Math.Max(1,strengths[primary]-.5*factor));
   }
  }
  if(kind=="compromised")compromises=compromises.SetItem("reliability",Math.Max(-6,compromises.GetValueOrDefault("reliability")-1));
  return source with{Version=quote.TargetVersion!.Value,Strengths=strengths,Compromises=compromises,Provenance=source.Provenance+"/"+quote.Version+"/"+project,RefinementHistory=[..source.RefinementHistory,project+"/source-v"+source.Version+"/"+quote.Refinement]};
 }
}
