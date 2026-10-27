namespace Game.Domain;

public enum IncidentKind { LockUp, Spin, Contact, MechanicalFailure }
public enum PhaseCompletionReason { NormalBoundary, SafetyCar, Finish, Weather }
public sealed record RaceInterruption(string Identity, double Time, string DriverId, IncidentKind Kind);
// Rates are hazards per running second; contact is probability per actual encounter.
public sealed record IncidentTuning(double ErrorRate = 0, double ContactProbability = 0,
    double FailureRate = 0, double ComponentWearPerSecond = .00001,
    double MechanicalStress = 1, string Version = "local-incidents-v1",
    bool InterruptMajorIncidents = false);
public sealed record IncidentProfile(double ErrorControl = 82, double Reliability = 82,
    double InitialComponentCondition = 1, string SourceVersion = "authored-profile-v1");
public sealed record ForcedIncident(string DriverId, IncidentKind Kind, double Time);
// Designer-only constructor input, never a DriverDecision.
public sealed record IncidentScenario(IReadOnlyList<ForcedIncident> Events);
public sealed record CarCondition(string DriverId, double DamageSecondsPerLap,
    bool Retired, double? RetirementTime);
public sealed record IncidentDiagnostic(string DriverId, double ComponentCondition, double ErrorExposure, double FailureExposure);
public sealed record TeamConditionEstimate(string DriverId, string ComponentCondition, string Risk,
    double ObservedDamageSecondsPerLap, bool Retired);
