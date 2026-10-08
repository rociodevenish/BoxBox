using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;

public record RecordedReplayPhase(int Phase, int LeaderLap, double RaceTime, bool Finished, string OutcomeHash);
public record BrowserReplayRecord(string FormatVersion, string SessionId, string Preset, string ControlledTeamId,
    string OpponentScriptVersion, RaceReplay Replay, IReadOnlyList<RecordedReplayPhase> Phases, bool Finished, BrowserSessionTiming? Timing = null);
public record BrowserPhaseTiming(int Phase, double DecisionMilliseconds, double ResolutionMilliseconds,
    double? ReadyWaitMilliseconds, string Source = "server-monotonic-window-clock");
public record PresentationTimingInput(string SampleId, string DeviceId, string Cursor, double? DurationMilliseconds,
    double? ClientElapsedMilliseconds, string PlaybackKind, string Completion);
public record RecordedPresentationTiming(string SampleId, string DeviceId, string Cursor, int Phase,
    double DurationMilliseconds, double ClientElapsedMilliseconds, string PlaybackKind, string Completion,
    string Source = "client-reported-monotonic-page-clock");
public record BrowserSessionTiming(double ElapsedMilliseconds, double? TotalToFinishMilliseconds,
    IReadOnlyList<BrowserPhaseTiming> PhaseTimings, IReadOnlyList<RecordedPresentationTiming> PresentationSamples);
public record VerifiedReplayPhase(int Phase, bool Matches, int LeaderLap, double RaceTime, bool Finished, string OutcomeHash);
public record BrowserReplayVerification(bool Matches, bool Finished, IReadOnlyList<VerifiedReplayPhase> Phases);

/// <summary>Designer-only replay of accepted complete phase plans through the existing engine.</summary>
public static class BrowserReplay
{
    public const string FormatVersion = "browser-dry-race-replay-v1";
    public static JsonSerializerOptions JsonOptions { get; } = CreateJsonOptions();
    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters = { new JsonStringEnumConverter() } };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
    public static string OutcomeHash(PhaseResult result) => Convert.ToHexString(
        SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(result, JsonOptions)));
    public static BrowserReplayRecord Read(JsonElement document)
    {
        foreach (var name in new[] { "formatVersion", "sessionId", "preset", "controlledTeamId", "opponentScriptVersion", "replay", "phases", "finished" }) Require(document, name);
        var replay = Require(document, "replay");
        foreach (var name in new[] { "modelVersion", "randomnessVersion", "configuration", "drivers", "plan", "seed" }) Require(replay, name);
        var config = Require(replay, "configuration");
        foreach (var name in new[] { "laps", "track", "attributeSeconds", "lapVariation", "tyres", "traffic", "stopVariation", "estimateError", "raceFormVariation", "passingVariation" }) Require(config, name);
        foreach (var name in new[] { "name", "baseLapTime", "speedWeight", "pitLaneLoss", "stationaryStop", "passingChance", "segments" }) Require(Require(config, "track"), name);
        foreach (var name in new[] { "scheduled", "conditional", "defaultPace", "driverPaces" }) Require(Require(replay, "plan"), name);
        return document.Deserialize<BrowserReplayRecord>(JsonOptions) ?? throw new ArgumentException("Replay record must be an object.");
    }
    private static JsonElement Require(JsonElement document, string name)
    {
        if (document.ValueKind == JsonValueKind.Object)
            foreach (var property in document.EnumerateObject())
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        throw new ArgumentException($"Browser replay is missing required {name}.");
    }
    public static BrowserReplayVerification Verify(BrowserReplayRecord record)
    {
        if (record is null || record.FormatVersion != FormatVersion || string.IsNullOrWhiteSpace(record.OpponentScriptVersion)
            || record.Replay is null || record.Replay.Configuration is null || record.Replay.Configuration.Track is null || record.Replay.Configuration.Tyres is null
            || record.Replay.Configuration.Traffic is null || record.Replay.Drivers is null || record.Replay.Plan is null
            || record.Replay.Plan.Scheduled is null || record.Replay.Plan.Conditional is null || record.Replay.Plan.Conditional.Length != 0
            || record.Phases is null || string.IsNullOrWhiteSpace(record.Replay.ModelVersion) || string.IsNullOrWhiteSpace(record.Replay.RandomnessVersion))
            throw new ArgumentException("Browser replay requires its version, explicit inputs and accepted complete phase plans.");
        SimulationReplay.RequireVersion(record.Replay.Configuration, record.Replay.ModelVersion, record.Replay.RandomnessVersion);
        var replay = SimulationReplay.Capture(record.Replay.Configuration, record.Replay.Drivers, record.Replay.Plan, record.Replay.Seed);
        if (replay.Plan.Scheduled.Any(entry => entry is null || entry.Phase < 0 || entry.Phase >= record.Phases.Count)
            || replay.Plan.Scheduled.Length != record.Phases.Count * replay.Drivers.Length)
            throw new ArgumentException("Replay must contain exactly one accepted decision per driver and recorded phase.");
        var engine = new RaceEngine(replay.Configuration, replay.Drivers, replay.Seed);
        var verified = new List<VerifiedReplayPhase>();
        for (var phase = 0; phase < record.Phases.Count; phase++)
        {
            var expected = record.Phases[phase];
            var entries = replay.Plan.Scheduled.Where(entry => entry.Phase == phase).ToArray();
            if (expected is null || expected.Phase != phase || expected.OutcomeHash is null || expected.OutcomeHash.Length != 64
                || !entries.Select(entry => entry.DriverId).Order().SequenceEqual(replay.Drivers.Select(driver => driver.Id).Order()))
                throw new ArgumentException("Replay phases and complete driver identities must be ordered and explicit.");
            if (engine.GetState().Finished) throw new ArgumentException("Replay cannot contain decisions after the finish.");
            var result = engine.ResolvePhase(entries.ToDictionary(entry => entry.DriverId, entry => entry.Decision));
            var hash = OutcomeHash(result);
            verified.Add(new(phase, hash == expected.OutcomeHash && result.Time == expected.RaceTime
                && result.LeaderLap == expected.LeaderLap && result.Finished == expected.Finished,
                result.LeaderLap, result.Time, result.Finished, hash));
        }
        var finished = engine.GetState().Finished;
        return new(verified.All(phase => phase.Matches) && finished == record.Finished, finished, Array.AsReadOnly(verified.ToArray()));
    }
}
