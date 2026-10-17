namespace Game.Domain;
public sealed record SafetyCarDecision(TyreCompound? PitCompound = null);
public sealed record SafetyCarOption(string DriverId,string Status,[property:System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))] TyreCompound? CommittedCompound,bool CanPit);
