using System.Text.Json;
using Game.Application;
using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Api.Tests;

public class WeatherAdviceTests
{
    [Fact]
    public void Wet_advice_uses_only_legitimate_observations_and_preserves_committed_stops()
    {
        var configuration=new RaceConfiguration(10,new("Advice lab"),Weather:new(InitialWetness:.8),WetTyres:new());
        var drivers=new Driver[]{new("a","A"),new("b","A")};
        var first=new RaceEngine(configuration,drivers,17);
        var changedFuture=new RaceEngine(configuration with{Weather=configuration.Weather! with{MeanRain=0,Volatility=0}},drivers,93);
        var observed=first.GetTeamObservation("A");
        var input=new ObservationDecisionInput(0,10,observed,first.GetTimingObservation(),true);
        var plan=ObservationDecisionPolicy.Evaluate(input);
        Assert.All(plan.Cars,car=>Assert.Equal(TyreCompound.Wet,car.Decision.PitCompound));
        Assert.Equal(JsonSerializer.Serialize(plan),JsonSerializer.Serialize(ObservationDecisionPolicy.Evaluate(
            input with{Observation=changedFuture.GetTeamObservation("A") with{Weather=observed.Weather,Forecast=observed.Forecast}})));
        var committed=ObservationDecisionPolicy.Evaluate(input with{Observation=observed with{TeamTyres=observed.TeamTyres.Select(t=>t with{StopQueued=true}).ToArray()},SafetyCarActive=true});
        Assert.All(committed.Cars,car=>{Assert.Null(car.Decision.PitCompound);Assert.Equal(PaceInstruction.Conserve,car.Decision.Pace);Assert.Single(car.PitOptions);Assert.Single(car.PaceOptions);});
        Assert.All(plan.Cars,car=>Assert.Contains("uncertain",car.Uncertainty));
    }
}

