namespace Game.Domain;

// Explicit observation boundary: no effective attributes, exact fit, replay seed or opponent context.
public sealed record PreparationDriverObservation(string DriverId,string Name,
    IReadOnlyList<DriverPreference> Preferences,double Adaptability);
public sealed record PreparationPracticeObservation(string ModelVersion,WeekendSetup Setup,
    string CircuitFeedback,IReadOnlyList<PracticeDriverFeedback> Drivers);
public sealed record PreparationPolicyObservation(string Version,string SourceVersion,string CircuitName,
    IReadOnlyDictionary<string,double> CircuitDemands,IReadOnlyDictionary<string,double> CarRatings,
    IReadOnlyList<PreparationDriverObservation> Drivers,PreparationPracticeObservation? Practice);
public sealed record PreparationDriverAdvice(string DriverId,string Name,string Preference,double Adaptability,string Feedback);
public sealed record PreparationRecommendation(string PolicyVersion,string ObservationVersion,WeekendSetup Setup,
    IReadOnlyList<string> Reasons,IReadOnlyList<PreparationDriverAdvice> Drivers,string Uncertainty,string ChoiceExplanation);
public sealed record RecordedPreparationChoice(string Stage,PreparationPolicyObservation Observation,
    PreparationRecommendation Recommendation);
