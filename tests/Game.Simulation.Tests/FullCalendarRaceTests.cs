using System.Collections.Immutable;
using System.Text.Json;
using Game.Domain;
using Game.Simulation;
using Xunit;
using Xunit.Abstractions;
namespace Game.Simulation.Tests;

public sealed class FullCalendarRaceTests(ITestOutputHelper output)
{
 [Theory]
 [MemberData(nameof(Circuits))]
 public void Complete_twenty_two_car_profile_race_replays_configured_weather_and_disruptions(string circuitId,bool forcedSafetyCar)
 {
  var pack=ReleaseContent.FullCalendar();var circuit=pack.Circuits.Single(c=>c.Id==circuitId);var config=pack.Conditions![circuitId] with{Laps=6};
  var field=pack.Constructors.Select(team=>
  {
   var roster=WeekendPresetCatalog.Open(circuitId,team.DriverIds[0],team.DriverIds[1]) with{Circuit=circuit};
   roster=roster with{Cars=roster.Cars.Select((c,i)=>c with{EntryId=team.TeamId+"-car-"+(i+1),TeamId=team.TeamId}).ToImmutableArray()};
   return WeekendPreparation.Prepare(roster,6,new());
  }).ToArray();
  var wetness=CircuitWeather.Observe(config.Weather!,123,0).Wetness;
  var compound=wetness>=.7?TyreCompound.Wet:wetness>=.25?TyreCompound.Intermediate:TyreCompound.Medium;
  var drivers=field.SelectMany(w=>w.Cars.Select(c=>c.RaceDriver with{StartingCompound=compound,IncidentProfile=new(c.Source.Driver.Ratings["errorControl"],c.Source.Car.Ratings["reliability"]),WetWeather=WeekendPreparation.EffectiveWetWeather(c.Source,w.Setup)})).ToArray();
  var plan=new StrategyPlan([],[]);
  var replay=forcedSafetyCar?SimulationReplay.CaptureDiagnostic(config,drivers,plan,123,new([new(drivers[^1].Id,IncidentKind.Spin,60)])):SimulationReplay.Capture(config,drivers,plan,123);
  var first=Run(SimulationReplay.CreateEngine(replay),forcedSafetyCar);
  var recorded=JsonSerializer.Serialize(replay);var restored=JsonSerializer.Deserialize<RaceReplay>(recorded)!;
  Assert.Equal(first.Trace,Run(SimulationReplay.CreateEngine(restored),forcedSafetyCar).Trace);
  Assert.Equal(22,first.Result.Classification.Count);Assert.All(first.Result.Classification,r=>Assert.True(r.Laps==6||r.RetirementTime is not null));
  Assert.True(first.Result.Finished&&double.IsFinite(first.Result.Time)&&first.Result.Time>0);
  Assert.NotNull(first.Result.Weather);
  if(!forcedSafetyCar&&new[]{"track-shanghai","track-circuit-gilles-villeneuve","track-silverstone","track-interlagos"}.Contains(circuitId))
   Assert.True(first.WeatherWindows>0,"Authored rain-arrival weekends must reach a tyre-condition reaction within the supported six-lap race.");
  if(forcedSafetyCar){Assert.True(first.SafetyWindows>0);Assert.True(first.PitEvents>0);}
  output.WriteLine($"{circuitId}: diagnostic={forcedSafetyCar}; 22 classified; seconds={first.Result.Time:F3}; weatherWindows={first.WeatherWindows}; safetyWindows={first.SafetyWindows}; pitEvents={first.PitEvents}; replay identical.");
 }
 public static IEnumerable<object[]> Circuits()=>new[]{"track-albert-park","track-shanghai","track-suzuka","track-miami","track-circuit-gilles-villeneuve","track-monaco","track-barcelona-catalunya","track-red-bull-ring","track-silverstone","track-spa-francorchamps","track-hungaroring","track-zandvoort","track-monza","track-madring","track-baku","track-sepang","track-marina-bay","track-cota","track-mexico-city","track-interlagos","track-las-vegas","track-lusail","track-yas-marina"}.SelectMany(id=>new[]{new object[]{id,false},new object[]{id,true}});
 private static (string Trace,PhaseResult Result,int WeatherWindows,int SafetyWindows,int PitEvents) Run(RaceEngine engine,bool pit)
 {
  var trace=new List<PhaseResult>();var weather=0;var safety=0;var pits=0;var first=true;
  for(var i=0;i<200&&!engine.GetState().Finished;i++)
  {
   var current=engine.GetState();PhaseResult next;
   if(current.WeatherReaction is {} reaction)
   {
    weather++;next=engine.ResolveWeatherReaction(engine.GetSafetyCarOptions().Where(o=>o.Status!="retired").ToDictionary(o=>o.DriverId,_=>new DriverDecision(reaction.SafetyCarActive?PaceInstruction.Conserve:PaceInstruction.Standard)));
   }
   else if(current.Interruption is not null)
   {
    safety++;next=engine.ResolveSafetyCar(engine.GetSafetyCarOptions().Where(o=>o.Status=="available").ToDictionary(o=>o.DriverId,_=>new SafetyCarDecision()));
   }
   else
   {
    var decisions=engine.GetSafetyCarOptions().Where(o=>o.Status!="retired").ToDictionary(o=>o.DriverId,_=>new DriverDecision());
    if(first&&pit){var entry=decisions.Keys.First();decisions[entry]=new(PitCompound:TyreCompound.Hard);}first=false;
    next=engine.ResolvePhase(decisions);
   }
   Assert.True(next.Time>current.Time);pits+=next.Events.Count(e=>e.Kind==RaceEventKind.PitEntry);trace.Add(next);
  }
  Assert.True(engine.GetState().Finished,"The complete race must terminate within 200 public windows.");
  return(JsonSerializer.Serialize(trace),engine.GetState(),weather,safety,pits);
 }
}
