using Game.Domain;
namespace Game.Application;

public record BeginPreparationInput(string CircuitId = "track-monza");
public record SetupCommand(string CommandId, long? Revision, long? AssignmentRevision, WeekendSetup? Setup);
public record PreparationPublicSnapshot(string SessionId, long Revision, string Lifecycle, CircuitPreset Circuit,
    IReadOnlyList<string> TeamIds);
public record PreparationTeamSnapshot(string SessionId, string TeamId, long Revision, long AssignmentRevision,
    string Lifecycle, WeekendSetup Setup, WeekendPreset Preset, IReadOnlyList<DriverSetupFit> DriverFits,
    string Summary, string ModelVersion);
public record SetupAck(string CommandId, string SessionId, string TeamId, long Revision, string Lifecycle);
