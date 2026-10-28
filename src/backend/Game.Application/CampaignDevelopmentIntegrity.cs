using Game.Domain;
using Game.Simulation;
using System.Collections.Immutable;
using System.Text.Json;
namespace Game.Application;
public static class CampaignDevelopmentIntegrity
{
 public static void Validate(CampaignRecord record)
 {
  var debits=CampaignEconomy.Ledger(record).Where(e=>e.Reason=="permanent-development").ToArray();
  if(record.Development is not {} development){if(debits.Length!=0)throw new ArgumentException("Missing development receipts.");return;}
  if(development.Version!=PermanentDevelopment.Version||development.EntropyKey.Length!=64||!development.EntropyKey.All(Uri.IsHexDigit)||development.Opportunities.Select(o=>o.Id).Distinct().Count()!=development.Opportunities.Length)throw new ArgumentException("Invalid development contract.");
  var decisions=development.Opportunities.SelectMany(o=>o.Decisions).ToArray();
  if(decisions.Select(d=>d.CommandId).Distinct().Count()!=decisions.Length||decisions.Select(d=>d.ProjectId).Distinct().Count()!=decisions.Length)throw new ArgumentException("Duplicate development commitment.");
  foreach(var opportunity in development.Opportunities)
  {
   var index=Array.FindIndex(record.Calendar,e=>e.EventId==opportunity.TargetEventId);
   if(index<1||opportunity.FromEventId!=record.Calendar[index-1].EventId||opportunity.Id!=record.CampaignId+"/development/"+opportunity.TargetEventId||!(record.Contributions??[]).Any(c=>c.EventId==opportunity.FromEventId)||!opportunity.Drafts.Keys.Order().SequenceEqual(record.Teams.Select(t=>t.TeamId).Order())||opportunity.Decisions.Select(d=>d.TeamId).Distinct().Count()!=opportunity.Decisions.Length||opportunity.Receipts.Select(r=>r.CommandId).Distinct().Count()!=opportunity.Receipts.Length||opportunity.Revision<1+opportunity.Receipts.Length+opportunity.Decisions.Length)throw new ArgumentException("Invalid development opportunity.");
   foreach(var draft in opportunity.Drafts.Values)
   {
    if(draft.Revision<0||draft.ActionKind is not("Save" or "PermanentRefinement")||draft.ActionKind=="Save"&&(draft.Quote is not null||draft.OptionId is not null)||draft.ActionKind=="PermanentRefinement"&&(draft.Quote is null||draft.OptionId!=draft.Quote.Option.Id))throw new ArgumentException("Invalid development draft.");
    if(draft.Quote is {} quote&&PermanentDevelopment.Quote(quote.Option.Id,quote.Personnel,quote.Controls)!=quote)throw new ArgumentException("Invalid saved quote.");
   }
   foreach(var decision in opportunity.Decisions)
   {
    if(!opportunity.Drafts.TryGetValue(decision.TeamId,out var draft)||decision.IntendedEventId!=opportunity.TargetEventId||decision.ProjectId!=opportunity.Id+"/"+decision.TeamId||decision.ActionKind!=draft.ActionKind||decision.Quote!=draft.Quote||!Guid.TryParse(decision.CommandId,out var key)||key==Guid.Empty||decision.Source is not("manual-confirmation" or "host-absent-save" or "unclaimed-save" or "unclaimed-ai"))throw new ArgumentException("Invalid project receipt.");
    var own=debits.Where(e=>e.CommandId==decision.CommandId).ToArray();
    if(decision.Quote is {} quote)
    {if(own.Length!=1||own[0]!=new CreditEntry(decision.CommandId,decision.TeamId,-quote.Cost,"permanent-development",decision.ProjectId,PermanentDevelopment.Version,decision.IntendedEventId))throw new ArgumentException("Project and exact debit must agree.");}
    else if(own.Length!=0)throw new ArgumentException("Save cannot spend credits.");
   }
  }
  var choices=record.DevelopmentChoices??[];
  if(choices.Select(c=>c.DraftCommandId).Distinct().Count()!=choices.Length)throw new ArgumentException("Duplicate development policy choice.");
  foreach(var choice in choices)
  {
   var opportunity=development.Opportunities.Single(o=>o.Id==choice.OpportunityId);
   if(choice.Source is not("assisted" or "unclaimed-ai")||choice.Revision<1||choice.Revision>=opportunity.Revision||!record.Teams.Any(t=>t.TeamId==choice.TeamId)||JsonSerializer.Serialize(DevelopmentDecisionPolicy.Evaluate(choice.Observation))!=JsonSerializer.Serialize(choice.Recommendation))throw new ArgumentException("Invalid saved development recommendation.");
   var targetIndex=Array.FindIndex(record.Calendar,e=>e.EventId==opportunity.TargetEventId);
   var balanceBefore=CampaignEconomy.Ledger(record).Where(e=>e.TeamId==choice.TeamId&&(e.Reason is not("event-income" or "permanent-development")||Array.FindIndex(record.Calendar,c=>c.EventId==e.EventId)<targetIndex)).Sum(e=>e.Credits);
   if(choice.Observation.Balance!=balanceBefore||choice.Observation.RemainingOpportunities!=record.Calendar.Length-targetIndex-1)throw new ArgumentException("Policy observation must use the legitimate balance and calendar.");
   var roster=record.Acquisition?.Hires.Where(h=>h.TeamId==choice.TeamId).Select(h=>record.Acquisition.Pool.People.Single(p=>p.Id==h.PersonId)).ToArray();
   if(choice.Observation.Options.Length!=PermanentDevelopment.Options.Length)throw new ArgumentException("Missing legitimate development alternatives.");
   foreach(var option in choice.Observation.Options)
   {
    var personnel=roster is null?new DevelopmentPersonnel("preset-development-neutral-v1",0,0,0,0):DevelopmentPersonnelMapping.Map(roster,record.Acquisition!.Pool.Version,option.Option.Id);
    if(personnel!=option.Personnel)throw new ArgumentException("Policy personnel must match the frozen own roster.");
   }
   var draft=opportunity.Receipts.Single(r=>r.CommandId==choice.DraftCommandId);
   using var command=JsonDocument.Parse(draft.Fingerprint);var edit=command.RootElement.GetProperty("command");
   if(command.RootElement.GetProperty("team").GetString()!=choice.TeamId||edit.GetProperty("OpportunityId").GetString()!=choice.OpportunityId||edit.GetProperty("Revision").GetInt64()!=choice.Revision||edit.GetProperty("ActionKind").GetString()!=choice.Recommendation.ActionKind||edit.GetProperty("OptionId").GetString()!=choice.Recommendation.OptionId||edit.GetProperty("QuoteId").GetString()!=choice.Recommendation.QuoteId)throw new ArgumentException("Recommendation and saved draft must agree.");
   if(choice.Source=="assisted"&&choice.CommitCommandId is not null)throw new ArgumentException("Advice alone cannot commit spending.");
   if(choice.Source=="unclaimed-ai")
   {
    var accepted=opportunity.Decisions.Single(d=>d.CommandId==choice.CommitCommandId);
    if(accepted.Source!="unclaimed-ai"||accepted.TeamId!=choice.TeamId||accepted.ActionKind!=choice.Recommendation.ActionKind||accepted.Quote?.Id!=choice.Recommendation.QuoteId||accepted.Quote?.Cost!= (choice.Recommendation.ActionKind=="Save"?null:choice.Recommendation.Cost))throw new ArgumentException("Automatic choice and exact commitment must agree.");
   }
  }
  foreach(var decision in decisions.Where(d=>d.Source=="unclaimed-ai"))if(!choices.Any(c=>c.Source=="unclaimed-ai"&&c.CommitCommandId==decision.CommandId))throw new ArgumentException("Missing automatic policy evidence.");
  var automatic=record.DevelopmentAutomaticCommands??[];
  if(automatic.Select(c=>c.CommandId).Distinct().Count()!=automatic.Length)throw new ArgumentException("Duplicate automatic command.");
  foreach(var receipt in automatic)
  {
   if(!Guid.TryParse(receipt.CommandId,out var command)||command==Guid.Empty||receipt.Revision<1||!development.Opportunities.Any(o=>o.Id==receipt.OpportunityId)||receipt.TeamIds.Distinct().Count()!=receipt.TeamIds.Length||receipt.TeamIds.Any(team=>!choices.Any(c=>c.OpportunityId==receipt.OpportunityId&&c.TeamId==team&&c.Source=="unclaimed-ai")))throw new ArgumentException("Invalid automatic development receipt.");
  }
  var replay=new DevelopmentState(development.Version,development.Opportunities.Select(o=>o with{Closed=false}).ToImmutableArray(),development.EntropyKey);
  foreach(var opportunity in development.Opportunities.Where(o=>o.Closed))replay=PermanentDevelopment.Close(replay,opportunity.Id,record.Calendar.Select(e=>e.EventId).ToArray());
  string Enc<T>(T value)=>JsonSerializer.Serialize(value,BrowserReplay.JsonOptions);
  if(Enc(replay.Outcomes.IsDefault?[]:replay.Outcomes)!=Enc(development.Outcomes.IsDefault?[]:development.Outcomes)||Enc(replay.Modifiers.IsDefault?[]:replay.Modifiers)!=Enc(development.Modifiers.IsDefault?[]:development.Modifiers))throw new ArgumentException("Saved development outcome or modifier is incompatible.");
  var snapshots=record.EffectiveEventPresets??new();
  if(snapshots.Count!=development.Opportunities.Count(o=>o.Closed))throw new ArgumentException("Every closed opportunity needs its frozen effective event input.");
  foreach(var (eventId,presets) in snapshots){var index=Array.FindIndex(record.Calendar,e=>e.EventId==eventId);if(index<1)throw new ArgumentException("Unknown development target.");var eligible=(development.Modifiers.IsDefault?[]:development.Modifiers).Where(m=>Array.FindIndex(record.Calendar,e=>e.EventId==m.CompletionEventId)<=index);var expected=record.FrozenPresets[eventId].Select(p=>DevelopedCar.Apply(p,eligible)).ToArray();if(Enc(expected)!=Enc(presets))throw new ArgumentException("Effective event input differs from its permanent modifiers.");}
  if((record.DevelopmentCloses??[]).Select(c=>c.CommandId).Distinct().Count()!=(record.DevelopmentCloses??[]).Length||(record.DevelopmentCloses??[]).Length!=development.Opportunities.Count(o=>o.Closed))throw new ArgumentException("Invalid development close receipts.");
  if(debits.Length!=decisions.Count(d=>d.Quote is not null))throw new ArgumentException("Unmatched development debit.");
 }
}
