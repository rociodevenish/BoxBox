namespace Game.Domain;

public enum TyreCompound { Soft, Medium, Hard }
public enum PaceInstruction { Conserve, Standard, Push }
public record DriverDecision(PaceInstruction Pace = PaceInstruction.Standard);
public record TyreTuning(double DegradationPerLap = .025, double WarmupPenalty = .8,
    double DegradationPenalty = 10, double ExhaustionPenalty = 15, double PushGain = 1,
    double PushWear = 1.65, double ConserveLoss = .8, double ConserveWear = .65,
    double SoftGain = 1.1, double SoftWear = 1.3, double HardLoss = .8, double HardWear = .7,
    double SoftWarmup = .5, double HardWarmup = 1.5);

public record Driver(string Id, string Team, double Speed = 0, double Cornering = 0,
    double Pace = 0, double Gap = 0, TyreCompound StartingCompound = TyreCompound.Medium,
    double TyrePreservation = 0, double TyreManagement = 0);

public record Track(string Name, double BaseLapTime = 90, double SpeedWeight = .5);

public record RaceConfiguration(int Laps, Track Track, double AttributeSeconds = 1,
    double LapVariation = 0, TyreTuning? Tyres = null);

public record TyreState(string DriverId, TyreCompound Compound, int Age, double Condition,
    PaceInstruction Pace);

public record RaceEvent(double Time, string DriverId, int Lap, string Kind, double Value = 0);

public record ClassificationEntry(string DriverId, int Position, int Laps, double Progress,
    double GapSeconds, int GapLaps, double? FinishTime);

public record PhaseResult(int LeaderLap, double Time, bool Finished,
    IReadOnlyList<ClassificationEntry> Classification, IReadOnlyList<RaceEvent> Events,
    IReadOnlyList<TyreState> Tyres);
