using Game.Domain;
namespace Game.Simulation;

public sealed partial class RaceEngine
{
    private WeatherReaction? weatherReaction;
    private int weatherBand;
    private double lastWeatherReaction = double.NegativeInfinity;

    private int InitialWeatherBand(double wetness) => configuration.WeatherReactions is not {} bands ? 0
        : wetness < bands.DryBoundary ? 0 : wetness < bands.WetBoundary ? 1 : 2;

    // Within a rain interval wetness is linear. Search only this interval; the shared clock drains
    // its endpoint before querying the next interval. Semantic effect cells remain independent.
    private double NextWeatherReactionTime()
    {
        if (chequered || weatherReaction is not null || configuration.WeatherReactions is not {} bands
            || configuration.Weather is not {} weather) return double.PositiveInfinity;
        var observed = GetCurrentWeather()!;
        var rate = observed.RainIntensity * weather.WettingPerSecond - (1-observed.RainIntensity) * weather.DryingPerSecond;
        var knot = (Math.Floor(observed.Time/weather.IntervalSeconds+1e-10)+1)*weather.IntervalSeconds-weather.TimeOriginSeconds;
        var eligible = Math.Max(time, lastWeatherReaction + bands.MinimumSeparationSeconds);
        if (eligible > knot+1e-8) return double.PositiveInfinity;
        var wetness = Math.Clamp(observed.Wetness + (eligible-time)*rate, 0, 1);
        var upper = weatherBand switch { 0 => bands.DryBoundary+bands.Hysteresis,
            1 => bands.WetBoundary+bands.Hysteresis, _ => double.PositiveInfinity };
        var lower = weatherBand switch { 2 => bands.WetBoundary-bands.Hysteresis,
            1 => bands.DryBoundary-bands.Hysteresis, _ => double.NegativeInfinity };
        // A crossing delayed by separation remains applicable even after rain flattens or
        // reverses. Test both remembered-band edges before predicting a future crossing.
        if (wetness >= upper || wetness <= lower) return eligible;
        if (Math.Abs(rate) < 1e-14) return double.PositiveInfinity;
        var threshold = rate > 0 ? weatherBand switch {
            0 => bands.DryBoundary + bands.Hysteresis, 1 => bands.WetBoundary + bands.Hysteresis, _ => double.PositiveInfinity }
            : weatherBand switch { 2 => bands.WetBoundary-bands.Hysteresis, 1 => bands.DryBoundary-bands.Hysteresis, _ => double.NegativeInfinity };
        var crossing = eligible + Math.Max(0, (threshold-wetness)/rate);
        return crossing <= knot+1e-8 ? crossing : double.PositiveInfinity;
    }

    private void OpenWeatherReaction()
    {
        var observed = GetCurrentWeather()!;
        var tuning = configuration.WeatherReactions!;
        var upper=weatherBand==0?tuning.DryBoundary+tuning.Hysteresis:tuning.WetBoundary+tuning.Hysteresis;
        // Conditions at a rain knot include the new rain intensity, but the crossing direction
        // belongs to the interval that just elapsed, so infer it from the reached wetness edge.
        weatherBand = Math.Clamp(weatherBand + (weatherBand<2 && observed.Wetness>=upper-1e-9 ? 1 : -1),0,2);
        lastWeatherReaction = time;
        weatherReaction = new(FormattableString.Invariant($"{WeatherReactionTuning.Version}:{time:R}:{weatherBand}"),time,
            new[] { "Dry", "Intermediate", "Wet" }[weatherBand], observed,neutralised,
            neutralised ? neutralisedUntil : null, ["Weather"]);
    }

    public PhaseResult ResolveWeatherReaction(IReadOnlyDictionary<string,DriverDecision> choices)
    {
        if (weatherReaction is null || Snapshot().Finished) throw new ArgumentException("No weather reaction awaits choices.");
        var required = cars.Where(Active).Select(c=>c.Driver.Id).Order().ToArray();
        if (choices is null || !choices.Keys.Order().SequenceEqual(required))
            throw new ArgumentException("Choose complete instructions for exactly the active cars.");
        // Coincident incident deployment starts neutralisation exactly once. Already active control
        // keeps its original deadline, including real lane travel and remaining duration.
        var deploy = interruption is not null && !neutralised;
        return ResolvePhaseCore(choices, deploy);
    }
}
