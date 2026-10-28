using System.Collections.Immutable;
using Game.Domain;

namespace Game.Simulation;

/// <summary>Public immutable preparation seam. Does not persist or advance a session.</summary>
public static class WeekendPreparation
{
    public static WetWeatherAttributes EffectiveWetWeather(WeekendCar source, WeekendSetup? setup = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        setup?.Validate();
        Validate(source.Driver.Ratings, DriverKeys);
        Validate(source.Car.Ratings, CarKeys);
        var car = setup is null ? source.Car.Ratings : WeekendSetupTuning.Apply(source.Car.Ratings, setup);
        return new(source.Driver.Ratings["wetWeatherSkill"], car["tyreTemperature"], car["traction"],
            $"wet-effective-v1/{source.Driver.Version}/{source.Car.Version}/{(setup is null ? "neutral-setup-v1" : WeekendSetup.Version)}");
    }
    public const string PracticeModelVersion = "practice-assessment-v1";

    public static PracticeAssessment Assess(WeekendPreset preset, int laps, WeekendSetup setup, int seed)
    {
        ArgumentNullException.ThrowIfNull(setup);
        var effective = Prepare(preset, laps, setup);
        var drivers = effective.DriverFits.Select(fit =>
        {
            var source = preset.Cars.Single(car => car.Driver.Id == fit.DriverId);
            var driver = source.Driver;
            // Authored information-quality hypothesis: better feedback narrows uncertainty only.
            var quality = ((source.Engineer ?? preset.Engineer).Setup + driver.Ratings["setupFeedback"]) / 2;
            var radius = 3 + (100 - quality) * .2;
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
                $"{PracticeModelVersion}|{seed}|{fit.DriverId}"));
            var observation = Math.Clamp(fit.Compatibility + (bytes[0] / 255d * 2 - 1) * radius, 0, 100);
            var description = observation >= 90 ? "Comfortable" : observation >= 80 ? "Manageable compromise" : "Preference mismatch";
            var confidence = quality >= 80 ? "High" : quality >= 55 ? "Moderate" : "Low";
            return new PracticeDriverFeedback(fit.DriverId, fit.Name, description, confidence,
                $"Indicative handling observation; uncertainty about {radius:0} compatibility points. No lap-time prediction.",
                $"{description} with this shared Mechanical choice. Adaptability moderates preference mismatch; a change can suit the other driver differently.");
        }).ToImmutableArray();
        var demand = preset.Circuit.Demands;
        var straight = (demand["topSpeed"] + demand["accelerationDeployment"]) / 2;
        var corner = (demand["highSpeedCornering"] + demand["lowSpeedCornering"] + demand["mechanicalGrip"] + demand["braking"] + demand["traction"]) / 5;
        var aeroFocus = setup.Aero < 0 ? "straight performance" : setup.Aero > 0 ? "cornering performance" : "balanced support";
        var circuitDemand = straight == corner ? "straight and cornering demand are balanced"
            : $"{(straight > corner ? "straight performance" : "cornering performance")} has greater relative demand";
        var feedback = new PracticeFeedback(PracticeModelVersion, setup, seed,
            $"{preset.Circuit.Name}: {circuitDemand}. Your Aero choice prioritises {aeroFocus}, trading straight performance against cornering. This is indicative circuit fit, not a predicted result.",
            ["More Downforce exchanges straight performance for cornering.",
             "More Stable exchanges rotation and grip for traction and preservation; both drivers share the compromise.",
             "More Preservation exchanges early tyre response for stint life. Practice consumes no race tyres or components."], drivers);
        return new(feedback, new(effective, effective.DriverFits));
    }
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
        if (weekend.SetupModelVersion is not ("neutral-setup-v1" or WeekendSetup.Version))
            throw new ArgumentException("Unsupported saved setup model.");
        weekend.Setup?.Validate();
        return new(weekend.Configuration, weekend.Cars.Select(c => weekend.Configuration.WetTyres is null ? c.RaceDriver
            : c.RaceDriver with { WetWeather = EffectiveWetWeather(c.Source, weekend.Setup) }).ToArray(), seed);
    }

    public static EffectiveWeekend Prepare(WeekendPreset preset, int laps, WeekendSetup? setup = null)
    {
        ArgumentNullException.ThrowIfNull(preset);
        setup?.Validate();
        var constructed = ConstructedBaseCar.TryAxis(preset, out var constructionAxis);
        var fitSetup = constructed ? (setup ?? new WeekendSetup()) with { Mechanical = Math.Clamp(constructionAxis + (setup?.Mechanical ?? 0), -2, 2) } : setup;
        // Source metadata identifies the frozen materialised values, not the currently
        // bundled catalogue. Compatibility is governed by the model and typed bounds.
        if (preset.Version != ModelVersion || !SupportsSource(preset.Circuit.Version,"1.0.0/")
            || preset.Engineer.Version != "authored-engineer-v1"
            || preset.Cars.Any(c => (!constructed && !SupportsSource(c.Car.Version,"car-spec/1.0.0/")) || !SupportsSource(c.Driver.Version,"1.0.0/"))
            || (!constructed && preset.BaseCarId != "neutral-base-v1")
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
            if (source.Engineer is {} engineer && (engineer.Version != "authored-engineer-v1"
                || !double.IsFinite(engineer.Setup) || engineer.Setup is < 1 or > 100))
                throw new ArgumentException("Assigned Race Engineer Setup must be versioned 1–100 design points.");
            if (string.IsNullOrWhiteSpace(source.EntryId) || string.IsNullOrWhiteSpace(source.TeamId))
                throw new ArgumentException("Stable entry and team identities are required.");
            var car = setup is null ? source.Car.Ratings : WeekendSetupTuning.Apply(source.Car.Ratings, setup);
            var driver = source.Driver.Ratings;
            var demand = preset.Circuit.Demands;
            // Whole-lap design-point gains. SegmentSimulation already apportions by baseline time.
            double Contribution(string key, string demandKey) => (car[key] - 82) / 10 * demand[demandKey] / 100;
            var speed = (Contribution("topSpeed", "topSpeed") + Contribution("accelerationDeployment", "accelerationDeployment")) / 2;
            var cornering = (Contribution("highSpeedAero", "highSpeedCornering") + Contribution("lowSpeedAero", "lowSpeedCornering")
                + Contribution("mechanicalGrip", "mechanicalGrip") + Contribution("braking", "braking") + Contribution("traction", "traction")) / 5;
            var raceDriver = new Driver(source.EntryId, source.TeamId, Speed: speed, Cornering: cornering,
                Pace: (driver["racePace"] - 82) / 10 - (fitSetup is null ? 0 : WeekendSetupTuning.Mismatch(source.Driver, fitSetup) * .15), Gap: index * .5,
                TyrePreservation: ((car["tyreWear"] - 82) * demand["tyreDegradation"] / 100 + driver["tyrePreservation"] - 82) / 20,
                TyreManagement: (car["tyreTemperature"] - 82) * demand["tyreThermalDemand"] / 100 / 10,
                Racecraft: (driver["overtaking"] + driver["defending"] - 164) / 20,
                TyreWarmupMultiplier: setup is null ? 1 : Math.Clamp(1 + setup.Tyres * .12 - (driver["tyreWarmup"] - 82) * .004, .65, 1.35));
            return new EffectiveWeekendCar(source, raceDriver);
        }).ToImmutableArray();
        // Validate the projected configuration at the existing public race boundary.
        _ = new RaceEngine(configuration, cars.Select(c => c.RaceDriver).ToArray(), 0);
        return new(ModelVersion, preset with { Circuit = preset.Circuit with { Track = configuration.Track } }, configuration, cars,
            $"{preset.Circuit.Name}: speed demand {(preset.Circuit.Demands["topSpeed"] + preset.Circuit.Demands["accelerationDeployment"]) / 2:0}/100; "
            + $"high-speed aero demand {preset.Circuit.Demands["highSpeedCornering"]:0}/100; low-speed aero demand {preset.Circuit.Demands["lowSpeedCornering"]:0}/100. "
            + (constructed ? $"Confirmed Base Car {preset.BaseCarId}; neutral Race Package; construction handling and Weekend Setup compose once. " : setup is null ? "Neutral Base Car, Race Package and Weekend Setup. "
                : $"Neutral Base Car and Race Package; Weekend Setup Aero {setup.Aero}, Mechanical {setup.Mechanical}, Tyres {setup.Tyres}. ")
            + string.Join("; ", cars.Select(c => $"{c.Source.Driver.Name}: race pace {c.Source.Driver.Ratings["racePace"]:0}/100, "
                + $"car Top Speed {c.Source.Car.Ratings["topSpeed"]:0}/100, High-Speed Aero {c.Source.Car.Ratings["highSpeedAero"]:0}/100, "
                + $"preferences {string.Join(", ", c.Source.Driver.Preferences.Select(p => p.Label))}"))
            + (constructed ? ". Provisional construction compatibility; reliability uses the existing explicit incident policy. Engineer information has no pace effect." : setup is null ? ". Provisional tuning; preferences and engineer advice are inactive in this slice."
                : ". Speed trades straight pace for aero; Responsive trades grip for traction and preservation; Warm-Up trades first-lap readiness for stint life. Engineer Setup and driver Setup Feedback affect information only."),
            setup, setup is null ? "neutral-setup-v1" : WeekendSetup.Version,
            fitSetup is null ? [] : preset.Cars.Select(c => WeekendSetupTuning.Fit(c.Driver, fitSetup)).ToImmutableArray());
    }

    private static bool SupportsSource(string version,string schema)=>version is not null&&version.StartsWith(schema,StringComparison.Ordinal)&&!string.IsNullOrWhiteSpace(version[schema.Length..]);
    private static void Validate(ImmutableDictionary<string, double> values, ImmutableArray<string> keys)
    {
        if (values.Count != keys.Length || keys.Any(key => !values.TryGetValue(key, out var value)
            || !double.IsFinite(value) || value is < 1 or > 100))
            throw new ArgumentException("Complete detailed vocabulary with finite 1–100 design points is required.");
    }
}
