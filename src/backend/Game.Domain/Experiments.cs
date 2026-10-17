namespace Game.Domain;

public record RaceReplay(string ModelVersion, string RandomnessVersion, RaceConfiguration Configuration,
    Driver[] Drivers, StrategyPlan Plan, int Seed,AutomaticStartReplay? Opening=null);
public record RandomChannelMetadata(string Name, double Magnitude, string Units, string Distribution, string Scope);
public record ReplayMetadata(string ModelVersion, string RandomnessVersion,
    IReadOnlyList<RandomChannelMetadata> Channels, string TimeUnit = "seconds", string DistanceUnit = "metres");
public record RandomDiagnostic(string Channel, string DriverId, string Scope, double Value);
public record RaceRun(int Seed, IReadOnlyList<PhaseResult> Phases, PhaseResult Result,
    RaceReplay? Replay = null, ReplayMetadata? Metadata = null, IReadOnlyList<RandomDiagnostic>? Diagnostics = null);
public record ComparisonRequest(RaceConfiguration Configuration, Driver[] Drivers, string TargetDriver,
    StrategyPlan Baseline, StrategyPlan Alternative, int[] Seeds, string? ModelVersion = null, string? RandomnessVersion = null);
public record ComparisonPair(int Seed, RaceRun Baseline, RaceRun Alternative,
    int PositionDifference, int BaselineLaps, int AlternativeLaps, bool BaselineEligible,
    bool AlternativeEligible, double? ComparableTimeDifference);
public record Distribution(int Count, double? Mean, double? StandardDeviation, double? Minimum, double? Maximum);
public record ComparisonGroup(bool BaselineEligible, bool AlternativeEligible, int BaselineLaps, int AlternativeLaps,
    Distribution PositionDifference, Distribution EqualDistanceTimeDifference);
public record ComparisonReport(ComparisonRequest Replay, IReadOnlyList<ComparisonPair> Pairs,
    Distribution EligiblePositionDifference, Distribution EligibleEqualDistanceTimeDifference,
    Distribution DiagnosticPositionDifference, IReadOnlyList<ComparisonGroup> Groups, ReplayMetadata? Metadata = null);
