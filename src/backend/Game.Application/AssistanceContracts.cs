using System.Text.Json.Serialization;
namespace Game.Application;
public sealed record PlayerAssistanceSettings(string HelpLevel="Standard",bool AutoAcceptRecommendation=false);
public sealed record PlayerAssistanceInput(string HelpLevel,bool AutoAcceptRecommendation);
public sealed record AssistanceCar(string DriverId,CarCommand Suggested,IReadOnlyList<string> Reasons,string Uncertainty,
    IReadOnlyList<string> PaceOptions,IReadOnlyList<string?> PitOptions,IReadOnlyList<string> BattleOptions,bool Confident);
public sealed record SafetyCarAdvice(string DriverId,SafetyCarCarCommand Suggested,IReadOnlyList<string> Reasons,string Uncertainty);
public sealed record AssistanceRecommendation(int Phase,long WindowRevision,double ObservationTime,IReadOnlyList<AssistanceCar> Cars,string ChoiceExplanation, [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string? PolicyVersion=null,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<SafetyCarAdvice>? SafetyCarCars=null);
public sealed record RecordedAssistance(int Phase,string TeamId,string HelpLevel,bool AutoAcceptRecommendation,string PolicyVersion,AssistanceRecommendation Recommendation);
public sealed record TeamAssistance(string HelpLevel,bool AutoAcceptRecommendation,int Phase,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] AssistanceRecommendation? Recommendation=null);
