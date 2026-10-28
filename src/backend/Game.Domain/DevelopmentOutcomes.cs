using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
namespace Game.Domain;
public record DevelopmentOutcomeControls(string Version,double PartialProbability,double DelayProbability,double GainVariation);
public record DevelopmentOutcome(string ProjectId,string TeamId,string Kind,string ResolvedEventId,string? CompletionEventId,DevelopmentQuote Contract,int Gain=0);
public record PermanentModifier(string ProjectId,string TeamId,string CompletionEventId,ImmutableDictionary<string,double> Changes);
public static partial class PermanentDevelopment
{
 public static readonly DevelopmentOutcomeControls DefaultControls=new("normal-controls-v1",.2,.15,1);
 public static void ValidateControls(DevelopmentOutcomeControls controls)
 {
  if(controls.Version!="normal-controls-v1"||!double.IsFinite(controls.PartialProbability)||!double.IsFinite(controls.DelayProbability)||!double.IsFinite(controls.GainVariation)||controls.PartialProbability<0||controls.DelayProbability<0||controls.PartialProbability+controls.DelayProbability>1||controls.GainVariation is <0 or >1)throw new ArgumentException("Unsupported bounded development outcome controls.");
 }
 public static DevelopmentState Close(DevelopmentState state,string opportunityId,IReadOnlyList<string> calendar)
 {
  var opportunity=state.Opportunities.Single(o=>o.Id==opportunityId);
  if(opportunity.Closed)return state;
  if(opportunity.Decisions.Length!=opportunity.Drafts.Count)throw new InvalidOperationException("Every team needs a committed decision or disclosed Save fallback.");
  var outcomes=state.Outcomes.IsDefault?ImmutableArray<DevelopmentOutcome>.Empty:state.Outcomes;
  var modifiers=state.Modifiers.IsDefault?ImmutableArray<PermanentModifier>.Empty:state.Modifiers;
  var index=calendar.ToList().IndexOf(opportunity.TargetEventId);
  if(index<0)throw new ArgumentException("Unknown completion event.");
  double Draw(string project,string channel)=>System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(HMACSHA256.HashData(Convert.FromHexString(state.EntropyKey),JsonSerializer.SerializeToUtf8Bytes(new[]{OutcomeVersion,opportunity.Id,project,channel})))/(uint.MaxValue+1d);
  foreach(var decision in opportunity.Decisions.OrderBy(d=>d.ProjectId,StringComparer.Ordinal))
  {
   if(decision.Quote is not {} quote)continue;
   var controls=quote.Controls??DefaultControls;ValidateControls(controls);
   
   var draw=Draw(decision.ProjectId,"class");
   var kind=draw<quote.DelayProbability?"delay":draw<quote.DelayProbability+quote.PartialProbability?"partial":"normal";
   var due=kind=="delay"?(index+1<calendar.Count?calendar[index+1]:null):opportunity.TargetEventId;
   var gain=kind=="partial"?quote.Option.MinimumGain:quote.Option.MaximumGain-(int)Math.Floor(Draw(decision.ProjectId,"magnitude")*controls.GainVariation*(quote.Option.MaximumGain-quote.Option.MinimumGain+1));
   outcomes=outcomes.Add(new(decision.ProjectId,decision.TeamId,kind,opportunity.TargetEventId,due,quote,Math.Max(quote.Option.MinimumGain,gain)));
  }
  // Previous delays complete first; then projects from this close in stable original opportunity order.
  foreach(var outcome in outcomes.Where(o=>o.CompletionEventId==opportunity.TargetEventId&&!modifiers.Any(m=>m.ProjectId==o.ProjectId)))
  {
   var quote=outcome.Contract;var option=quote.Option;
   var accumulated=modifiers.Where(m=>m.TeamId==outcome.TeamId).Sum(m=>Math.Max(0,m.Changes.GetValueOrDefault(option.Attribute)));
   var raw=outcome.Gain;
   var gain=Math.Min(Math.Max(0,option.StackingCap-accumulated),Math.Max(option.MinimumGain,raw-(int)(accumulated/3)));
   var changes=ImmutableDictionary<string,double>.Empty.Add(option.Attribute,gain);
   if(outcome.Kind!="partial"&&gain>0)
   {
    var compromise=option.Id switch{"handling"=>"topSpeed","tyres"=>"tyreTemperature",_=>"accelerationDeployment"};
    var loss=modifiers.Where(m=>m.TeamId==outcome.TeamId).Sum(m=>Math.Min(0,m.Changes.GetValueOrDefault(compromise)));
    changes=changes.Add(compromise,Math.Max(-1,-3-loss));
   }
   modifiers=modifiers.Add(new(outcome.ProjectId,outcome.TeamId,opportunity.TargetEventId,changes));
  }
  return state with{Opportunities=state.Opportunities.Select(o=>o.Id==opportunityId?o with{Closed=true,Revision=o.Revision+1}:o).ToImmutableArray(),Outcomes=outcomes,Modifiers=modifiers};
 }
}

