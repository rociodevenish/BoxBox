using System.Text.Json.Serialization;
namespace Game.Domain;

public sealed record QualifyingRules(int Retain=16,string Version="qualifying-format-v1");
public sealed record QualifyingChoice(string EntryId,string Approach);
public sealed record QualifyingAttemptInput(string Session,int Attempt,int Seed,IReadOnlyList<EffectiveWeekend> Weekends,
    IReadOnlyList<QualifyingChoice> Choices,IReadOnlyList<string> PreSessionOrder,QualifyingRules Rules,
    string ModelVersion="qualifying-attempt-v1",string RandomnessVersion="qualifying-keyed-v1",
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] QualifyingConditions? Conditions=null);
public sealed record QualifyingTime(string EntryId,string DriverId,string TeamId,string Status,int? TimeMilliseconds);
public sealed record QualifyingRank(int Position,string EntryId,string DriverId,string TeamId,string Status,int? TimeMilliseconds,bool Advances);
public sealed record QualifyingResult(string Session,int Attempt,string ModelVersion,string RandomnessVersion,string RulesVersion,
    IReadOnlyList<QualifyingRank> Ranking);

public sealed record QualifyingConditions(string Version,double FromSeconds,double ToSeconds,CurrentWeather Observed);
