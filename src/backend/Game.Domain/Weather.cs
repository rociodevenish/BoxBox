namespace Game.Domain;

// Fractions 0–1, seconds on the authoritative weekend clock. Authored hypotheses, not meteorology.
public sealed record WeatherTuning(double MeanRain = .3, double Volatility = .2,
    double InitialWetness = 0, double WettingPerSecond = .004, double DryingPerSecond = .001,
    double IntervalSeconds = 60, double ForecastRefreshSeconds = 120,
    double InformationQuality = .5, double TimeOriginSeconds = 0,
    string SourceVersion = "authored-weather-v1", string ModelVersion = WeatherTuning.Version)
{
    public const string Version = "circuit-weather-v1";
    public void Validate()
    {
        if (new[] { MeanRain, Volatility, InitialWetness, InformationQuality }.Any(x => !double.IsFinite(x) || x is < 0 or > 1)
            || new[] { WettingPerSecond, DryingPerSecond }.Any(x => !double.IsFinite(x) || x is < 0 or > .1)
            || !double.IsFinite(IntervalSeconds) || IntervalSeconds is < 10 or > 3600
            || !double.IsFinite(ForecastRefreshSeconds) || ForecastRefreshSeconds is < 10 or > 3600
            || !double.IsFinite(TimeOriginSeconds) || TimeOriginSeconds is < 0 or > 604800
            || string.IsNullOrWhiteSpace(SourceVersion) || ModelVersion != Version)
            throw new ArgumentException("Invalid bounded weather tuning.");
    }
}
public sealed record CurrentWeather(string Version, double Time, string IntervalIdentity,
    double RainIntensity, double Wetness, double DryingPerSecond);

public sealed record WeatherReactionTuning(double DryBoundary = .25, double WetBoundary = .7,
    double Hysteresis = .03, double MinimumSeparationSeconds = 30)
{
    public const string Version = "weather-reactions-v1";
    public void Validate()
    {
        if (new[] { DryBoundary, WetBoundary, Hysteresis, MinimumSeparationSeconds }.Any(x => !double.IsFinite(x))
            || DryBoundary <= 0 || WetBoundary >= 1 || DryBoundary >= WetBoundary
            || Hysteresis < 0 || Hysteresis >= Math.Min(DryBoundary, Math.Min(1-WetBoundary, (WetBoundary-DryBoundary)/2))
            || MinimumSeparationSeconds is < 0 or > 3600)
            throw new ArgumentException("Invalid weather reaction bands.");
    }
}
public sealed record WeatherReaction(string Identity, double Time, string Band, CurrentWeather Observed,
    bool SafetyCarActive, double? SafetyCarUntil, IReadOnlyList<string> Causes,
    string Policy = "Complete Pace, Pit and contextual Battle choices for both active cars. New stops occur at the next lap end; committed stops remain locked. Under Safety Car control only Conserve and Neutral are legal.");
public sealed record ForecastHorizon(double FromSeconds, double ToSeconds, double RainProbability,
    double RainLower, double RainUpper);
public sealed record TeamWeatherForecast(string Version, string ObservationIdentity, double ObservedAt,
    double RefreshAt, string Confidence, IReadOnlyList<ForecastHorizon> Horizons,
    string Explanation = "Predicted rain, not observed conditions. Probabilities are uncertain; repeated reads do not improve the estimate.");


