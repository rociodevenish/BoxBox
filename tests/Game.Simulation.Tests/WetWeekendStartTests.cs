using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class WetWeekendStartTests
{
 [Fact] public void Wet_qualifying_has_one_common_interval_and_no_submission_order_advantage()
 {
  var weekend=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),6,new(0,0,0));
  weekend=weekend with{Configuration=weekend.Configuration with{Weather=new(MeanRain:1,Volatility:0,InitialWetness:1),WetTyres=new()}};
  var input=new QualifyingAttemptInput("Q1",1,17,[weekend],weekend.Cars.Select(c=>new QualifyingChoice(c.Source.EntryId,"Safe")).ToArray(),weekend.Cars.Select(c=>c.Source.EntryId).ToArray(),new(1),ModelVersion:Qualifying.WetModelVersion,Conditions:Qualifying.SessionConditions(weekend.Configuration,17,"Q1"));
  var first=Qualifying.Resolve(input);
  Assert.Equal(90,input.Conditions!.Observed.Time);
  Assert.Equal(first.Ranking,Qualifying.Resolve(input with{Choices=input.Choices.Reverse().ToArray()}).Ranking);
  Assert.Throws<ArgumentException>(()=>Qualifying.Resolve(input with{Conditions=input.Conditions with{Observed=input.Conditions.Observed with{Wetness=0}}}));
 }
 [Fact] public void Wet_opening_uses_the_shared_earliest_weather_reaction_boundary()
 {
  var weekend=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),6,new(0,0,0));
  var config=weekend.Configuration with{Weather=new(MeanRain:1,Volatility:0,InitialWetness:0,WettingPerSecond:.01,DryingPerSecond:0),WetTyres=new(),WeatherReactions=new()};
  var grid=weekend.Cars.Select((c,i)=>new StartGridSlot(i+1,c.Source.EntryId)).ToArray();
  var input=new AutomaticStartInput(config,[weekend],grid,17,new(),ModelVersion:AutomaticRaceStart.WetModelVersion,StartingCompounds:grid.ToDictionary(g=>g.EntryId,g=>TyreCompound.Intermediate));
  var opening=AutomaticRaceStart.Create(input);
  Assert.NotNull(opening.Opening.WeatherReaction);Assert.Null(opening.Opening.Interruption);
  Assert.Equal(28,opening.Opening.Time,6);Assert.Equal(0,opening.Opening.LeaderLap);
  Assert.Equal(AutomaticRaceStart.OutcomeHash(opening.Opening),AutomaticRaceStart.OutcomeHash(AutomaticRaceStart.Replay(new(input,AutomaticRaceStart.OutcomeHash(opening.Opening),"frozen")).Opening));
 }
 [Fact] public void Starting_compounds_are_separate_immutable_inputs_and_survive_opening_replay()
 {
  var weekend=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),6,new(0,0,0));
  var config=weekend.Configuration with{Weather=new(MeanRain:1,Volatility:0,InitialWetness:1),WetTyres=new()};
  var grid=weekend.Cars.Select((c,i)=>new StartGridSlot(i+1,c.Source.EntryId)).ToArray();
  var map=grid.ToDictionary(g=>g.EntryId,g=>TyreCompound.Wet);
  var input=new AutomaticStartInput(config,[weekend],grid,17,new(),ModelVersion:AutomaticRaceStart.WetModelVersion,StartingCompounds:map);
  var start=AutomaticRaceStart.Create(input);
  Assert.All(start.Opening.Tyres,t=>Assert.Equal(TyreCompound.Wet,t.Compound));
  Assert.All(weekend.Cars,c=>Assert.Equal(TyreCompound.Medium,c.RaceDriver.StartingCompound));
  Assert.All(AutomaticRaceStart.StartingDrivers(input),d=>Assert.NotNull(d.WetWeather));
  Assert.Equal(AutomaticRaceStart.OutcomeHash(start.Opening),AutomaticRaceStart.OutcomeHash(AutomaticRaceStart.Replay(new(input,AutomaticRaceStart.OutcomeHash(start.Opening),"frozen")).Opening));
  Assert.Throws<ArgumentException>(()=>AutomaticRaceStart.Create(input with{StartingCompounds=new Dictionary<string,TyreCompound>()}));
 }
}
