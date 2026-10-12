using System.Collections.Immutable;
using Game.Domain;

namespace Game.Simulation;

/// <summary>Public immutable preparation seam. Does not persist or advance a session.</summary>
public static class WeekendPreparation
{
    public const string ModelVersion = "effective-weekend-v1";
    public const string CardVersion = "1.0.0/2026.10.08-initial-balance";
    public const string CarVersion = "car-spec/1.0.0/2026.10.08-initial-model";
    public static readonly ImmutableArray<string> CarKeys = ["topSpeed", "accelerationDeployment",
        "highSpeedAero", "lowSpeedAero", "mechanicalGrip", "braking", "traction", "tyreWear", "tyreTemperature", "reliability"];
    public static readonly ImmutableArray<string> DriverKeys = ["qualifyingPace", "racePace", "overtaking",
        "defending", "starts", "tyrePreservation", "tyreWarmup", "errorControl", "wetWeatherSkill", "setupFeedback", "adaptability"];
    public static readonly ImmutableArray<string> DemandKeys = ["topSpeed", "accelerationDeployment",
        "highSpeedCornering", "lowSpeedCornering", "mechanicalGrip", "braking", "traction", "tyreDegradation", "tyreThermalDemand"];

    public static RaceEngine CreateRace(EffectiveWeekend weekend, int seed)
    {
        ArgumentNullException.ThrowIfNull(weekend);
        if (weekend.ModelVersion != ModelVersion || weekend.Source.Version != ModelVersion)
            throw new ArgumentException("Unsupported effective weekend replay model.");
        return new(weekend.Configuration, weekend.Cars.Select(c => c.RaceDriver).ToArray(), seed);
    }

    public static EffectiveWeekend Prepare(WeekendPreset preset, int laps)
    {
        ArgumentNullException.ThrowIfNull(preset);
        if (preset.Version != ModelVersion || preset.Circuit.Version != CardVersion
            || preset.Engineer.Version != "authored-engineer-v1"
            || preset.Cars.Any(c => c.Car.Version != CarVersion || c.Driver.Version != CardVersion)
            || preset.BaseCarId != "neutral-base-v1"
            || preset.PackageId != "neutral-package-v1" || preset.SetupId != "neutral-setup-v1"
            || preset.Cars.Length != 2 || preset.Cars.Select(c => c.EntryId).Distinct().Count() != 2
            || preset.Cars.Select(c => c.TeamId).Distinct().Count() != 1 || laps < 1)
            throw new ArgumentException("Expected a versioned neutral two-car team preset.");
        Validate(preset.Circuit.Demands, DemandKeys);
        if (!double.IsFinite(preset.Engineer.Setup) || preset.Engineer.Setup is < 1 or > 100)
            throw new ArgumentException("Engineer Setup must be 1–100 design points.");
        var configuration = new RaceConfiguration(laps, preset.Circuit.Track).WithExplicitTuning();
        var cars = preset.Cars.Select((source, index) =>
        {
            Validate(source.Car.Ratings, CarKeys);
            Validate(source.Driver.Ratings, DriverKeys);
            if (string.IsNullOrWhiteSpace(source.EntryId) || string.IsNullOrWhiteSpace(source.TeamId))
                throw new ArgumentException("Stable entry and team identities are required.");
            var car = source.Car.Ratings;
            var driver = source.Driver.Ratings;
            var demand = preset.Circuit.Demands;
            // Whole-lap design-point gains. SegmentSimulation already apportions by baseline time.
            double Contribution(string key, string demandKey) => (car[key] - 82) / 10 * demand[demandKey] / 100;
            var speed = (Contribution("topSpeed", "topSpeed") + Contribution("accelerationDeployment", "accelerationDeployment")) / 2;
            var cornering = (Contribution("highSpeedAero", "highSpeedCornering") + Contribution("lowSpeedAero", "lowSpeedCornering")
                + Contribution("mechanicalGrip", "mechanicalGrip") + Contribution("braking", "braking") + Contribution("traction", "traction")) / 5;
            var raceDriver = new Driver(source.EntryId, source.TeamId, Speed: speed, Cornering: cornering,
                Pace: (driver["racePace"] - 82) / 10, Gap: index * .5,
                TyrePreservation: ((car["tyreWear"] - 82) * demand["tyreDegradation"] / 100 + driver["tyrePreservation"] - 82) / 20,
                TyreManagement: (car["tyreTemperature"] - 82) * demand["tyreThermalDemand"] / 100 / 10,
                Racecraft: (driver["overtaking"] + driver["defending"] - 164) / 20);
            return new EffectiveWeekendCar(source, raceDriver);
        }).ToImmutableArray();
        // Validate the projected configuration at the existing public race boundary.
        _ = new RaceEngine(configuration, cars.Select(c => c.RaceDriver).ToArray(), 0);
        return new(ModelVersion, preset with { Circuit = preset.Circuit with { Track = configuration.Track } }, configuration, cars,
            $"{preset.Circuit.Name}: speed demand {(preset.Circuit.Demands["topSpeed"] + preset.Circuit.Demands["accelerationDeployment"]) / 2:0}/100; "
            + $"high-speed aero demand {preset.Circuit.Demands["highSpeedCornering"]:0}/100; low-speed aero demand {preset.Circuit.Demands["lowSpeedCornering"]:0}/100. "
            + "Neutral Base Car, Race Package and Weekend Setup. "
            + string.Join("; ", cars.Select(c => $"{c.Source.Driver.Name}: race pace {c.Source.Driver.Ratings["racePace"]:0}/100, "
                + $"car Top Speed {c.Source.Car.Ratings["topSpeed"]:0}/100, High-Speed Aero {c.Source.Car.Ratings["highSpeedAero"]:0}/100, "
                + $"preferences {string.Join(", ", c.Source.Driver.Preferences.Select(p => p.Label))}")) + ". Provisional tuning; preferences and engineer advice are inactive in this slice.");
    }

    private static void Validate(ImmutableDictionary<string, double> values, ImmutableArray<string> keys)
    {
        if (values.Count != keys.Length || keys.Any(key => !values.TryGetValue(key, out var value)
            || !double.IsFinite(value) || value is < 1 or > 100))
            throw new ArgumentException("Complete detailed vocabulary with finite 1–100 design points is required.");
    }
}
