using Game.Domain;
using System.Text.Json.Serialization;
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
    DecisionTimingPolicy TimingPolicy,string RulesVersion,int Retain,string BaselinePolicy,IReadOnlyList<QualifyingResult> Results,IReadOnlyList<QualifyingRank> Grid,long WindowRevision,DateTimeOffset? PresentationDeadlineUtc,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] CurrentWeather? Weather=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] QualifyingConditions? Conditions=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] bool? WetTyresEnabled=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? Compounds=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<StartingCompoundSuitability>? Suitability=null);
public sealed record QualifyingTeamSnapshot(string SessionId,string TeamId,long Revision,long AssignmentRevision,string Session,
    string Lifecycle,bool Locked,IReadOnlyList<QualifyingDriver> EligibleDrivers,IReadOnlyList<QualifyingChoice>? Choices,QualifyingAssistance? Assistance=null,string? Source=null,string? PolicyVersion=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] TeamWeatherForecast? Forecast=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<StartingTyreChoice>? StartingCompounds=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<StartingTyreChoice>? StartingFallback=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] StartingTyreGuidance? StartingGuidance=null);
public sealed record StartingTyreGuidance(string HelpLevel,bool AutoAcceptRecommendation,string PolicyVersion,string Explanation,IReadOnlyList<StartingTyreChoice>? Suggested,IReadOnlyList<string> Options);
public sealed record QualifyingAssistance(string HelpLevel,bool AutoAcceptRecommendation,string Session,long WindowRevision,string PolicyVersion,[property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<QualifyingPolicyCar>? Cars);

public sealed record StartingTyreChoice(string EntryId,string Compound);
public sealed record StartingTyresCommand(string CommandId,long? Revision,long? AssignmentRevision,IReadOnlyList<StartingTyreChoice>? Choices);
public sealed record StartingCompoundSuitability(string Compound,string Assessment);
