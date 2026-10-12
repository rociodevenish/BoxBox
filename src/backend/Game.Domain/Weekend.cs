using System.Collections.Immutable;

namespace Game.Domain;

// Detailed ratings are provisional 1–100 design points, never card stars.
public sealed record CarPreset(string Id, string Version, ImmutableDictionary<string, double> Ratings);
public sealed record DriverPreference(string Key, string Label);
public sealed record DriverPreset(string Id, string Name, string Slug, string Version,
    ImmutableDictionary<string, double> Ratings, ImmutableArray<DriverPreference> Preferences);
public sealed record CircuitPreset(string Id, string Name, string Slug, string Version,
    ImmutableDictionary<string, double> Demands, Track Track);
public sealed record EngineerPreset(string Id, string Name, string Version, double Setup);
public sealed record WeekendCar(string EntryId, string TeamId, CarPreset Car, DriverPreset Driver);
public sealed record WeekendPreset(string Version, CircuitPreset Circuit, ImmutableArray<WeekendCar> Cars,
    EngineerPreset Engineer, string BaseCarId = "neutral-base-v1", string PackageId = "neutral-package-v1",
    string SetupId = "neutral-setup-v1");
public sealed record EffectiveWeekendCar(WeekendCar Source, Driver RaceDriver);
public sealed record EffectiveWeekend(string ModelVersion, WeekendPreset Source, RaceConfiguration Configuration,
    ImmutableArray<EffectiveWeekendCar> Cars, string Summary);
