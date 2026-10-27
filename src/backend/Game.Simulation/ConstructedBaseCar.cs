using System.Collections.Immutable;
using System.Globalization;
using Game.Domain;

namespace Game.Simulation;

/// <summary>Materializes a confirmed immutable Base Car without touching personnel or weekend setup.</summary>
public static class ConstructedBaseCar
{
    public const string ModelVersion = "constructed-base-v1";
    private const string Prefix = ModelVersion + "/";

    public static WeekendPreset Apply(WeekendPreset preset, string baseCarId, string catalogueVersion,
        ImmutableDictionary<string, double> ratings, int mechanicalAxis)
    {
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(ratings);
        if (string.IsNullOrWhiteSpace(baseCarId) || baseCarId == "neutral-base-v1"
            || string.IsNullOrWhiteSpace(catalogueVersion) || mechanicalAxis is < -2 or > 2
            || ratings.Count != WeekendPreparation.CarKeys.Length
            || WeekendPreparation.CarKeys.Any(key => !ratings.TryGetValue(key, out var value)
                || !double.IsFinite(value) || value is < 1 or > 100))
            throw new ArgumentException("Confirmed Base Car requires identity, catalogue, bounded handling and complete detailed ratings.");
        var version = $"{Prefix}{mechanicalAxis.ToString(CultureInfo.InvariantCulture)}/{catalogueVersion}";
        if (preset.BaseCarId != "neutral-base-v1")
        {
            if (preset.Cars.Length == 2 && preset.BaseCarId == baseCarId && preset.Cars.All(c => c.Car.Id == baseCarId && c.Car.Version == version && c.Car.Ratings.Count == ratings.Count
                && ratings.All(pair => c.Car.Ratings.TryGetValue(pair.Key, out var value) && value == pair.Value)))
                return preset;
            throw new ArgumentException("A materialized Base Car cannot be replaced.");
        }
        var result = preset with { BaseCarId = baseCarId, Cars = preset.Cars.Select(c => c with {
            Car = new(baseCarId, version, ratings) }).ToImmutableArray() };
        _ = WeekendPreparation.Prepare(result, 1);
        return result;
    }

    internal static bool TryAxis(WeekendPreset preset, out int axis)
    {
        axis = 0;
        if (preset.BaseCarId == "neutral-base-v1" || string.IsNullOrWhiteSpace(preset.BaseCarId) || preset.Cars.Length != 2)
            return false;
        var version = preset.Cars[0].Car.Version;
        if (!version.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        var parts = version[Prefix.Length..].Split('/', 2);
        return parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1])
            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out axis) && axis is >= -2 and <= 2
            && preset.Cars.All(c => c.Car.Id == preset.BaseCarId && c.Car.Version == version
                && c.Car.Ratings.Count == preset.Cars[0].Car.Ratings.Count
                && c.Car.Ratings.All(pair => preset.Cars[0].Car.Ratings.TryGetValue(pair.Key, out var value) && value == pair.Value));
    }
}
