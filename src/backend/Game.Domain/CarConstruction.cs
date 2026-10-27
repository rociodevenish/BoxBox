using System.Collections.Immutable;
namespace Game.Domain;

public sealed record ConstructionOption(string Id,string Name,int Cost,string Summary,ImmutableDictionary<string,double> Modifiers,ImmutableDictionary<string,double> HandlingProfile);
public sealed record ConstructionCategory(string Id,string Label,ImmutableArray<ConstructionOption> Options);
public sealed record ConstructionCatalogue(string Version,string SourceVersion,string MappingVersion,ImmutableDictionary<string,double> Baseline,ImmutableArray<ConstructionCategory> Categories);
public sealed record ConstructionReceipt(string CommandId,string Fingerprint);
public sealed record ConstructionDraft(string TeamId,long Revision,ImmutableArray<string> OptionIds,ImmutableArray<ConstructionReceipt> Commands);
public sealed record ConstructionState(string Version,ConstructionCatalogue Catalogue,ImmutableDictionary<string,ConstructionDraft> Drafts);
public sealed record ConstructionEdit(string CommandId,long Revision,string TeamId,string CatalogueVersion,ImmutableArray<string> OptionIds);

/// <summary>Free saved drafts over an equal-access frozen catalogue. Credits remain in the personnel ledger.</summary>
public static class CarConstruction
{
 public const string Version="car-construction-v1";
 public const string MappingVersion="bounded-detailed-deltas-v1";
 public static ConstructionState Open(ConstructionCatalogue catalogue,IReadOnlyList<string> teams)
 {
  ValidateCatalogue(catalogue);
  if(teams.Count==0||teams.Any(string.IsNullOrWhiteSpace)||teams.Distinct().Count()!=teams.Count)throw new ArgumentException("Choose distinct construction teams.");
  var baseline=catalogue.Categories.Select(c=>c.Options.OrderBy(o=>o.Cost).ThenBy(o=>o.Id,StringComparer.Ordinal).First().Id).ToImmutableArray();
  return new(Version,catalogue,teams.ToImmutableDictionary(t=>t,t=>new ConstructionDraft(t,1,baseline,[])));
 }
 public static ImmutableArray<ConstructionOption> Selected(ConstructionCatalogue catalogue,ImmutableArray<string> choices)
 {
  if(choices.IsDefault||choices.Length!=catalogue.Categories.Length||choices.Distinct().Count()!=choices.Length)throw new ArgumentException("Choose exactly one option from each construction category.");
  var selected=catalogue.Categories.Select(category=>category.Options.Where(o=>choices.Contains(o.Id)).ToArray()).ToArray();
  if(selected.Any(options=>options.Length!=1))throw new ArgumentException("Choose exactly one known option from every construction category.");
  return selected.Select(options=>options[0]).ToImmutableArray();
 }
 public static int Cost(ConstructionCatalogue catalogue,ImmutableArray<string> choices)=>checked(Selected(catalogue,choices).Sum(o=>o.Cost));
 // Authored tunable diminishing returns: first two delta points fully apply; further
 // stacking applies at half strength, bounded at six points in either direction.
 public static double BoundedDelta(double delta)=>Math.Sign(delta)*Math.Min(6,Math.Min(2,Math.Abs(delta))+Math.Max(0,Math.Abs(delta)-2)*.5);
 public static ImmutableDictionary<string,double> Ratings(ConstructionCatalogue catalogue,ImmutableArray<string> choices)
 {
  var selected=Selected(catalogue,choices);
  return catalogue.Baseline.ToImmutableDictionary(p=>p.Key,p=>Math.Clamp(p.Value+BoundedDelta(selected.Sum(o=>o.Modifiers.GetValueOrDefault(p.Key))),1,100));
 }
 public static ConstructionState Edit(ConstructionState state,ConstructionEdit command,int balance)
 {
  if(command is null||!Guid.TryParse(command.CommandId,out var id)||id==Guid.Empty||!state.Drafts.TryGetValue(command.TeamId,out var draft))throw new ArgumentException("Supply a command identity and a construction team.");
  if(command.OptionIds.IsDefault)throw new ArgumentException("Choose construction options.");
  var fingerprint=$"{command.Revision}/{command.CatalogueVersion}/{string.Join('/',command.OptionIds)}";
  if(draft.Commands.FirstOrDefault(c=>c.CommandId==id.ToString("D")) is {} receipt)return receipt.Fingerprint==fingerprint?state:throw new InvalidOperationException("Command identity already accepted with different inputs.");
  if(command.CatalogueVersion!=state.Catalogue.Version||command.Revision!=draft.Revision)throw new InvalidOperationException("Draft changed. Refresh the saved revision and catalogue.");
  if(Cost(state.Catalogue,command.OptionIds)>balance)throw new InvalidOperationException("This draft exceeds your remaining credits. No credits were spent and your saved draft is unchanged.");
  return state with{Drafts=state.Drafts.SetItem(command.TeamId,draft with{Revision=draft.Revision+1,OptionIds=command.OptionIds,Commands=draft.Commands.Add(new(id.ToString("D"),fingerprint))})};
 }
 public static void ValidateCatalogue(ConstructionCatalogue catalogue)
 {
  string[] keys=["topSpeed","accelerationDeployment","highSpeedAero","lowSpeedAero","mechanicalGrip","braking","traction","tyreWear","tyreTemperature","reliability"];
  string[] handling=["frontResponse","rearStability","rotation","brakingEntryStability","tractionConfidence","highSpeedStability"];
  if(catalogue.Version!="construction-catalogue-v1/"+catalogue.SourceVersion||catalogue.SourceVersion!="1.0.0/2026.10.09-preseason-car-build-v1"||catalogue.MappingVersion!=MappingVersion||catalogue.Baseline.Count!=10||keys.Any(k=>!catalogue.Baseline.TryGetValue(k,out var value)||!double.IsFinite(value)||value is <1 or >100)||catalogue.Categories.Length!=4||catalogue.Categories.Select(c=>c.Id).Distinct().Count()!=4)throw new ArgumentException("Unsupported frozen construction catalogue.");
  var options=catalogue.Categories.SelectMany(c=>c.Options).ToArray();
  if(options.Select(o=>o.Id).Distinct().Count()!=options.Length||catalogue.Categories.Any(c=>string.IsNullOrWhiteSpace(c.Id)||string.IsNullOrWhiteSpace(c.Label)||c.Options.IsDefaultOrEmpty)||options.Any(o=>string.IsNullOrWhiteSpace(o.Id)||string.IsNullOrWhiteSpace(o.Name)||string.IsNullOrWhiteSpace(o.Summary)||o.Cost is <1 or >100||o.Modifiers.Any(p=>!keys.Contains(p.Key)||!double.IsFinite(p.Value)||p.Value is < -6 or >6)||o.HandlingProfile.Any(p=>!handling.Contains(p.Key)||!double.IsFinite(p.Value)||p.Value is < -6 or >6))||catalogue.Categories.Sum(c=>c.Options.Min(o=>o.Cost))!=18)throw new ArgumentException("Invalid construction content or minimum credit reserve.");
 }
 public static void Validate(ConstructionState state,IReadOnlyList<string> teams)
 {
  ValidateCatalogue(state.Catalogue);
  if(state.Version!=Version||!state.Drafts.Keys.Order().SequenceEqual(teams.Order()))throw new ArgumentException("Unsupported construction save.");
  foreach(var (team,draft) in state.Drafts)
  {
   if(draft.TeamId!=team||draft.Revision<1||draft.Commands.IsDefault||draft.Revision!=draft.Commands.Length+1||draft.Commands.Select(c=>c.CommandId).Distinct().Count()!=draft.Commands.Length||draft.Commands.Any(c=>!Guid.TryParse(c.CommandId,out var id)||id==Guid.Empty||string.IsNullOrWhiteSpace(c.Fingerprint)))throw new ArgumentException("Invalid saved draft revision.");
   _=Ratings(state.Catalogue,draft.OptionIds);
  }
 }
}
