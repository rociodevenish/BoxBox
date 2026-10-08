namespace Game.Application;
public record CreateSessionInput(string Preset, int? Laps, int? Seed);
public record SessionCreated(string SessionId, string DisplayUrl, string ControllerUrl,
    string PlayerId, string TeamId, string DeviceId, string ControllerCapability);
public record PublicGap(string Kind, double? Seconds = null, int? Laps = null);
public record PublicCar(string DriverId, string Name, string TeamId, int Position, int CompletedLaps,
    double Progress, PublicGap GapToLeader, PublicGap GapToAhead, string Compound, int TyreAge,
    string PitState, bool? EligibleAtFinish);
public record PublicEvent(string Id, string Kind, double Time, int Lap, string DriverId,
    string? OtherDriverId, string? Compound);
public record PublicSnapshot(string SessionId, int Phase, long Revision, string Lifecycle,
    int LeaderLap, int TotalLaps, double RaceTime, IReadOnlyList<PublicCar> Classification,
    IReadOnlyList<PublicEvent> Events, PublicPresentation? Presentation = null);
public record PublicPresentation(string Cursor, int ResolvedPhase, int TotalEvents, int Overtakes, int PitEntries,
    int PitExits, int TyreChanges, int OmittedEvents, string Summary);
public record PrivateCar(string DriverId, string Compound, int TyreAge, double EstimatedTyreCondition,
    bool InPits, bool StopQueued, IReadOnlyList<string> BattleOptions);
public record TeamSnapshot(string SessionId, int Phase, long Revision, string Lifecycle,
    string PlayerId, string TeamId, string DeviceId, IReadOnlyList<PrivateCar> Cars, TeamCommand? ConfirmedPlan);
public record CarCommand(string DriverId, string Pace, string Pit, string? Compound, string Battle);
public record TeamCommand(string CommandId, int? Phase, long? Revision, IReadOnlyList<CarCommand> Cars);
public record SubmissionAck(string CommandId, string SessionId, int Phase, long Revision, string Lifecycle);
public enum SessionFailure { InvalidInput, Forbidden, Unavailable, Conflict }
public sealed class SessionFault(SessionFailure failure, string message) : Exception(message)
{ public SessionFailure Failure { get; } = failure; }
