using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class DevelopmentGuidanceTests
{
 private static PersonnelCard Staff(string id,string role,params (string,int)[] attributes)=>new(id,id,role,8,"authored-staff-v1","Frozen printed attributes",Attributes:attributes.ToImmutableDictionary(x=>x.Item1,x=>x.Item2));
 private static PersonnelCard[] Roster()=>[
  Staff("manager","Manager",("Efficiency",60),("Development",40),("Leadership",100),("Operations",100)),
  Staff("technical-a","Technical Engineer",("Development Efficiency",80),("Aerodynamics",100),("Power Systems",80),("Mechanical / Chassis",10),("Reliability",20)),
  Staff("technical-b","Technical Engineer",("Development Efficiency",80),("Aerodynamics",80),("Power Systems",60),("Mechanical / Chassis",30),("Reliability",40))];
 [Fact] public void Frozen_matching_specialists_affect_only_bounded_quote_cost_and_quality()
 {
  var quotes=DevelopmentPersonnelMapping.Quotes(Roster(),"roster-v1");
  var speed=quotes.Single(q=>q.Option.Id=="speed");var handling=quotes.Single(q=>q.Option.Id=="handling");
  Assert.Equal(80,speed.Personnel.TechnicalQuality);Assert.Equal(20,handling.Personnel.TechnicalQuality);
  Assert.Equal(15,speed.Cost);Assert.Equal(.14,speed.PartialProbability,8);Assert.Equal(.71,speed.NormalProbability,8);
  Assert.Equal(.17,handling.PartialProbability,8);Assert.Equal(.15,speed.DelayProbability,8);
  Assert.Contains("matched-specialists-v1",speed.Personnel.RosterVersion);
  Assert.Equal(16,DevelopmentPersonnelMapping.PresetQuotes().Single(q=>q.Option.Id=="speed").Cost);
 }
 [Fact] public void Advice_preserves_next_opportunity_reserve_and_uses_public_relevance_with_Save_as_a_real_choice()
 {
  var observation=new DevelopmentPolicyObservation(30,1,DevelopmentPersonnelMapping.PresetQuotes(),new Dictionary<string,double>{{"speed",100},{"handling",70},{"tyres",60},{"reliability",90}}.ToImmutableDictionary(),PermanentDevelopment.Options.ToImmutableDictionary(o=>o.Id,_=>6d));
  var recommendation=DevelopmentDecisionPolicy.Evaluate(observation);
  Assert.Equal("PermanentRefinement",recommendation.ActionKind);Assert.Equal("speed",recommendation.OptionId);Assert.Equal(14,recommendation.RemainingCredits);Assert.Equal(12,recommendation.FutureReserve);
  Assert.Equal("Save",DevelopmentDecisionPolicy.Evaluate(observation with{Balance=20}).ActionKind);
  Assert.Equal("Save",DevelopmentDecisionPolicy.Evaluate(observation with{GainCapacity=PermanentDevelopment.Options.ToImmutableDictionary(o=>o.Id,_=>0d)}).ActionKind);
  Assert.Equal("reliability",DevelopmentDecisionPolicy.Evaluate(observation with{Balance=24}).OptionId);
 } [Fact] public void Inactive_staff_and_unrelated_specialties_add_no_hidden_benefit_and_quality_stays_capped()
 {
  var roster=Roster();var before=DevelopmentPersonnelMapping.Quotes(roster,"frozen");
  var changed=roster.Select(p=>p.Id=="manager"?p with{Attributes=p.Attributes!.SetItem("Leadership",1).SetItem("Operations",1)}:p.Id=="technical-a"?p with{Attributes=p.Attributes!.SetItem("Reliability",100)}:p).Append(Staff("race","Race Engineer",("Setup",100))).ToArray();
  Assert.Equal(before.Single(q=>q.Option.Id=="speed"),DevelopmentPersonnelMapping.Quotes(changed,"frozen").Single(q=>q.Option.Id=="speed"));
  var maximum=roster.Select(p=>p with{Attributes=p.Attributes!.ToImmutableDictionary(a=>a.Key,_=>100)}).ToArray();
  var quote=DevelopmentPersonnelMapping.Quotes(maximum,"maximum").Single(q=>q.Option.Id=="tyres");
  Assert.Equal(10,quote.Cost);Assert.Equal(.1,quote.PartialProbability,8);Assert.Equal(.75,quote.NormalProbability,8);Assert.Equal(.15,quote.DelayProbability,8);
  var frozen=System.Text.Json.JsonSerializer.Deserialize<DevelopmentQuote>(System.Text.Json.JsonSerializer.Serialize(quote))!;
  Assert.Equal(quote,PermanentDevelopment.Quote(frozen.Option.Id,frozen.Personnel,frozen.Controls));
  Assert.Throws<ArgumentException>(()=>DevelopmentPersonnelMapping.Map(roster.Take(2).ToArray(),"incomplete","speed"));
 }}


