namespace Game.Domain;

public enum TyreCompound { Soft, Medium, Hard }
public enum PaceInstruction { Conserve, Standard, Push }
public enum BattleInstruction { Neutral, Attack, Defend }
public record DriverDecision(PaceInstruction Pace = PaceInstruction.Standard, TyreCompound? PitCompound = null,
    BattleInstruction Battle = BattleInstruction.Neutral)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Pace) || !Enum.IsDefined(Battle)
            || PitCompound is { } compound && !Enum.IsDefined(compound))
            throw new ArgumentException("Invalid driver decision.");
    }
}
public record ScheduledDecision(int Phase, string DriverId, DriverDecision Decision);
public enum ConditionField { EstimatedTyreCondition, GapToLeader, LeaderLap, Position }
public enum ConditionComparison { LessThanOrEqual, GreaterThanOrEqual }
public record ConditionalDecision(string DriverId, ConditionField Field, double Threshold,
    DriverDecision Decision, ConditionComparison Comparison = ConditionComparison.LessThanOrEqual, bool Once = true);
public record StrategyPlan(ScheduledDecision[] Scheduled, ConditionalDecision[] Conditional,
    PaceInstruction DefaultPace = PaceInstruction.Standard, IReadOnlyDictionary<string, PaceInstruction>? DriverPaces = null);
public record EstimatedTyreState(string DriverId, TyreCompound Compound, int Age,
    double EstimatedCondition, bool InPits, bool StopQueued);
public record TeamObservation(double Time, int LeaderLap, IReadOnlyList<ClassificationEntry> Classification,
    IReadOnlyList<EstimatedTyreState> TeamTyres,
    IReadOnlyDictionary<string, IReadOnlyList<BattleInstruction>> BattleOptions);
public record DecisionRecord(int Phase, double Time, string DriverId, DriverDecision Decision,
    double EstimatedCondition, string Source);
public record TyreTuning(double DegradationPerLap = .025, double WarmupPenalty = .8,
    double DegradationPenalty = 10, double ExhaustionPenalty = 15, double PushGain = 1,
    double PushWear = 1.65, double ConserveLoss = .8, double ConserveWear = .65,
    double SoftGain = 1.1, double SoftWear = 1.3, double HardLoss = .8, double HardWear = .7,
    double SoftWarmup = .5, double HardWarmup = 1.5);

public record Driver(string Id, string Team, double Speed = 0, double Cornering = 0,
    double Pace = 0, double Gap = 0, TyreCompound StartingCompound = TyreCompound.Medium,
    double TyrePreservation = 0, double TyreManagement = 0, double Racecraft = 0);

public record Track(string Name, double BaseLapTime = 90, double SpeedWeight = .5,
    double PitLaneLoss = 20, double StationaryStop = 3, double PassingChance = .4);

public record TrafficTuning(bool Enabled = true, double FollowingLoss = .3, double BlueFlagLoss = .15,
    double BattleProximity = 3, double AttackMultiplier = 1.7, double DefendMultiplier = .55, double BattleWear = .2);

public record RaceConfiguration(int Laps, Track Track, double AttributeSeconds = 1,
    double LapVariation = 0, TyreTuning? Tyres = null, double StopVariation = .6,
    TrafficTuning? Traffic = null, double EstimateError = .04);

public record TyreState(string DriverId, TyreCompound Compound, int Age, double Condition,
    PaceInstruction Pace, bool InPits = false);

public enum RaceEventKind { LapCompleted, TyreCondition, PitEntry, PitService, PitExit,
    TyreChanged, Overtake, TrafficLoss, BlueFlag, BattleDecision, BattleWear }

// Value is seconds for time-loss/service events and a fraction for TyreCondition.
public record RaceEvent(double Time, string DriverId, int Lap, RaceEventKind Kind, double Value = 0,
    string? OtherDriverId = null, TyreCompound? Compound = null);

public record ClassificationEntry(string DriverId, int Position, int Laps, double Progress,
    double GapSeconds, int GapLaps, double? FinishTime);

public record PhaseResult(int LeaderLap, double Time, bool Finished,
    IReadOnlyList<ClassificationEntry> Classification, IReadOnlyList<RaceEvent> Events,
    IReadOnlyList<TyreState> Tyres, IReadOnlyDictionary<string, bool> Eligibility,
    IReadOnlyDictionary<string, IReadOnlyList<BattleInstruction>> BattleOptions,
    IReadOnlyList<DecisionRecord> Decisions);
