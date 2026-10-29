using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
namespace Game.Domain;

public sealed record RacePackageTemplate(string Id,string Name,string Version,int BaseCost,
 ImmutableDictionary<string,double> Strengths,ImmutableDictionary<string,double> Compromises,string Applicability);
public sealed record PackageDevelopmentQuote(string Id,string Version,RacePackageTemplate Template,int Cost,
 DevelopmentPersonnel Personnel,string OutcomeVersion,string Timing,string OutcomeRange);
public sealed record PackageDevelopmentOutcome(string ProjectId,string TeamId,string Kind,string ResolvedEventId,
 string? CompletionEventId,PackageDevelopmentQuote Contract,RacePackageVersion? Package);

/// <summary>Versioned package creation contracts share the existing opportunity and credits.</summary>
public static class PackageDevelopment
{
 public const string Version="package-development-v1";
 public const string OutcomeVersion="package-normal-outcome-v1";
 public const string TemplateVersion="package-templates-v1/provisional-2026.10";
 private static ImmutableDictionary<string,double> Changes(params (string Key,double Value)[] changes)=>changes.ToImmutableDictionary(p=>p.Key,p=>p.Value);
 public static readonly ImmutableArray<RacePackageTemplate> Templates=[
  new("low-drag","Low-drag package",TemplateVersion,18,Changes(("topSpeed",4),("accelerationDeployment",2)),Changes(("highSpeedAero",-2),("lowSpeedAero",-1)),"Straight performance focus; exchanges aero support for speed. Available at every circuit."),
  new("downforce","Downforce package",TemplateVersion,18,Changes(("highSpeedAero",4),("lowSpeedAero",3)),Changes(("topSpeed",-3)),"Cornering focus; exchanges straight performance for aero support. Available at every circuit."),
  new("tyre-management","Tyre-management package",TemplateVersion,14,Changes(("tyreWear",4),("tyreTemperature",2)),Changes(("traction",-2)),"Stint management focus; exchanges traction for tyre support. Available at every circuit."),
  new("cooling-reliability","Cooling / reliability package",TemplateVersion,14,Changes(("reliability",4),("tyreTemperature",2)),Changes(("accelerationDeployment",-2)),"Resilience and temperature focus; exchanges deployment performance. Reliability uses the existing incident policy and is inactive without it. Available at every circuit."),
  new("mechanical-grip","Mechanical-grip package",TemplateVersion,16,Changes(("mechanicalGrip",4),("traction",2)),Changes(("topSpeed",-2)),"Low-speed handling focus; exchanges straight performance for grip. Available at every circuit.")];
 public static PackageDevelopmentQuote Quote(string templateId,DevelopmentPersonnel personnel)
 {
  var template=Templates.SingleOrDefault(t=>t.Id==templateId)??throw new ArgumentException("Choose a supported Race Package template.");
  if(string.IsNullOrWhiteSpace(personnel.RosterVersion)||new[]{personnel.ManagerEfficiency,personnel.ManagerDevelopment,personnel.TechnicalEfficiency,personnel.TechnicalQuality}.Any(v=>v is <0 or >100))throw new ArgumentException("Invalid frozen package personnel inputs.");
  var cost=Math.Max(8,template.BaseCost-Math.Min(3,(personnel.ManagerEfficiency+personnel.TechnicalEfficiency)/80));
  const string timing="Pending until preparation close; a normal outcome completes for the intended event. No additional payment.";
  const string range="Normal creation: the printed strengths and compromises. One selected package only; entries never stack. Per-attribute strength cap +6, compromise floor -3; composed ratings remain 1–100. Repeated creation adds an alternative, not a cumulative gain. Manager Efficiency and Technical Development Efficiency discount at most 3 credits (cost floor 8). Manager Development and matched Technical quality are saved; normal-only v1 has no quality roll.";
  var basis=JsonSerializer.SerializeToUtf8Bytes(new{Version,Template=template.Id,TemplateVersion=template.Version,template.BaseCost,Strengths=template.Strengths.OrderBy(p=>p.Key,StringComparer.Ordinal).ToArray(),Compromises=template.Compromises.OrderBy(p=>p.Key,StringComparer.Ordinal).ToArray(),template.Applicability,Cost=cost,Personnel=personnel,OutcomeVersion,Timing=timing,OutcomeRange=range});
  return new(Convert.ToHexString(SHA256.HashData(basis)),Version,template,cost,personnel,OutcomeVersion,timing,range);
 }
 public static void ValidateQuote(PackageDevelopmentQuote quote)
 {
  var expected=Quote(quote.Template.Id,quote.Personnel);
  if(quote.Id!=expected.Id||quote.Version!=Version||quote.OutcomeVersion!=OutcomeVersion||quote.Cost!=expected.Cost||quote.Timing!=expected.Timing||quote.OutcomeRange!=expected.OutcomeRange
   ||quote.Template.Version!=expected.Template.Version||quote.Template.Name!=expected.Template.Name||quote.Template.BaseCost!=expected.Template.BaseCost||quote.Template.Applicability!=expected.Template.Applicability
   ||!quote.Template.Strengths.OrderBy(p=>p.Key).SequenceEqual(expected.Template.Strengths.OrderBy(p=>p.Key))||!quote.Template.Compromises.OrderBy(p=>p.Key).SequenceEqual(expected.Template.Compromises.OrderBy(p=>p.Key)))throw new ArgumentException("Saved package quote differs from its versioned contract.");
 }
 internal static ImmutableArray<PackageDevelopmentOutcome> Resolve(DevelopmentState state,DevelopmentOpportunity opportunity)
 {
  if(!opportunity.Decisions.Any(d=>d.PackageQuote is not null))return state.PackageOutcomes;
  var outcomes=state.PackageOutcomes.IsDefault?ImmutableArray<PackageDevelopmentOutcome>.Empty:state.PackageOutcomes;
  foreach(var decision in opportunity.Decisions.Where(d=>d.PackageQuote is not null).OrderBy(d=>d.ProjectId,StringComparer.Ordinal))
  {
   var quote=decision.PackageQuote!;ValidateQuote(quote);
   var package=new RacePackageVersion(decision.ProjectId+"/package",1,quote.Template.Name,"race-package-v1",quote.Template.Strengths,quote.Template.Compromises,[],quote.Template.Applicability,quote.Version+"/"+quote.Template.Version+"/"+decision.ProjectId,[]);
   outcomes=outcomes.Add(new(decision.ProjectId,decision.TeamId,"normal",opportunity.TargetEventId,opportunity.TargetEventId,quote,package));
  }
  return outcomes;
 }
}
