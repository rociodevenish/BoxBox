using Game.Domain;
namespace Game.Application;

public record BeginPreparationInput(string CircuitId = "track-monza");
public record PracticeCommand(string CommandId, long? Revision, long? AssignmentRevision);
public record PracticeSnapshot(string ModelVersion, WeekendSetup Setup, string CircuitFeedback,
    IReadOnlyList<string> Compromises, IReadOnlyList<PracticeDriverFeedback> Drivers);
public record SetupCommand(string CommandId, long? Revision, long? AssignmentRevision, WeekendSetup? Setup);
public record PreparationPublicSnapshot(string SessionId, long Revision, string Lifecycle, CircuitPreset Circuit,
    IReadOnlyList<string> TeamIds);
public record PreparationTeamSnapshot(string SessionId, string TeamId, long Revision, long AssignmentRevision,
    string Lifecycle, WeekendSetup Setup, WeekendPreset Preset, IReadOnlyList<DriverSetupFit> DriverFits,
    string Summary, string ModelVersion, PracticeSnapshot? Assessment);
public record SetupAck(string CommandId, string SessionId, string TeamId, long Revision, string Lifecycle);
