using Game.Domain;
namespace Game.Application;

public record BeginPreparationInput(string CircuitId = "track-monza");
public record PracticeCommand(string CommandId, long? Revision, long? AssignmentRevision);
public record FreezePreparationCommand(string CommandId, long? Revision);
public record FreezePreparationAck(string CommandId, string SessionId, long Revision, string ContextId);
public record FrozenPreparedTeam(string TeamId, long Revision, string Source, WeekendPreset Preset,
    WeekendSetup Setup, EffectiveWeekend Effective,IReadOnlyList<RecordedPreparationChoice>? AutomaticChoices=null);
public record PreparationGuidance(string HelpLevel,bool AutoAcceptRecommendation,PreparationRecommendation? Recommendation);
public record PreparationAutomaticRecord(string TeamId,string Source,IReadOnlyList<RecordedPreparationChoice> Choices);
public record FrozenPreparationContext(string ContextId, string ModelVersion, IReadOnlyList<FrozenPreparedTeam> Teams);
public record PreparationReadiness(string TeamId, bool FinalReady, bool Locked);
public record PracticeSnapshot(string ModelVersion, WeekendSetup Setup, string CircuitFeedback,
    IReadOnlyList<string> Compromises, IReadOnlyList<PracticeDriverFeedback> Drivers);
public record SetupCommand(string CommandId, long? Revision, long? AssignmentRevision, WeekendSetup? Setup);
public record PreparationPublicSnapshot(string SessionId, long Revision, string Lifecycle, CircuitPreset Circuit,
    IReadOnlyList<string> TeamIds, IReadOnlyList<PreparationReadiness> Readiness,
    [property:System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] CurrentWeather? Weather=null);
public record PreparationTeamSnapshot(string SessionId, string TeamId, long Revision, long AssignmentRevision,
    string Lifecycle, WeekendSetup Setup, WeekendPreset Preset,
    string Summary, string ModelVersion, PracticeSnapshot? Assessment,PreparationGuidance Guidance,
    [property:System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] CurrentWeather? Weather=null,
    [property:System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] TeamWeatherForecast? Forecast=null);
public record SetupAck(string CommandId, string SessionId, string TeamId, long Revision, string Lifecycle);
