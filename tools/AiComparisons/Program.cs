using Game.Application;
using Game.Domain;
using Game.Simulation;
using System.Diagnostics;
using System.Text.Json;

var output=args.Length>0?args[0]:".tools/reports/ai-comparisons.json";
var seeds=Enumerable.Range(7,20).ToArray();
var samples=new List<Sample>();
foreach(var laps in new[]{6,40})
foreach(var presetName in new[]{"open-equal","restricted-equal","open-mixed","restricted-mixed"})
foreach(var strategy in new[]{"balanced-one-stop","early-two-stop","conserve-one-stop"})
foreach(var seed in seeds)
{
    var preset=new DryRacePresets().Get(presetName,laps);
    // Rotate the comparison driver across grid positions to avoid a starting-order claim of strength.
    var target=preset.Drivers[(seed-7)%preset.Drivers.Count].Id;
    var race=new RaceEngine(preset.Configuration,preset.Drivers,seed);
    var policyMilliseconds=0d;var resolutionMilliseconds=0d;var phases=0;PhaseResult result;
    do
    {
        var choices=new Dictionary<string,DriverDecision>();
        var started=Stopwatch.GetTimestamp();
        var timing=race.GetTimingObservation();
        foreach(var team in preset.Drivers.Select(d=>d.Team).Distinct())
        {
            var observation=race.GetTeamObservation(team);
            var plan=ObservationDecisionPolicy.Evaluate(new(phases,laps,observation,timing));
            foreach(var car in plan.Cars)choices.Add(car.DriverId,car.Decision);
            foreach(var tyre in observation.TeamTyres.Where(t=>t.DriverId==target))
            {
                var pace=strategy=="conserve-one-stop"?PaceInstruction.Conserve:PaceInstruction.Standard;
                TyreCompound? pit=null;
                if(!tyre.InPits&&!tyre.StopQueued)
                {
                    var row=observation.Classification.Single(c=>c.DriverId==target);
                    var stopAt=strategy=="early-two-stop"?laps/3:laps/2;
                    if(tyre.Compound==TyreCompound.Medium&&(row.Laps>=stopAt||laps-observation.LeaderLap<=10))pit=TyreCompound.Hard;
                    else if(strategy=="early-two-stop"&&tyre.Compound==TyreCompound.Hard&&row.Laps>=2*laps/3&&laps-row.Laps>5)pit=TyreCompound.Soft;
                }
                choices[target]=new(pace,pit);
            }
        }
        policyMilliseconds+=Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        started=Stopwatch.GetTimestamp();result=race.ResolvePhase(choices);
        resolutionMilliseconds+=Stopwatch.GetElapsedTime(started).TotalMilliseconds;phases++;
    }while(!result.Finished);
    var targetRow=result.Classification.Single(c=>c.DriverId==target);
    samples.Add(new(presetName,laps,strategy,seed,target,targetRow.Position,targetRow.FinishTime!.Value,result.Eligibility[target],
        result.Eligibility.Where(p=>p.Key!=target).Count(p=>p.Value),result.Events.Count(e=>e.DriverId==target&&e.Kind==RaceEventKind.PitEntry),policyMilliseconds,resolutionMilliseconds,phases));
}
var policyHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes("src/backend/Game.Application/ObservationDecisionPolicy.cs")));
var runnerHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes("tools/AiComparisons/Program.cs")));
var summaries=samples.GroupBy(s=>new{s.Preset,s.Laps,s.Strategy}).Select(g=>
{
    var ranks=g.Select(s=>(double)s.Position).ToArray();var mean=ranks.Average();var sd=Math.Sqrt(ranks.Sum(x=>Math.Pow(x-mean,2))/(ranks.Length-1));
    return new{g.Key.Preset,g.Key.Laps,g.Key.Strategy,Count=g.Count(),MeanRepresentativePosition=mean,PositionSampleSd=sd,
        PositionMean95Interval=new[]{mean-2.093*sd/Math.Sqrt(ranks.Length),mean+2.093*sd/Math.Sqrt(ranks.Length)},
        RepresentativeEligible=g.Count(s=>s.Eligible),AiEligible=g.Sum(s=>s.AiEligible),AiEntries=g.Count()*21,
        MeanPolicyMilliseconds=g.Average(s=>s.PolicyMilliseconds),MeanResolutionMilliseconds=g.Average(s=>s.ResolutionMilliseconds)};
}).ToArray();
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
File.WriteAllText(output,JsonSerializer.Serialize(new{Policy=ObservationDecisionPolicy.Version,PolicySha256=policyHash,RunnerSha256=runnerHash,Seeds=seeds,
    Scope="serial local public engine runs; comparison driver rotates positions; intervals descriptive, not paired causal guarantees",Summaries=summaries,Samples=samples},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"Saved {samples.Count} races to {output}");
record Sample(string Preset,int Laps,string Strategy,int Seed,string Driver,int Position,double FinishSeconds,bool Eligible,int AiEligible,int Stops,double PolicyMilliseconds,double ResolutionMilliseconds,int Phases);


