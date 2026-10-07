namespace Game.Domain;

public record RaceRun(int Seed, IReadOnlyList<PhaseResult> Phases, PhaseResult Result);
public record ComparisonRequest(RaceConfiguration Configuration, Driver[] Drivers, string TargetDriver,
    StrategyPlan Baseline, StrategyPlan Alternative, int[] Seeds);
public record ComparisonPair(int Seed, RaceRun Baseline, RaceRun Alternative,
    int PositionDifference, int BaselineLaps, int AlternativeLaps, bool BaselineEligible,
    bool AlternativeEligible, double? ComparableTimeDifference);
public record Distribution(int Count, double? Mean, double? StandardDeviation, double? Minimum, double? Maximum);
public record ComparisonGroup(bool BaselineEligible, bool AlternativeEligible, int BaselineLaps, int AlternativeLaps,
    Distribution PositionDifference, Distribution EqualDistanceTimeDifference);
public record ComparisonReport(ComparisonRequest Replay, IReadOnlyList<ComparisonPair> Pairs,
    Distribution EligiblePositionDifference, Distribution EligibleEqualDistanceTimeDifference,
    Distribution DiagnosticPositionDifference, IReadOnlyList<ComparisonGroup> Groups);
