using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Game.Domain;
namespace Game.Simulation;

/// <summary>One circuit-wide process; callers supply simulation time. Reads never evolve a clock.</summary>
public static class CircuitWeather
{
    public static CurrentWeather Observe(WeatherTuning tuning, int seed, double time)
    {
        tuning.Validate();
        if (!double.IsFinite(time) || time is < 0 or > 604800) throw new ArgumentException("Weather time must be 0–604800 simulation seconds.");
        var interval = (long)Math.Floor(time / tuning.IntervalSeconds);
        var wetness = tuning.InitialWetness;
        for (long i = 0; i <= interval; i++)
        {
            var elapsed = Math.Min(tuning.IntervalSeconds, time - i * tuning.IntervalSeconds);
            var rain = Rain(tuning, seed, i);
            wetness = Math.Clamp(wetness + elapsed * (rain * tuning.WettingPerSecond - (1 - rain) * tuning.DryingPerSecond), 0, 1);
        }
        return new(WeatherTuning.Version, time, Identity(tuning, interval), Rain(tuning, seed, interval), wetness, tuning.DryingPerSecond);
    }

    // The forecast boundary accepts only legitimate observations and authored information quality.
    // It has no process seed or access to future rain draws.
    public static TeamWeatherForecast Forecast(CurrentWeather observation, string team, double quality, double refreshSeconds = 120)
    {
        if (observation is null || observation.Version != WeatherTuning.Version
            || !double.IsFinite(observation.Time) || observation.Time is < 0 or > 604800
            || new[] { observation.RainIntensity, observation.Wetness }.Any(x => !double.IsFinite(x) || x is < 0 or > 1)
            || string.IsNullOrWhiteSpace(observation.IntervalIdentity) || string.IsNullOrWhiteSpace(team) || !double.IsFinite(quality) || quality is < 0 or > 1
            || !double.IsFinite(refreshSeconds) || refreshSeconds is < 10 or > 3600)
            throw new ArgumentException("Invalid forecast observation inputs.");
        var identity = $"weather-forecast-v1:{Uri.EscapeDataString(team)}:{observation.IntervalIdentity}:{observation.Time.ToString("R", CultureInfo.InvariantCulture)}";
        var radius = .35 - .15 * quality;
        var horizons = new[] { 300d, 900d }.Select(horizon =>
        {
            var uncertainty = Math.Min(.49, radius + horizon / 9000);
            var persistence = horizon == 300 ? .7 : .4;
            var estimate = Math.Clamp(.3 * (1 - persistence) + observation.RainIntensity * persistence
                + (Unit(identity + ":" + horizon.ToString(CultureInfo.InvariantCulture)) - .5) * radius, .05, .95);
            return new ForecastHorizon(observation.Time, observation.Time + horizon, estimate,
                Math.Max(0, estimate - uncertainty), Math.Min(1, estimate + uncertainty));
        }).ToArray();
        return new("weather-forecast-v1", identity, observation.Time, observation.Time + refreshSeconds,
            quality >= .75 ? "Moderate" : "Low", Array.AsReadOnly(horizons));
    }

    private static double Rain(WeatherTuning tuning, int seed, long interval) => Math.Clamp(tuning.MeanRain
        + (Unit($"{WeatherTuning.Version}:{seed}:{Identity(tuning, interval)}:rain") * 2 - 1) * tuning.Volatility, 0, 1);
    private static string Identity(WeatherTuning tuning, long interval) => $"{WeatherTuning.Version}:{tuning.SourceVersion}:{interval.ToString(CultureInfo.InvariantCulture)}";
    private static double Unit(string identity)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes) / (double)uint.MaxValue;
    }
}

