using Game.Domain;
namespace Game.Application;

/// <summary>Only disclosed observations and legal own-car state enter this policy.</summary>
public static class WeatherDecisionPolicy
{
    public const string Version="weather-observation-ai-v1";
    public static TyreCompound StartingCompound(CurrentWeather observed)=>observed.Wetness<.25?TyreCompound.Medium:
        observed.Wetness<.7?TyreCompound.Intermediate:TyreCompound.Wet;

    public static ObservationDecisionPlan Evaluate(ObservationDecisionInput input)
    {
        var observed=input.Observation.Weather??throw new ArgumentException("Observed weather is required.");
        var suited=StartingCompound(observed);
        var cars=new List<CarDecisionAdvice>();
        foreach(var tyre in input.Observation.TeamTyres.OrderBy(t=>t.DriverId,StringComparer.Ordinal))
        {
            var row=input.Observation.Classification.Single(c=>c.DriverId==tyre.DriverId);
            if(row.RetirementTime is not null||row.FinishTime is not null)continue;
            var unavailable=tyre.InPits||tyre.StopQueued;
            var wetUsed=tyre.UsedCompounds?.Any(c=>c is TyreCompound.Intermediate or TyreCompound.Wet)==true;
            var needsDry=!wetUsed && (tyre.UsedCompounds??[tyre.Compound]).Distinct().Count()<2;
            var currentSuited=suited==TyreCompound.Medium?tyre.Compound is TyreCompound.Soft or TyreCompound.Medium or TyreCompound.Hard:tyre.Compound==suited;
            TyreCompound? pit=unavailable?null:!currentSuited||tyre.EstimatedCondition<.38?suited:
                suited==TyreCompound.Medium&&needsDry&&row.Laps>=input.TotalLaps/2?tyre.Compound==TyreCompound.Hard?TyreCompound.Medium:TyreCompound.Hard:null;
            var reasons=new List<string>{"Observed track wetness is "+(observed.Wetness*100).ToString("F0",System.Globalization.CultureInfo.InvariantCulture)+"%; the coarse current suitability choice is "+suited+"."};
            if(unavailable)reasons.Add(tyre.StopQueued?"The accepted stop is queued for the next lap end and cannot be replaced.":"The current stop is in progress and cannot be replaced.");
            else reasons.Add(pit is null?"Keep the current compound while it suits the observed band.":"Consider "+pit+" at the next lap end; conditions may change before arrival.");
            reasons.Add(wetUsed?"Actual wet-compound use has met the tyre eligibility exemption.":"Two dry compounds are required unless Intermediate or Wet is actually raced on.");
            if(input.SafetyCarActive)reasons.Add("Safety Car control requires Conserve and Neutral; the existing neutralisation deadline continues.");
            var forecast=input.Observation.Forecast;
            if(forecast is not null)reasons.Add("Predicted rain has "+forecast.Confidence+" confidence; current suitability is observed, not a prediction.");
            cars.Add(new(tyre.DriverId,new(input.SafetyCarActive?PaceInstruction.Conserve:PaceInstruction.Standard,pit),reasons,
                "Forecast rain, tyre estimates and stop outcomes remain uncertain. This recommendation uses current conditions; it does not know future rain.",
                input.SafetyCarActive?[PaceInstruction.Conserve]:Enum.GetValues<PaceInstruction>(),
                unavailable?[null]:[null,TyreCompound.Soft,TyreCompound.Medium,TyreCompound.Hard,TyreCompound.Intermediate,TyreCompound.Wet],
                input.SafetyCarActive?[BattleInstruction.Neutral]:input.Observation.BattleOptions[tyre.DriverId],false));
        }
        return new(input.Phase,input.Observation.Time,Version,cars);
    }
}
