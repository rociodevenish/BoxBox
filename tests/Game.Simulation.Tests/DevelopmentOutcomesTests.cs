using System.Collections.Immutable;
using Game.Domain;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class DevelopmentOutcomesTests
{
 [Fact] public void Partial_success_uses_the_saved_minimum_without_a_normal_compromise()
 {
  var staff=new DevelopmentPersonnel("neutral",0,0,0,0);var controls=new DevelopmentOutcomeControls("normal-controls-v1",1,0,0);
  var quote=PermanentDevelopment.Quote("reliability",staff,controls);
  var op=PermanentDevelopment.Open("op","one","two",["a"]);
  op=PermanentDevelopment.Edit(op,"a",new(Guid.NewGuid().ToString(),"op",1,"PermanentRefinement","reliability",quote.Id),staff,controls);
  var paid=PermanentDevelopment.Commit(op,[new("start","a",100,"starting-credit")],"a",new(Guid.NewGuid().ToString(),"op",2,1,quote.Id),staff);
  var resolved=PermanentDevelopment.Close(new(PermanentDevelopment.Version,[paid.State],new string('3',64)),"op",["one","two"]);
  Assert.Equal("partial",Assert.Single(resolved.Outcomes).Kind);
  var modifier=Assert.Single(resolved.Modifiers);Assert.Equal(1,modifier.Changes["reliability"]);Assert.Single(modifier.Changes);
 }
 [Fact] public void Delayed_work_completes_with_next_independent_project_without_repayment_and_caps_in_stable_order()
 {
  var calendar=new[]{"one","two","three","four","five","six"};
  var staff=new DevelopmentPersonnel("neutral",0,0,0,0);
  var state=new DevelopmentState(PermanentDevelopment.Version,[],new string('2',64));
  ImmutableArray<CreditEntry> ledger=[new("start","a",100,"starting-credit")];
  for(var i=1;i<calendar.Length;i++)
  {
   var controls=new DevelopmentOutcomeControls("normal-controls-v1",0,i==1||i==5?1:0,0);
   var quote=PermanentDevelopment.Quote("speed",staff,controls);
   var op=PermanentDevelopment.Open("op-"+i,calendar[i-1],calendar[i],["a"]);
   op=PermanentDevelopment.Edit(op,"a",new(Guid.NewGuid().ToString(),op.Id,op.Revision,"PermanentRefinement","speed",quote.Id),staff,controls);
   var paid=PermanentDevelopment.Commit(op,ledger,"a",new(Guid.NewGuid().ToString(),op.Id,op.Revision,1,quote.Id),staff);ledger=paid.Ledger;
   state=PermanentDevelopment.Close(state with{Opportunities=state.Opportunities.Add(paid.State)},op.Id,calendar);
   if(i==1){Assert.Empty(state.Modifiers);Assert.Equal("three",Assert.Single(state.Outcomes).CompletionEventId);}
   if(i==2){Assert.Equal(new[]{"op-1/a","op-2/a"},state.Modifiers.Select(m=>m.ProjectId));Assert.Equal(3,state.Modifiers[0].Changes["topSpeed"]);Assert.Equal(2,state.Modifiers[1].Changes["topSpeed"]);}
  }
  Assert.Equal(20,ledger.Sum(e=>e.Credits));
  Assert.Equal(6,state.Modifiers.Sum(m=>m.Changes["topSpeed"]));
  Assert.Equal(-3,state.Modifiers.Sum(m=>m.Changes.GetValueOrDefault("accelerationDeployment")));
  Assert.Null(state.Outcomes.Single(o=>o.ProjectId=="op-5/a").CompletionEventId);
  Assert.DoesNotContain(state.Modifiers,m=>m.ProjectId=="op-5/a");
 }
 [Fact] public void Composition_preserves_Base_Car_and_handling_while_practice_and_wet_inputs_use_completed_gains()
 {
  var original=Game.Simulation.WeekendPresetCatalog.Open("track-monza");
  var constructed=Game.Simulation.ConstructedBaseCar.Apply(original,"confirmed-car","frozen-menu",original.Cars[0].Car.Ratings,1);
  var modifier=new PermanentModifier("project","team-1","two",ImmutableDictionary<string,double>.Empty.Add("topSpeed",3).Add("tyreTemperature",2).Add("reliability",1));
  var composed=Game.Simulation.DevelopedCar.Apply(constructed,[modifier]);
  Assert.Equal(82,constructed.Cars[0].Car.Ratings["topSpeed"]);
  Assert.Equal(85,composed.Cars[0].Car.Ratings["topSpeed"]);
  var setup=new WeekendSetup();
  var before=Game.Simulation.WeekendPreparation.Prepare(constructed,6,setup);
  var after=Game.Simulation.WeekendPreparation.Prepare(composed,6,setup);
  Assert.True(after.Cars[0].RaceDriver.Speed>before.Cars[0].RaceDriver.Speed);
  Assert.Equal(before.DriverFits.ToArray(),after.DriverFits.ToArray());
  Assert.Equal(84,Game.Simulation.WeekendPreparation.EffectiveWetWeather(composed.Cars[0]).TyreTemperatureManagement);
  var incident=Game.Simulation.WeekendPresetCatalog.IncidentInputs(after,new IncidentTuning());
  var opening=new AutomaticStartInput(incident.Configuration,[after],after.Cars.Select((c,i)=>new StartGridSlot(i+1,c.Source.EntryId)).ToArray(),123,new());
  Assert.All(Game.Simulation.AutomaticRaceStart.StartingDrivers(opening),d=>Assert.Equal(83,d.IncidentProfile!.Reliability));
  var choices=after.Cars.Select(c=>new QualifyingChoice(c.Source.EntryId,"Safe")).ToArray();var order=choices.Select(c=>c.EntryId).ToArray();
  var beforeQ=Game.Simulation.Qualifying.Resolve(new("Q1",1,123,[before],choices,order,new(2)));
  var afterQ=Game.Simulation.Qualifying.Resolve(new("Q1",1,123,[after],choices,order,new(2)));
  Assert.True(afterQ.Ranking[0].TimeMilliseconds<beforeQ.Ranking[0].TimeMilliseconds);
  Assert.Throws<ArgumentException>(()=>Game.Simulation.DevelopedCar.Apply(composed,[modifier]));
 }
 [Fact] public void Saved_normal_contract_resolves_once_and_keeps_the_paid_commitment()
 {
  var staff=new DevelopmentPersonnel("neutral",0,0,0,0);
  var quote=PermanentDevelopment.Quote("speed",staff,new DevelopmentOutcomeControls("normal-controls-v1",0,0,0));
  var op=PermanentDevelopment.Open("op","one","two",["a"]);
  op=PermanentDevelopment.Edit(op,"a",new(Guid.NewGuid().ToString(),"op",1,"PermanentRefinement","speed",quote.Id),staff,quote.Controls);
  var paid=PermanentDevelopment.Commit(op,[new("start","a",100,"starting-credit")],"a",new(Guid.NewGuid().ToString(),"op",2,1,quote.Id),staff);
  var state=new DevelopmentState(PermanentDevelopment.Version,[paid.State],new string('1',64));
  var result=PermanentDevelopment.Close(state,"op",["one","two","three"]);
  Assert.Equal("normal",Assert.Single(result.Outcomes).Kind);
  Assert.Equal(3,Assert.Single(result.Modifiers).Changes["topSpeed"]);
  Assert.Equal(-1,Assert.Single(result.Modifiers).Changes["accelerationDeployment"]);
  Assert.Equal(84,paid.Ledger.Sum(e=>e.Credits));
  Assert.Equal(result,PermanentDevelopment.Close(result,"op",["one","two","three"]));
  Assert.Equal(quote,Assert.Single(result.Opportunities[0].Decisions).Quote);
 }
}


