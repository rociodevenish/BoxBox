using System.Text.Json.Serialization;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Game.Domain;

public record DevelopmentPersonnel(string RosterVersion,int ManagerEfficiency,int ManagerDevelopment,int TechnicalEfficiency,int TechnicalQuality);
public record DevelopmentOption(string Id,string Name,int BaseCost,string Attribute,int MinimumGain,int MaximumGain,int StackingCap,string Compromise);
public record DevelopmentQuote(string Id,string Version,DevelopmentOption Option,int Cost,DevelopmentPersonnel Personnel,string OutcomeVersion,string OutcomeRange,DevelopmentOutcomeControls? Controls=null,double NormalProbability=.65,double PartialProbability=.2,double DelayProbability=.15);
public record DevelopmentDraft(long Revision,string ActionKind,string? OptionId,DevelopmentQuote? Quote);
public record DevelopmentEdit(string CommandId,string OpportunityId,long Revision,string ActionKind,string? OptionId,string? QuoteId,long AssignmentRevision=0);
public record DevelopmentCommit(string CommandId,string OpportunityId,long Revision,long DraftRevision,string? QuoteId,long AssignmentRevision=0);
public record DevelopmentDecision(string TeamId,string CommandId,string Fingerprint,string ActionKind,string ProjectId,string IntendedEventId,DevelopmentQuote? Quote,string Source);
public record DevelopmentReceipt(string CommandId,string Fingerprint);
public record DevelopmentOpportunity(string Id,string FromEventId,string TargetEventId,long Revision,ImmutableDictionary<string,DevelopmentDraft> Drafts,ImmutableArray<DevelopmentDecision> Decisions,ImmutableArray<DevelopmentReceipt> Receipts,bool Closed=false);
public record DevelopmentState(string Version,ImmutableArray<DevelopmentOpportunity> Opportunities,string EntropyKey,[property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] ImmutableArray<DevelopmentOutcome> Outcomes=default,[property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingDefault)] ImmutableArray<PermanentModifier> Modifiers=default);
public record DevelopmentCommitment(DevelopmentOpportunity State,ImmutableArray<CreditEntry> Ledger);

