using System.Text.Json;
using Game.Domain;

namespace SimulationRunner;

internal static class JsonInput
{
    public static T Read<T>(string path, JsonSerializerOptions options)
    {
        var text = File.ReadAllText(path);
        using var document = JsonDocument.Parse(text);
        if (typeof(T) == typeof(StrategyPlan))
        {
            var root = document.RootElement;
            ValidateEntries(Required(root, "scheduled"), false);
            ValidateEntries(Required(root, "conditional"), true);
        }
        else if (typeof(T) == typeof(RaceReplay))
        {
            var root = document.RootElement;
            RequireReplayVersions(root);
            foreach (var name in new[] { "configuration", "drivers", "plan", "seed" }) _ = Required(root, name);
            ValidateReplayConfiguration(Required(root, "configuration"));
            ValidatePlan(Required(root, "plan"));
        }
        else if (typeof(T) == typeof(ComparisonRequest))
        {
            var root = document.RootElement;
            RequireReplayVersions(root);
            foreach (var name in new[] { "configuration", "drivers", "targetDriver", "baseline", "alternative", "seeds" }) _ = Required(root, name);
            ValidateReplayConfiguration(Required(root, "configuration"));
            ValidatePlan(Required(root, "baseline"));
            ValidatePlan(Required(root, "alternative"));
        }
        else if (typeof(T) == typeof(ScheduledDecision[])) ValidateEntries(document.RootElement, false);
        else if (typeof(T) == typeof(RaceConfiguration))
        {
            _ = Required(document.RootElement, "laps");
            _ = Required(document.RootElement, "track");
        }
        return JsonSerializer.Deserialize<T>(text, options) ?? throw new ArgumentException($"{path} contains a null input.");
    }

    private static void RequireReplayVersions(JsonElement root)
    {
        foreach (var name in new[] { "modelVersion", "randomnessVersion" })
        {
            var version = Required(root, name);
            if (version.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(version.GetString()))
                throw new ArgumentException("Replay requires nonempty explicit contract versions.");
        }
    }
    private static void ValidatePlan(JsonElement plan)
    {
        ValidateEntries(Required(plan, "scheduled"), false);
        ValidateEntries(Required(plan, "conditional"), true);
    }
    private static void ValidateReplayConfiguration(JsonElement config)
    {
        foreach (var name in new[] { "laps", "track", "attributeSeconds", "lapVariation", "tyres", "stopVariation",
            "traffic", "estimateError", "raceFormVariation", "passingVariation" }) _ = Required(config, name);
        if (Required(config, "tyres").ValueKind != JsonValueKind.Object || Required(config, "traffic").ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Replay requires explicit tyre and traffic tuning.");
        var track = Required(config, "track");
        foreach (var name in new[] { "name", "baseLapTime", "speedWeight", "pitLaneLoss", "stationaryStop", "passingChance", "segments" })
            _ = Required(track, name);
    }

    private static void ValidateEntries(JsonElement entries, bool conditional)
    {
        if (entries.ValueKind != JsonValueKind.Array) throw new ArgumentException("Strategy entries must be arrays.");
        foreach (var entry in entries.EnumerateArray())
        {
            _ = Required(entry, "driverId");
            var decision = Required(entry, "decision");
            if (decision.ValueKind != JsonValueKind.Object) throw new ArgumentException("Decision must be an object.");
            if (conditional) { _ = Required(entry, "field"); _ = Required(entry, "threshold"); }
            else _ = Required(entry, "phase");
        }
    }

    private static JsonElement Required(JsonElement value, string name)
    {
        if (value.ValueKind == JsonValueKind.Object)
            foreach (var property in value.EnumerateObject())
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        throw new ArgumentException($"Missing required property {name}.");
    }
}
