using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;

public sealed class PreparationDecisionPolicyTests
{
    private static PreparationPolicyObservation Observation()=>new(PreparationDecisionPolicy.ObservationVersion,
        "authored-policy-control-v1","Control circuit",new Dictionary<string,double>
        { ["topSpeed"]=95,["accelerationDeployment"]=95,["highSpeedCornering"]=50,["lowSpeedCornering"]=50,
          ["mechanicalGrip"]=50,["braking"]=50,["traction"]=50,["tyreDegradation"]=80 },
        new Dictionary<string,double>{["topSpeed"]=82,["highSpeedAero"]=82},
        [new("responsive","Responsive driver",[new("responsive-front","Responsive front")],10),
         new("stable","Stable driver",[new("stable-rear","Stable rear")],100)],null);

    [Fact]
    public void Circuit_and_shared_driver_observations_produce_legal_explained_compromises()
    {
        var input=Observation();var advice=PreparationDecisionPolicy.Evaluate(input);
        Assert.Equal(new WeekendSetup(-1,-1,1),advice.Setup);
        Assert.Equal(2,advice.Drivers.Count);Assert.Contains("uncertain",advice.Uncertainty);
        Assert.All(advice.Drivers,d=>Assert.Contains("No practice",d.Feedback));
        Assert.Contains("All five choices",advice.ChoiceExplanation);
        var corner=input with{CircuitDemands=input.CircuitDemands.ToDictionary(p=>p.Key,p=>p.Key is "topSpeed" or "accelerationDeployment"?30d:p.Value)};
        Assert.Equal(1,PreparationDecisionPolicy.Evaluate(corner).Setup.Aero);
    }

    [Fact]
    public void Final_revision_uses_own_observed_confidence_and_never_requests_another_assessment()
    {
        var input=Observation();
        PreparationPolicyObservation Assessed(string confidence)=>input with{Practice=new("practice-assessment-v1",new(0,2,0),"Indicative circuit fit",
            [new("responsive","Responsive driver","Preference mismatch",confidence,"Wide uncertainty","Observed mismatch"),
             new("stable","Stable driver","Comfortable",confidence,"Wide uncertainty","Observed comfort")])};
        Assert.Equal(2,PreparationDecisionPolicy.Evaluate(Assessed("Low")).Setup.Mechanical);
        Assert.Equal(1,PreparationDecisionPolicy.Evaluate(Assessed("High")).Setup.Mechanical);
        Assert.Contains("Preference mismatch",PreparationDecisionPolicy.Evaluate(Assessed("High")).Drivers[0].Feedback);
    }
}
