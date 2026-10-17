using Game.Domain;
namespace Game.Simulation;

public static class QualifyingDecisionPolicy
{
    public const string Version="qualifying-observation-v1";
    public static QualifyingPolicyPlan Evaluate(QualifyingPolicyObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if(observation.Session is not ("Q1" or "Q2" or "Q3")||string.IsNullOrWhiteSpace(observation.TeamId)||observation.Drivers is null
            ||observation.Drivers.Count==0||observation.Drivers.Any(d=>string.IsNullOrWhiteSpace(d.EntryId))
            ||observation.Drivers.Select(d=>d.EntryId).Distinct().Count()!=observation.Drivers.Count)
            throw new ArgumentException("Supply the team's unique eligible drivers and qualifying session.");
        var cars=observation.Drivers.Select(driver=>
        {
            var cautious=driver.PreparationFit=="Preference mismatch"||driver.Confidence is null or "Low";
            return new QualifyingPolicyCar(new(driver.EntryId,cautious?"Safe":"Push"),[
                cautious?"Disclosed preparation mismatch or low confidence favours lower error exposure.":"Disclosed preparation supports normal maximum effort.",
                "Safe: lower ceiling and error exposure. Push: normal maximum effort. All-Out: higher potential, variance and error exposure."],
                "The outcome remains uncertain. Advice cannot predict a time or hidden error; all legal approaches remain available.",["Safe","Push","All-Out"]);
        }).ToArray();
        return new(Version,observation,cars);
    }
}
