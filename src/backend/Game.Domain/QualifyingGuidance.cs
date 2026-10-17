namespace Game.Domain;

// Deliberately allowlisted: no attempt seed, ratings, opponent locks or future results.
public sealed record QualifyingPolicyDriver(string EntryId,string? PreparationFit,string? Confidence);
public sealed record QualifyingPolicyObservation(string Session,string TeamId,IReadOnlyList<QualifyingPolicyDriver> Drivers,string PreparationObservationVersion="practice-assessment-v1");
public sealed record QualifyingPolicyCar(QualifyingChoice Choice,IReadOnlyList<string> Reasons,string Uncertainty,IReadOnlyList<string> LegalApproaches);
public sealed record QualifyingPolicyPlan(string PolicyVersion,QualifyingPolicyObservation Observation,IReadOnlyList<QualifyingPolicyCar> Cars);
