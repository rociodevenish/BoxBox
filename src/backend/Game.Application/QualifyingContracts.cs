using Game.Domain;
namespace Game.Application;

public sealed record StartQualifyingCommand(string CommandId,long? Revision);
public sealed record StartQualifyingAck(string CommandId,string SessionId,long Revision,string Session,string ContextId);
public sealed record QualifyingCommand(string CommandId,string Session,long? Revision,long? AssignmentRevision,IReadOnlyList<QualifyingChoice>? Choices);
public sealed record QualifyingAck(string CommandId,string SessionId,string TeamId,string Session,long Revision,string Lifecycle);
public sealed record QualifyingDriver(string EntryId,string DriverId,string Name,string Slug,string TeamId);
public sealed record QualifyingTeamStatus(string TeamId,bool Locked,string? Source);
public sealed record QualifyingPublicSnapshot(string SessionId,long Revision,string Session,string Lifecycle,bool Paused,
    string CircuitId,string CircuitName,string CircuitSlug,IReadOnlyList<QualifyingDriver> Drivers,
    IReadOnlyList<QualifyingTeamStatus> Teams,QualifyingResult? Result,DateTimeOffset? DecisionDeadlineUtc,
    DecisionTimingPolicy TimingPolicy,string RulesVersion,int Retain,string BaselinePolicy);
public sealed record QualifyingTeamSnapshot(string SessionId,string TeamId,long Revision,long AssignmentRevision,string Session,
    string Lifecycle,bool Locked,IReadOnlyList<QualifyingDriver> EligibleDrivers,IReadOnlyList<QualifyingChoice>? Choices);
