namespace Game.Domain;

public enum TyreCompound { Soft, Medium, Hard }

public record Driver(string Id, string Team, double Speed = 0, double Cornering = 0,
    double Pace = 0, double Gap = 0, TyreCompound StartingCompound = TyreCompound.Medium);

public record Track(string Name, double BaseLapTime = 90, double SpeedWeight = .5);

public record RaceConfiguration(int Laps, Track Track, double AttributeSeconds = 1,
    double LapVariation = 0);

public record RaceEvent(double Time, string DriverId, int Lap, string Kind, double Value = 0);

public record ClassificationEntry(string DriverId, int Position, int Laps, double Progress,
    double GapSeconds, int GapLaps, double? FinishTime);

public record PhaseResult(int LeaderLap, double Time, bool Finished,
    IReadOnlyList<ClassificationEntry> Classification, IReadOnlyList<RaceEvent> Events);
