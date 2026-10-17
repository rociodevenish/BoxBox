using Game.Domain;

namespace Game.Simulation;

internal sealed class StrategyEvaluator
{
    private readonly StrategyPlan plan;
    private readonly HashSet<(string Driver, int Rule)> consumed = [];

    public StrategyEvaluator(StrategyPlan source, IReadOnlyList<string> drivers, int laps)
    {
        Validate(source, drivers, laps);
        plan = source with { Scheduled = source.Scheduled.ToArray(), Conditional = source.Conditional.ToArray(),
            DriverPaces = source.DriverPaces?.ToDictionary() };
    }

    public void RequireSamePlan(StrategyPlan source)
    {
        if (source is null || source.DefaultPace != plan.DefaultPace || source.Scheduled is null
            || source.Conditional is null || !source.Scheduled.SequenceEqual(plan.Scheduled)
            || !source.Conditional.SequenceEqual(plan.Conditional)
            || !(source.DriverPaces ?? new Dictionary<string, PaceInstruction>()).OrderBy(k => k.Key)
                .SequenceEqual((plan.DriverPaces ?? new Dictionary<string, PaceInstruction>()).OrderBy(k => k.Key)))
            throw new ArgumentException("A race's strategy plan cannot change between phases.");
    }

    public static void Validate(StrategyPlan source, IReadOnlyList<string> drivers, int laps)
    {
        if (source is null || source.Scheduled is null || source.Conditional is null || !Enum.IsDefined(source.DefaultPace))
            throw new ArgumentException("Invalid strategy plan.");
        if (source.DriverPaces?.Any(p => !drivers.Contains(p.Key) || !Enum.IsDefined(p.Value)) == true)
            throw new ArgumentException("Invalid per-driver default pace.");
        foreach (var entry in source.Scheduled)
        {
            if (entry is null || entry.Phase < 0 || entry.Phase >= (laps - 1) / 5 + 1
                || !drivers.Contains(entry.DriverId) || entry.Decision is null)
                throw new ArgumentException("Invalid scheduled decision.");
            entry.Decision.Validate();
        }
        if (source.Scheduled.GroupBy(s => (s.Phase, s.DriverId)).Any(g => g.Count() > 1))
            throw new ArgumentException("Duplicate scheduled decision.");
        foreach (var rule in source.Conditional)
        {
            if (rule is null || !drivers.Contains(rule.DriverId) || !Enum.IsDefined(rule.Field)
                || !Enum.IsDefined(rule.Comparison) || !double.IsFinite(rule.Threshold) || rule.Decision is null)
                throw new ArgumentException("Invalid conditional decision.");
            rule.Decision.Validate();
        }
    }

    internal record Evaluation(Dictionary<string, DriverDecision> Choices, List<DecisionRecord> Records,
        List<(string Driver, int Rule)> Fired);

    public Evaluation Evaluate(int phase, IReadOnlyList<TeamObservation> observations)
    {
        var choices = new Dictionary<string, DriverDecision>();
        var records = new List<DecisionRecord>();
        var fired = new List<(string Driver, int Rule)>();
        foreach (var observation in observations)
        foreach (var tyre in observation.TeamTyres)
        {
            var classification = observation.Classification.Single(c => c.DriverId == tyre.DriverId);
            if (classification.FinishTime is not null || classification.RetirementTime is not null) continue;
            var decision = new DriverDecision(plan.DriverPaces?.GetValueOrDefault(tyre.DriverId, plan.DefaultPace) ?? plan.DefaultPace);
            var source = "default";
            var scheduled = plan.Scheduled.SingleOrDefault(s => s.Phase == phase && s.DriverId == tyre.DriverId);
            if (scheduled is not null) { decision = scheduled.Decision; source = "scheduled"; }
            else
            {
                for (var index = 0; index < plan.Conditional.Length; index++)
                {
                    var rule = plan.Conditional[index];
                    if (rule.DriverId != tyre.DriverId || rule.Once && consumed.Contains((tyre.DriverId, index))
                        || rule.Decision.PitCompound is not null && (tyre.InPits || tyre.StopQueued)) continue;
                    var value = rule.Field switch
                    {
                        ConditionField.EstimatedTyreCondition => tyre.EstimatedCondition,
                        ConditionField.GapToLeader => classification.GapSeconds,
                        ConditionField.LeaderLap => observation.LeaderLap,
                        ConditionField.Position => classification.Position,
                        _ => throw new ArgumentException("Unknown rule field.")
                    };
                    if (!(rule.Comparison == ConditionComparison.LessThanOrEqual
                        ? value <= rule.Threshold : value >= rule.Threshold)) continue;
                    decision = rule.Decision;
                    source = $"conditional:{index}";
                    fired.Add((tyre.DriverId, index));
                    break;
                }
            }
            choices[tyre.DriverId] = decision;
            records.Add(new DecisionRecord(phase, observation.Time, tyre.DriverId, decision, tyre.EstimatedCondition, source));
        }
        return new Evaluation(choices, records, fired);
    }

    public void Accept(Evaluation evaluation)
    {
        foreach (var rule in evaluation.Fired) consumed.Add(rule);
    }
}
