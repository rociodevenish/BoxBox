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

// Length is metres; BaselineTime is seconds. Demands are independent fractions.
public record TrackSegment(string Id, double Length, double BaselineTime,
    double SpeedDemand = .5, double CorneringDemand = .5, double TyreDemand = 1,
    bool PassingAllowed = true, double PassingDifficulty = .4, string? PassingZoneId = null)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || PassingZoneId is not null && string.IsNullOrWhiteSpace(PassingZoneId)
            || !double.IsFinite(Length) || Length <= 0
            || !double.IsFinite(BaselineTime) || BaselineTime <= 0
            || !double.IsFinite(SpeedDemand) || SpeedDemand is < 0 or > 1
            || !double.IsFinite(CorneringDemand) || CorneringDemand is < 0 or > 1
            || !double.IsFinite(TyreDemand) || TyreDemand < 0
            || !double.IsFinite(PassingDifficulty) || PassingDifficulty is < 0 or > 1)
            throw new ArgumentException("Invalid track segment.");
    }
}

public record Track(string Name, double BaseLapTime = 90, double SpeedWeight = .5,
    double PitLaneLoss = 20, double StationaryStop = 3, double PassingChance = .4, IReadOnlyList<TrackSegment>? Segments = null);

public record TrafficTuning(bool Enabled = true, double FollowingLoss = .3, double BlueFlagLoss = .15,
    double BattleProximity = 3, double AttackMultiplier = 1.7, double DefendMultiplier = .55, double BattleWear = .2);

public record RaceConfiguration(int Laps, Track Track, double AttributeSeconds = 1,
    double LapVariation = 0, TyreTuning? Tyres = null, double StopVariation = .6,
    TrafficTuning? Traffic = null, double EstimateError = .04, double RaceFormVariation = 0, double PassingVariation = 1)
{
    public RaceConfiguration WithExplicitTuning() => this with { Tyres = Tyres ?? new(), Traffic = Traffic ?? new(),
        Track = Track with { Segments = Track.Segments is null ? null : Array.AsReadOnly(Track.Segments.ToArray()) } };
}

public record TyreState(string DriverId, TyreCompound Compound, int Age, double Condition,
    PaceInstruction Pace, bool InPits = false);

public enum RaceEventKind { LapCompleted, TyreCondition, PitEntry, PitService, PitExit,
    TyreChanged, Overtake, TrafficLoss, BlueFlag, BattleDecision, BattleWear, SegmentExited, PassingAttempt }

// Value is seconds for time-loss/service events and a fraction for TyreCondition.
public record RaceEvent(double Time, string DriverId, int Lap, RaceEventKind Kind, double Value = 0,
    string? OtherDriverId = null, TyreCompound? Compound = null, string? SegmentId = null, double? DistanceMetres = null, string? EncounterId = null);

public record ClassificationEntry(string DriverId, int Position, int Laps, double Progress,
    double GapSeconds, int GapLaps, double? FinishTime);

public record PhaseResult(int LeaderLap, double Time, bool Finished,
    IReadOnlyList<ClassificationEntry> Classification, IReadOnlyList<RaceEvent> Events,
    IReadOnlyList<TyreState> Tyres, IReadOnlyDictionary<string, bool> Eligibility,
    IReadOnlyDictionary<string, IReadOnlyList<BattleInstruction>> BattleOptions,
    IReadOnlyList<DecisionRecord> Decisions);
