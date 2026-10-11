using Game.Domain;
namespace Game.Application;

public record ObservationDecisionInput(int Phase, int TotalLaps, TeamObservation Observation,
    IReadOnlyList<RaceTimingEntry> Timing);
public record CarDecisionAdvice(string DriverId, DriverDecision Decision, IReadOnlyList<string> Reasons,
    string Uncertainty, IReadOnlyList<PaceInstruction> PaceOptions, IReadOnlyList<TyreCompound?> PitOptions,
    IReadOnlyList<BattleInstruction> BattleOptions, bool Confident);
public record ObservationDecisionPlan(int Phase, double ObservationTime, string PolicyVersion,
    IReadOnlyList<CarDecisionAdvice> Cars);

/// <summary>Bounded choices from legitimate observations only; no engine or randomness access.</summary>
public static class ObservationDecisionPolicy
{
    public const string Version="dry-observation-ai-v2";
    public static ObservationDecisionPlan Evaluate(ObservationDecisionInput input)
    {
        var cars=new List<CarDecisionAdvice>();
        foreach(var tyre in input.Observation.TeamTyres.OrderBy(t=>t.DriverId,StringComparer.Ordinal))
        {
            var row=input.Observation.Classification.Single(c=>c.DriverId==tyre.DriverId);
            var remaining=Math.Max(0,input.TotalLaps-row.Laps);
            var leaderRemaining=Math.Max(0,input.TotalLaps-input.Observation.LeaderLap);
            var reasons=new List<string>();
            var used=tyre.UsedCompounds??[tyre.Compound];
            var needsDifferent=used.Distinct().Count()<2;
            var stopUnavailable=tyre.InPits||tyre.StopQueued;
            TyreCompound? pit=null;
            var gap=input.Timing.FirstOrDefault(t=>t.DriverId==tyre.DriverId)?.ToAhead;
            var inTraffic=gap?.Seconds is >=0 and <=3 && gap.Laps==0 || gap?.Seconds is null && row.Position>1;
            var pace=tyre.EstimatedCondition<.45?PaceInstruction.Conserve:
                remaining<=5 && tyre.EstimatedCondition>.55?PaceInstruction.Push:PaceInstruction.Standard;
            if(stopUnavailable)reasons.Add(tyre.StopQueued?"The accepted stop is queued for the next lap end.":"The current stop is still in progress.");
            else if(row.FinishTime is null && remaining>0 && (needsDifferent && (row.Laps>=input.TotalLaps/2 || leaderRemaining<=10)
                || tyre.EstimatedCondition<.38 && remaining>3))
            {
                pit=Enum.GetValues<TyreCompound>().Where(c=>c!=tyre.Compound)
                    .OrderBy(c=>needsDifferent && used.Contains(c)?1:0)
                    .ThenBy(c=>remaining<=8?(c==TyreCompound.Soft?0:1):(c==TyreCompound.Hard?0:1)).ThenBy(c=>c).First();
                reasons.Add(needsDifferent?"Use a different dry compound before the finish to satisfy the race rule.":"Estimated tyre condition is low enough to consider fresh tyres.");
                pace=PaceInstruction.Standard;
            }
            else reasons.Add(needsDifferent?"A different dry compound is still needed before finishing.":"The two-compound requirement has been met.");
            if(pace==PaceInstruction.Conserve)reasons.Add("The tyre estimate is low; conserve to limit wear.");
            if(pace==PaceInstruction.Push)reasons.Add("There is little distance left and the tyre estimate supports pushing.");
            var battles=input.Observation.BattleOptions[tyre.DriverId];
            var battle=pit is null && !stopUnavailable && tyre.EstimatedCondition>.55 && inTraffic && battles.Contains(BattleInstruction.Attack)
                ?BattleInstruction.Attack:pit is null && !stopUnavailable && battles.Contains(BattleInstruction.Defend) && !battles.Contains(BattleInstruction.Attack)
                ?BattleInstruction.Defend:BattleInstruction.Neutral;
            if(battle!=BattleInstruction.Neutral)reasons.Add(battle==BattleInstruction.Attack?"The legal nearby battle supports an attack; timing gaps may be unavailable.":"A legal nearby battle supports defending.");
            if(inTraffic && pit is null && pace==PaceInstruction.Push){pace=PaceInstruction.Standard;reasons.Add("Close traffic may limit the benefit of pushing.");}
            var pits=stopUnavailable?new TyreCompound?[]{null}:new TyreCompound?[]{null,TyreCompound.Soft,TyreCompound.Medium,TyreCompound.Hard};
            cars.Add(new(tyre.DriverId,new(pace,pit,battle),reasons.AsReadOnly(),"Tyre condition is an estimate; traffic and stop outcomes remain uncertain.",
                Array.AsReadOnly(Enum.GetValues<PaceInstruction>()),Array.AsReadOnly(pits),Array.AsReadOnly(battles.ToArray()),false));
        }
        return new(input.Phase,input.Observation.Time,Version,cars.AsReadOnly());
    }
}


