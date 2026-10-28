using Game.Domain;
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
    if(draft.Quote is {} quote&&PermanentDevelopment.Quote(quote.Option.Id,quote.Personnel)!=quote)throw new ArgumentException("Invalid saved quote.");
   }
   foreach(var decision in opportunity.Decisions)
   {
    if(!opportunity.Drafts.TryGetValue(decision.TeamId,out var draft)||decision.IntendedEventId!=opportunity.TargetEventId||decision.ProjectId!=opportunity.Id+"/"+decision.TeamId||decision.ActionKind!=draft.ActionKind||decision.Quote!=draft.Quote||!Guid.TryParse(decision.CommandId,out var key)||key==Guid.Empty||decision.Source is not("manual-confirmation" or "host-absent-save" or "unclaimed-save"))throw new ArgumentException("Invalid project receipt.");
    var own=debits.Where(e=>e.CommandId==decision.CommandId).ToArray();
    if(decision.Quote is {} quote)
    {if(own.Length!=1||own[0]!=new CreditEntry(decision.CommandId,decision.TeamId,-quote.Cost,"permanent-development",decision.ProjectId,PermanentDevelopment.Version,decision.IntendedEventId))throw new ArgumentException("Project and exact debit must agree.");}
    else if(own.Length!=0)throw new ArgumentException("Save cannot spend credits.");
   }
  }
  if(debits.Length!=decisions.Count(d=>d.Quote is not null))throw new ArgumentException("Unmatched development debit.");
 }
}
