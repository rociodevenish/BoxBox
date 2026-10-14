using System.Collections.Immutable;
using Game.Domain;
namespace Game.Simulation;

// Authored hypotheses, not inferred physics or researched numerical preference data.
internal static class WeekendSetupTuning
{
    internal static readonly ImmutableDictionary<string, double> MechanicalPreference = new Dictionary<string, double>
    {
        ["adaptable-balance"] = 0, ["aggressive-entry-braking"] = -1, ["aggressive-front"] = -2,
        ["braking-stability"] = 2, ["clean-exit-traction"] = 2, ["confidence-predictability"] = 1,
        ["decisive-front-response"] = -2, ["early-rotation"] = -2, ["entry-rotation"] = -2,
        ["entry-stability"] = 2, ["high-speed-rear-stability"] = 2, ["immediate-turn-in"] = -2,
        ["loose-rear-tolerant"] = -1, ["moderate-rear-stability"] = 1, ["natural-front-feel"] = -1,
        ["neutral-adaptable"] = 0, ["neutral-balance"] = 0, ["neutral-predictable"] = 0,
        ["pointy-front"] = -2, ["positive-front"] = -1, ["predictable-balance"] = 0,
        ["predictable-rear"] = 1, ["progressive-front"] = -1, ["rear-stability"] = 2,
        ["responsive-front"] = -2, ["rotation"] = -2, ["sharp-front"] = -2,
        ["sharp-initial-turn"] = -2, ["stable-car"] = 2, ["stable-entry"] = 2,
        ["stable-rear"] = 2, ["strong-braking-platform"] = 1, ["strong-front"] = -1,
        ["strong-front-feedback"] = -1, ["understeer-tolerant"] = 1
    }.ToImmutableDictionary();

    internal static ImmutableDictionary<string, double> Apply(ImmutableDictionary<string, double> source, WeekendSetup setup)
    {
        // Each axis exchanges capabilities; reliability and information scores are untouched.
        var deltas = new Dictionary<string, double>
        {
            ["topSpeed"] = -4 * setup.Aero, ["accelerationDeployment"] = -2 * setup.Aero,
            ["highSpeedAero"] = 4 * setup.Aero, ["lowSpeedAero"] = 4 * setup.Aero,
            ["mechanicalGrip"] = -3 * setup.Mechanical, ["traction"] = 3 * setup.Mechanical,
            ["tyreWear"] = 2 * setup.Mechanical + 4 * setup.Tyres
        };
        return source.SetItems(deltas.Select(pair => new KeyValuePair<string, double>(pair.Key,
            Math.Clamp(source[pair.Key] + pair.Value, 1, 100))));
    }

    internal static double Mismatch(DriverPreset driver, WeekendSetup setup)
    {
        if (driver.Preferences.IsEmpty) return 0;
        var desired = driver.Preferences.Select(p => MechanicalPreference.TryGetValue(p.Key, out var value)
            ? value : throw new ArgumentException($"Unsupported compatibility trait {p.Key}.")).Average();
        return Math.Abs(setup.Mechanical - desired) / 4 * (1 - driver.Ratings["adaptability"] / 125);
    }

    internal static DriverSetupFit Fit(DriverPreset driver, WeekendSetup setup)
    {
        var mismatch = Mismatch(driver, setup);
        var fit = 100 * (1 - mismatch);
        return new(driver.Id, driver.Name, fit,
            $"{(fit >= 90 ? "Comfortable" : fit >= 80 ? "Manageable compromise" : "Preference mismatch")}; Adaptability {driver.Ratings["adaptability"]:0}/100 moderates mechanical mismatch. Provisional compatibility model.");
    }
}
