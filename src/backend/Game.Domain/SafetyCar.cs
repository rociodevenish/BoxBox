namespace Game.Domain;
public sealed record SafetyCarTuning(double DurationSeconds=180,double LeaderLapMultiplier=1.8,double CatchUpLapMultiplier=1.2,double FollowingSeconds=1);
public sealed record SafetyCarDecision(TyreCompound? PitCompound = null);
public sealed record SafetyCarOption(string DriverId,string Status,[property:System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))] TyreCompound? CommittedCompound,bool CanPit);
