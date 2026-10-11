namespace Game.Application;
public record CreateSessionInput(string Preset, int? Laps, int? Seed);
public record SessionCreated(string SessionId, string DisplayUrl, string ControllerUrl,
    string PlayerId, string TeamId, string DeviceId, string ControllerCapability);
public record PublicGap(string Kind, double? Seconds = null, int? Laps = null);
public record PublicLapTime(int Lap, double Seconds);
public record PublicCar(string DriverId, string Name, string TeamId, int Position, int CompletedLaps,
    double Progress, PublicGap GapToLeader, PublicGap GapToAhead, string Compound, int TyreAge,
    string PitState, bool? EligibleAtFinish, IReadOnlyList<PublicLapTime> RecentLapTimes,
    int CompletedPitStops, bool IsFastestLap);
public record PublicEvent(string Id, string Kind, double Time, int Lap, string DriverId,
    string? OtherDriverId, string? Compound);
public record PublicSnapshot(string SessionId, int Phase, long Revision, string Lifecycle,
    int LeaderLap, int TotalLaps, double RaceTime, IReadOnlyList<PublicCar> Classification,
    IReadOnlyList<PublicEvent> Events, PublicPresentation? Presentation = null, int NextDecisionLaps = 0, IReadOnlyList<TeamReadiness>? Readiness = null,bool Paused=false,DateTimeOffset? DecisionDeadlineUtc=null,double? PausedRemainingSeconds=null,DecisionTimingPolicy? TimingPolicy=null,DateTimeOffset? ServerTimeUtc=null,IReadOnlyList<TeamDecisionSource>? DecisionSources=null,IReadOnlyList<TeamControlStatus>? ControlStatus=null);
public record TeamReadiness(string TeamId, bool Locked);
public record OpenTeamSnapshot(string SessionId,int Phase,long Revision,string Lifecycle,string TeamId,
    IReadOnlyList<PrivateCar> Cars,TeamCommand? ConfirmedPlan,long AssignmentRevision);
public record PublicPresentation(string Cursor, int ResolvedPhase, int TotalEvents, int Overtakes, int PitEntries,
    int PitExits, int TyreChanges, int OmittedEvents, string Summary);
public record PrivateCar(string DriverId, string Compound, int TyreAge, double EstimatedTyreCondition,
    bool InPits, bool StopQueued, IReadOnlyList<string> BattleOptions);
public record TeamSnapshot(string SessionId, int Phase, long Revision, string Lifecycle,
    string PlayerId, string TeamId, string DeviceId, IReadOnlyList<PrivateCar> Cars, TeamCommand? ConfirmedPlan, long AssignmentRevision = 0, long CommandRevision = 0,TeamControlStatus? ControlStatus=null);
public record CarCommand(string DriverId, string Pace, string Pit, string? Compound, string Battle);
public record TeamCommand(string CommandId, int? Phase, long? Revision, IReadOnlyList<CarCommand> Cars);
public record SubmissionAck(string CommandId, string SessionId, int Phase, long Revision, string Lifecycle);
public enum SessionFailure { InvalidInput, Forbidden, Unavailable, Conflict, StorageUnavailable }
public sealed class SessionFault(SessionFailure failure, string message,PublicSnapshot? currentState=null) : Exception(message)
{ public SessionFailure Failure { get; } = failure; public PublicSnapshot? CurrentState {get;}=currentState; }
