namespace Game.Domain;

// Detailed effective 1–100 design points, never headline stars.
public sealed record WetWeatherAttributes(double WetWeatherSkill = 82, double TyreTemperatureManagement = 82,
    double Traction = 82, string SourceVersion = "authored-wet-attributes-v1")
{
    public void Validate()
    {
        if (new[] { WetWeatherSkill, TyreTemperatureManagement, Traction }.Any(x => !double.IsFinite(x) || x is < 1 or > 100)
            || string.IsNullOrWhiteSpace(SourceVersion)) throw new ArgumentException("Invalid wet effective attributes.");
    }
}

// Pace penalties are seconds/lap; wear and warm-up are multipliers of existing tyre tuning.
public sealed record WetTyreTuning(double DryUnsuitabilitySeconds = 26, double IntermediateUnsuitabilitySeconds = 20,
    double WetUnsuitabilitySeconds = 24, double IntermediateOptimumWetness = .45,
    double IntermediateWear = 1.05, double WetWear = 1.1, double IntermediateWarmup = .8,
    double WetWarmup = 1.1, double WrongConditionWear = 3, double WetSkillSeconds = 4,
    double WetErrorMultiplier = 2, string ModelVersion = WetTyreTuning.Version)
{
    public const string Version = "wet-tyres-v1";
    public const string EligibilityVersion = "actual-race-wet-use-v1";
    public void Validate()
    {
        if (ModelVersion != Version || !double.IsFinite(IntermediateOptimumWetness) || IntermediateOptimumWetness is < .1 or > .9
            || new[] { DryUnsuitabilitySeconds, IntermediateUnsuitabilitySeconds, WetUnsuitabilitySeconds, WetSkillSeconds }
                .Any(x => !double.IsFinite(x) || x is < 0 or > 60)
            || new[] { IntermediateWear, WetWear, IntermediateWarmup, WetWarmup, WrongConditionWear, WetErrorMultiplier }
                .Any(x => !double.IsFinite(x) || x is < 0 or > 10))
            throw new ArgumentException("Invalid bounded wet tyre tuning.");
    }
}
