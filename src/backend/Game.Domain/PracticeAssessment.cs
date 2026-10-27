using System.Collections.Immutable;

namespace Game.Domain;

public sealed record PracticeDriverFeedback(string DriverId, string Name, string Fit,
    string Confidence, string Uncertainty, string Feedback);
public sealed record PracticeFeedback(string ModelVersion, WeekendSetup Setup, int Seed,
    string CircuitFeedback, ImmutableArray<string> Compromises, ImmutableArray<PracticeDriverFeedback> Drivers);
// Designer-only result. Session projections must select Feedback explicitly.
public sealed record PracticeDiagnostics(EffectiveWeekend Effective, ImmutableArray<DriverSetupFit> ExactFits);
public sealed record PracticeAssessment(PracticeFeedback Feedback, PracticeDiagnostics Diagnostics);
