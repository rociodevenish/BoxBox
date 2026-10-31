using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;
public sealed class FullCalendarPitLossTests
{
 [Theory]
 [InlineData("track-monza",22)]
 [InlineData("track-albert-park",21)][InlineData("track-shanghai",23)][InlineData("track-suzuka",22)]
 [InlineData("track-miami",22)][InlineData("track-circuit-gilles-villeneuve",20)][InlineData("track-monaco",20)]
 [InlineData("track-barcelona-catalunya",22)][InlineData("track-red-bull-ring",19)][InlineData("track-silverstone",23)]
 [InlineData("track-spa-francorchamps",24)][InlineData("track-hungaroring",21)][InlineData("track-zandvoort",21)]
 [InlineData("track-madring",22)][InlineData("track-baku",23)][InlineData("track-sepang",23)]
 [InlineData("track-marina-bay",25)][InlineData("track-cota",23)][InlineData("track-mexico-city",22)]
 [InlineData("track-interlagos",21)][InlineData("track-las-vegas",23)][InlineData("track-lusail",22)][InlineData("track-yas-marina",22)]
 public void Nominal_green_flag_pit_loss_matches_the_authored_public_circuit_summary(string circuitId,double expectedSeconds)
 {
  var track=ReleaseContent.FullCalendar().Circuits.Single(c=>c.Id==circuitId).Track;
  var config=new RaceConfiguration(6,track,Tyres:new(DegradationPerLap:0,WarmupPenalty:0,DegradationPenalty:0,ExhaustionPenalty:0),StopVariation:0,Traffic:new(Enabled:false));
  var race=new RaceEngine(config,[new("pitter","pit-team"),new("reference","reference-team")],123);
  race.ResolvePhase(new Dictionary<string,DriverDecision>{{"pitter",new(PitCompound:TyreCompound.Medium)}});
  while(!race.GetState().Finished)race.ResolvePhase();
  var result=race.GetState();var pitter=result.Classification.Single(c=>c.DriverId=="pitter");var reference=result.Classification.Single(c=>c.DriverId=="reference");
  Assert.Equal(expectedSeconds,pitter.FinishTime!.Value-reference.FinishTime!.Value,6);
 }
}