/// <summary>One free editable draft and one accepted action per between-event opportunity.</summary>
public static partial class PermanentDevelopment
{
 public const string Version="permanent-development-v1";
 public const string OutcomeVersion="normal-refinement-v1";
 public static readonly ImmutableArray<DevelopmentOption> Options=[
  new("speed","Straight-line refinement",16,"topSpeed",1,3,6,"At full success, accelerationDeployment -1 (total compromise floor -3)."),
  new("handling","Mechanical refinement",14,"mechanicalGrip",1,3,6,"At full success, topSpeed -1 (total compromise floor -3)."),
  new("tyres","Tyre platform refinement",12,"tyreWear",1,3,6,"At full success, tyreTemperature -1 (total compromise floor -3)."),
  new("reliability","Reliability refinement",12,"reliability",1,3,6,"At full success, accelerationDeployment -1 (total compromise floor -3).")];
 public static DevelopmentOpportunity Open(string id,string fromEventId,string targetEventId,IEnumerable<string> teams)=>new(id,fromEventId,targetEventId,1,teams.ToImmutableDictionary(t=>t,_=>new DevelopmentDraft(0,"Save",null,null)),[],[]);
 public static DevelopmentQuote Quote(string optionId,DevelopmentPersonnel personnel,DevelopmentOutcomeControls? controls=null)
 {
  var option=Options.SingleOrDefault(o=>o.Id==optionId)??throw new ArgumentException("Choose a supported refinement.");
  if(string.IsNullOrWhiteSpace(personnel.RosterVersion)||new[]{personnel.ManagerEfficiency,personnel.ManagerDevelopment,personnel.TechnicalEfficiency,personnel.TechnicalQuality}.Any(v=>v is <0 or >100))throw new ArgumentException("Invalid frozen personnel inputs.");
  controls??=DefaultControls; ValidateControls(controls);
  var discount=Math.Min(3,(personnel.ManagerEfficiency+personnel.TechnicalEfficiency)/80);
  var cost=Math.Max(8,option.BaseCost-discount);
  var partial=Math.Max(0,controls.PartialProbability-Math.Min(.1,(personnel.ManagerDevelopment+personnel.TechnicalQuality)/2000d));
  var normal=1-controls.DelayProbability-partial;
  var basis=JsonSerializer.Serialize(new{Version,option,personnel,cost,controls,normal,partial});
  return new(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(basis))),Version,option,cost,personnel,OutcomeVersion,$"Normal {normal:P0}, partial {partial:P0}, delay {controls.DelayProbability:P0}. Normal +1 to +3; partial +1; delay +0 until following preparation close. Cumulative gain cap +6 per attribute; ratings remain 1–100. Quality contribution bounded to 10 percentage points.",controls,normal,partial,controls.DelayProbability);
 }
 public static DevelopmentOpportunity Edit(DevelopmentOpportunity state,string team,DevelopmentEdit command,DevelopmentPersonnel personnel,DevelopmentOutcomeControls? controls=null)
 {
  if(command is null)throw new ArgumentException("Supply a development draft command.");
  var fingerprint=JsonSerializer.Serialize(new{team,command});
  var key=Key(command.CommandId);
  if(state.Decisions.Any(d=>d.CommandId==key))throw new InvalidOperationException("Command identity already belongs to an accepted decision.");
  if(state.Receipts.FirstOrDefault(r=>r.CommandId==key) is {} prior)return prior.Fingerprint==fingerprint?state:throw new InvalidOperationException("Command identity already accepted with different inputs.");
  Check(state,team,command.OpportunityId,command.Revision);
  var quote=command.ActionKind switch{"Save" when command.OptionId is null&&command.QuoteId is null=>null,"PermanentRefinement"=>Quote(command.OptionId??"",personnel,controls),_=>throw new ArgumentException("Choose Save or PermanentRefinement explicitly.")};
  if(quote?.Id!=command.QuoteId)throw new InvalidOperationException("Quote changed. Refresh before editing.");
  return state with{Revision=state.Revision+1,Drafts=state.Drafts.SetItem(team,new(state.Drafts[team].Revision+1,command.ActionKind,command.OptionId,quote)),Receipts=state.Receipts.Add(new(key,fingerprint))};
 }
 public static DevelopmentCommitment Commit(DevelopmentOpportunity state,ImmutableArray<CreditEntry> ledger,string team,DevelopmentCommit command,DevelopmentPersonnel personnel,string source="manual-confirmation")
 {
  if(command is null)throw new ArgumentException("Supply a development commitment command.");
  var key=Key(command.CommandId);var fingerprint=JsonSerializer.Serialize(new{team,command});
  if(state.Decisions.FirstOrDefault(d=>d.CommandId==key) is {} previous)return previous.Fingerprint==fingerprint?new(state,ledger):throw new InvalidOperationException("Command identity already accepted with different inputs.");
  if(state.Receipts.Any(r=>r.CommandId==key))throw new InvalidOperationException("Command identity already used for a draft.");
  Check(state,team,command.OpportunityId,command.Revision);
  var draft=state.Drafts[team];
  if(draft.Revision!=command.DraftRevision||draft.Quote?.Id!=command.QuoteId)throw new InvalidOperationException("Draft changed. Refresh before commitment.");
  if(draft.Quote is {} quote)
  {
   if(Quote(quote.Option.Id,personnel,quote.Controls)!=quote)throw new InvalidOperationException("Personnel or quote changed. Refresh before commitment.");
   if(ledger.Where(e=>e.TeamId==team).Sum(e=>e.Credits)<quote.Cost)throw new InvalidOperationException("Insufficient credits. No investment or Save was accepted.");
   ledger=ledger.Add(new(key,team,-quote.Cost,"permanent-development",state.Id+"/"+team,Version,state.TargetEventId));
  }
  var decision=new DevelopmentDecision(team,key,fingerprint,draft.ActionKind,state.Id+"/"+team,state.TargetEventId,draft.Quote,source);
  return new(state with{Revision=state.Revision+1,Decisions=state.Decisions.Add(decision)},ledger);
 }
 private static string Key(string value)=>Guid.TryParse(value,out var key)&&key!=Guid.Empty?key.ToString("D"):throw new ArgumentException("Supply a command identity.");
 private static void Check(DevelopmentOpportunity state,string team,string opportunity,long revision)
 {
  if(state.Id!=opportunity||state.Revision!=revision)throw new InvalidOperationException("Development changed. Refresh the saved revision.");
  if(state.Closed||state.Decisions.Any(d=>d.TeamId==team))throw new InvalidOperationException("This development opportunity has already been consumed.");
  if(!state.Drafts.ContainsKey(team))throw new ArgumentException("Choose a team in this opportunity.");
 }
}
