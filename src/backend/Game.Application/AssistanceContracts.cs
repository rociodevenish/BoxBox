using System.Text.Json.Serialization;
namespace Game.Application;
public sealed record PlayerAssistanceSettings(string HelpLevel="Standard",bool AutoAcceptRecommendation=false);
public sealed record PlayerAssistanceInput(string HelpLevel,bool AutoAcceptRecommendation);
public sealed record AssistanceCar(string DriverId,CarCommand Suggested,IReadOnlyList<string> Reasons,string Uncertainty,
    IReadOnlyList<string> PaceOptions,IReadOnlyList<string?> PitOptions,IReadOnlyList<string> BattleOptions,bool Confident);
public sealed record AssistanceRecommendation(int Phase,long WindowRevision,double ObservationTime,IReadOnlyList<AssistanceCar> Cars,string ChoiceExplanation);
public sealed record TeamAssistance(string HelpLevel,bool AutoAcceptRecommendation,int Phase,
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] AssistanceRecommendation? Recommendation=null);
