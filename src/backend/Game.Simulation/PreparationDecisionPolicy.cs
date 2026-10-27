using Game.Domain;
namespace Game.Simulation;

/// <summary>Deterministic authored setup guidance using legitimate observations only.</summary>
public static class PreparationDecisionPolicy
{
    public const string Version="preparation-observation-v1";
    public const string ObservationVersion="preparation-observation-input-v1";
    public static PreparationRecommendation Evaluate(PreparationPolicyObservation input)
    {
        if(input.Version!=ObservationVersion || input.Drivers.Count!=2)
            throw new ArgumentException("Expected versioned observations for both drivers.");
        double Demand(string key)=>input.CircuitDemands.TryGetValue(key,out var value)&&double.IsFinite(value)&&value is >=0 and <=100
            ?value:throw new ArgumentException("Expected bounded circuit demands.");
        foreach(var driver in input.Drivers)
            if(!double.IsFinite(driver.Adaptability)||driver.Adaptability is <1 or >100||driver.Preferences.Any(p=>!WeekendSetupTuning.MechanicalPreference.ContainsKey(p.Key)))
                throw new ArgumentException("Expected supported preferences and Adaptability.");
        var straight=(Demand("topSpeed")+Demand("accelerationDeployment"))/2;
        var corner=(Demand("highSpeedCornering")+Demand("lowSpeedCornering")+Demand("mechanicalGrip")+Demand("braking")+Demand("traction"))/5;
        var aero=straight-corner>5?-1:corner-straight>5?1:0;
        var weight=input.Drivers.Sum(d=>125-d.Adaptability);
        var preferred=input.Drivers.Sum(d=>(d.Preferences.Count==0?0:d.Preferences.Average(p=>WeekendSetupTuning.MechanicalPreference[p.Key]))*(125-d.Adaptability))/weight;
        var mechanical=Math.Clamp((int)Math.Round(preferred,MidpointRounding.AwayFromZero),-2,2);
        var tyres=Demand("tyreDegradation")>=65?1:Demand("tyreDegradation")<55?-1:0;
        // Legitimate uncertain feedback can justify one step toward the shared preference compromise.
        // Low-confidence feedback does not provoke a new change or another assessment.
        if(input.Practice is not null)
        {
            input.Practice.Setup.Validate();
            var actionable=input.Practice.Drivers.Any(d=>d.Fit=="Preference mismatch"&&d.Confidence is "Moderate" or "High");
            mechanical=actionable?input.Practice.Setup.Mechanical+Math.Sign(mechanical-input.Practice.Setup.Mechanical):input.Practice.Setup.Mechanical;
        }
        var setup=new WeekendSetup(aero,mechanical,tyres);setup.Validate();
        var drivers=input.Drivers.Select(driver=>new PreparationDriverAdvice(driver.DriverId,driver.Name,
            driver.Preferences.Count==0?"No directional preference supplied":string.Join(", ",driver.Preferences.Select(p=>p.Label)),driver.Adaptability,
            input.Practice?.Drivers.SingleOrDefault(d=>d.DriverId==driver.DriverId) is {} feedback
                ?$"{feedback.Fit}. Confidence: {feedback.Confidence}. {feedback.Uncertainty}"
                :"No practice observation yet. This shared Mechanical compromise can suit the two drivers differently.")).ToArray();
        var ownStrength=input.CarRatings.TryGetValue("topSpeed",out var speed)&&input.CarRatings.TryGetValue("highSpeedAero",out var downforce)
            ?$"Own base-car Top Speed {speed:0}/100 and High-Speed Aero {downforce:0}/100 are design inputs, not lap-time predictions."
            :"Own car information is incomplete; retain a cautious compromise.";
        return new(Version,input.Version,setup,
            [$"{input.CircuitName}: {(straight>corner?"straight performance":"cornering performance")} has greater relative demand. Aero trades straight performance against cornering.",
             ownStrength,"Mechanical balances both drivers' preferences, weighting lower Adaptability more strongly; it trades mechanical grip for traction and preservation.",
             "Tyres trades warm-up response against stint preservation. No choice upgrades the base car."],drivers,
            "Indicative guidance from legitimate observations. Practice confidence describes information quality; future qualifying and race outcomes remain uncertain.",
            "All five choices on each axis remain available. Apply advice explicitly or keep your own draft; one setup serves both cars.");
    }
}
