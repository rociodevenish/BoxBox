using System.Collections.Immutable;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Domain;
using Game.Simulation;

var json=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
json.Converters.Add(new JsonStringEnumConverter());
if(args.Length==2&&args[0]=="--replay")
{
    using var stream=File.OpenRead(args[1]);
    using var compressed=new GZipStream(stream,CompressionMode.Decompress);
    var saved=JsonSerializer.Deserialize<Evidence>(compressed,json)!;
    foreach(var run in saved.Runs)
    {
        var actual=Run(run.Attempts.Select(a=>a.Input).ToArray(),run.OpeningInput,run.Plan);
        if(JsonSerializer.Serialize(actual,json)!=JsonSerializer.Serialize(run,json))throw new InvalidOperationException("Saved-file replay differs.");
    }
    Console.WriteLine($"Replayed {saved.Runs.Length} full-field Q1/Q2/Q3, opening and first Race Phase outcomes without catalog access.");
    return;
}
if(args.Length!=1&&!(args.Length==2&&args[0]=="--controls"))throw new ArgumentException("Pass output directory, or --replay saved.json.");
Directory.CreateDirectory(args.Length==1?args[0]:args[1]);
var roster=new[]{("lando-norris","oscar-piastri"),("charles-leclerc","lewis-hamilton"),("max-verstappen","isack-hadjar"),("george-russell","andrea-kimi-antonelli"),("fernando-alonso","lance-stroll"),("pierre-gasly","franco-colapinto"),("esteban-ocon","oliver-bearman"),("liam-lawson","arvid-lindblad"),("alexander-albon","carlos-sainz"),("nico-hulkenberg","gabriel-bortoleto"),("sergio-perez","valtteri-bottas")};
if(args.Length==2&&args[0]=="--controls")
{
    var controls=new List<object>();
    foreach(var circuit in new[]{"track-monza","track-monaco"})
    foreach(var aero in new[]{-2,2})
    {
        var weekends=roster.Select((pair,i)=>
        {
            var preset=WeekendPresetCatalog.Open(circuit,"driver-"+pair.Item1,"driver-"+pair.Item2);
            preset=preset with{Cars=preset.Cars.Select((c,j)=>c with{TeamId=$"team-{i+1:00}",EntryId=$"team-{i+1:00}-car-{j+1}"}).ToImmutableArray()};
            return WeekendPreparation.Prepare(preset,10,new(i==0?aero:0,0,0));
        }).ToArray();
        var runs=new List<RunRecord>();
        foreach(var seed in Enumerable.Range(101,4))
        {
            var order=weekends.SelectMany(w=>w.Cars).Select(c=>c.Source.EntryId).ToArray();
            var attempts=new List<QualifyingAttemptInput>();
            foreach(var (session,retain) in new[]{("Q1",16),("Q2",10),("Q3",0)})
            {
                var input=new QualifyingAttemptInput(session,1,seed,weekends,order.Select(id=>new QualifyingChoice(id,"Push")).ToArray(),order,new(retain));
                attempts.Add(input);order=Qualifying.Resolve(input).Ranking.Where(r=>r.Advances).Select(r=>r.EntryId).ToArray();
            }
            var grid=Qualifying.Grid(attempts.Select(Qualifying.Resolve).ToArray());
            runs.Add(Run(attempts.ToArray(),new(weekends[0].Configuration.WithExplicitTuning(),weekends,grid.Select(g=>new StartGridSlot(g.Position,g.EntryId)).ToArray(),seed,new()),new([],[])));
        }
        var filename=$"control-{circuit}-aero-{aero}.json.gz";
        using(var stream=File.Create(Path.Combine(args[1],filename)))
        using(var compressed=new GZipStream(stream,CompressionLevel.SmallestSize))
            JsonSerializer.Serialize(compressed,new Evidence("qualifying-evidence-v1",circuit,"mixed-target-aero-"+aero,Enumerable.Range(101,4).ToArray(),runs.ToArray()),json);
        controls.Add(new{filename,circuit,targetAero=aero,q1=runs.SelectMany(r=>r.Attempts[0].Result.Ranking.Where(t=>t.TeamId=="team-01").Select(t=>new{seed=r.OpeningInput.Seed,t.EntryId,t.Status,t.TimeMilliseconds})).ToArray()});
    }
    File.WriteAllText(Path.Combine(args[1],"controls.json"),JsonSerializer.Serialize(controls,json));
    Console.WriteLine("Generated 16 full-field setup controls.");return;
}
var seeds=Enumerable.Range(101,32).ToArray();
var summaries=new List<object>();
foreach(var circuit in new[]{"track-monza","track-monaco"})
foreach(var equal in new[]{true,false})
{
    var opened=roster.Select(p=>WeekendPresetCatalog.Open(circuit,"driver-"+p.Item1,"driver-"+p.Item2)).ToArray();
    var weekends=opened.Select((preset,i)=>WeekendPreparation.Prepare(preset with{Cars=preset.Cars.Select((car,j)=>car with{
        TeamId=$"team-{i+1:00}",EntryId=$"team-{i+1:00}-car-{j+1}",
        Driver=equal?car.Driver with{Ratings=opened[0].Cars[0].Driver.Ratings,Preferences=opened[0].Cars[0].Driver.Preferences}:car.Driver
    }).ToImmutableArray()},10,new(0,0,0))).ToArray();
    var runs=new List<RunRecord>();
    foreach(var seed in seeds)
    foreach(var approach in new[]{"Safe","Push","All-Out"})
    {
        var order=weekends.SelectMany(w=>w.Cars).Select(c=>c.Source.EntryId).ToArray();
        var attempts=new List<QualifyingAttemptInput>();
        foreach(var (session,retain) in new[]{("Q1",16),("Q2",10),("Q3",0)})
        {
            var input=new QualifyingAttemptInput(session,1,seed,weekends,order.Select(id=>new QualifyingChoice(id,id.StartsWith("team-01-",StringComparison.Ordinal)?approach:"Push")).ToArray(),order,new(retain));
            attempts.Add(input);
            order=Qualifying.Resolve(input).Ranking.Where(r=>r.Advances).Select(r=>r.EntryId).ToArray();
        }
        var grid=Qualifying.Grid(attempts.Select(Qualifying.Resolve).ToArray());
        var opening=new AutomaticStartInput(weekends[0].Configuration.WithExplicitTuning(),weekends,grid.Select(r=>new StartGridSlot(r.Position,r.EntryId)).ToArray(),seed,new());
        var plan=new StrategyPlan([],[],DriverPaces:weekends.SelectMany(w=>w.Cars).ToDictionary(c=>c.Source.EntryId,_=>PaceInstruction.Standard));
        runs.Add(Run(attempts.ToArray(),opening,plan));
    }
    var file=$"{circuit}-{(equal?"equal":"mixed")}.json.gz";
    using(var stream=File.Create(Path.Combine(args[0],file)))
    using(var compressed=new GZipStream(stream,CompressionLevel.SmallestSize))
        JsonSerializer.Serialize(compressed,new Evidence("qualifying-evidence-v1",circuit,equal?"equal":"mixed",seeds,runs.ToArray()),json);
    // Q1 includes both identical target entries for every seed; later stages are selection-conditioned.
    var stats=new List<object>();
    foreach(var approach in new[]{"Safe","Push","All-Out"})
    {
        var target=runs.Where(r=>r.Attempts[0].Input.Choices[0].Approach==approach).SelectMany(r=>r.Attempts[0].Result.Ranking.Where(t=>t.TeamId=="team-01")).ToArray();
        var times=target.Where(t=>t.TimeMilliseconds.HasValue).Select(t=>(double)t.TimeMilliseconds!.Value).ToArray();
        var mean=times.Average();
        stats.Add(new{approach,attempts=target.Length,valid=times.Length,noTime=target.Length-times.Length,conditionalMeanMilliseconds=mean,conditionalMinMilliseconds=times.Min(),conditionalMaxMilliseconds=times.Max(),conditionalPopulationSdMilliseconds=Math.Sqrt(times.Average(t=>(t-mean)*(t-mean)))});
    }
    var pairs=new List<object>();
    foreach(var alternative in new[]{"Push","All-Out"})
    {
        var differences=new List<int>();var missing=0;
        foreach(var seed in seeds)
        foreach(var id in new[]{"team-01-car-1","team-01-car-2"})
        {
            QualifyingRank Target(string approach)=>runs.Single(r=>r.OpeningInput.Seed==seed&&r.Attempts[0].Input.Choices[0].Approach==approach).Attempts[0].Result.Ranking.Single(r=>r.EntryId==id);
            var safe=Target("Safe");var other=Target(alternative);
            if(safe.TimeMilliseconds is {} a&&other.TimeMilliseconds is {} b)differences.Add(b-a);else missing++;
        }
        pairs.Add(new{alternative,baseline="Safe",bothValid=differences.Count,excludedNoTime=missing,faster=differences.Count(d=>d<0),equal=differences.Count(d=>d==0),slower=differences.Count(d=>d>0),meanMilliseconds=differences.Average()});
    }
    summaries.Add(new{file,circuit,field=equal?"equal":"mixed",q1TargetStatistics=stats,pairedQ1Signs=pairs});
}
File.WriteAllText(Path.Combine(args[0],"summary.json"),JsonSerializer.Serialize(summaries,json));
Console.WriteLine("Generated four frozen files: 384 complete weekends and 8,448 Q1 attempts; summary uses 64 target Q1 attempts per approach/field/circuit.");

RunRecord Run(QualifyingAttemptInput[] inputs,AutomaticStartInput opening,StrategyPlan plan)
{
    var attempts=inputs.Select(i=>new Attempt(i,Qualifying.Resolve(i))).ToArray();
    var grid=Qualifying.Grid(attempts.Select(a=>a.Result).ToArray());
    if(grid.Length!=22||grid.Select(g=>g.EntryId).Distinct().Count()!=22||!grid.Select(g=>g.Position).SequenceEqual(Enumerable.Range(1,22)))throw new InvalidOperationException("Incomplete grid.");
    if(!grid.Select(g=>new StartGridSlot(g.Position,g.EntryId)).SequenceEqual(opening.Grid))throw new InvalidOperationException("Opening grid differs.");
    var start=AutomaticRaceStart.Create(opening);
    var first=start.Engine.ResolvePhase(plan);
    if(start.Opening.LeaderLap!=1||first.LeaderLap!=6||first.Classification.Count!=22)throw new InvalidOperationException("Opening handoff differs.");
    return new(attempts,grid,opening,SimulationReplay.Metadata(opening.Configuration),start.Opening,plan,first);
}
record Attempt(QualifyingAttemptInput Input,QualifyingResult Result);
record RunRecord(Attempt[] Attempts,QualifyingRank[] Grid,AutomaticStartInput OpeningInput,ReplayMetadata RaceMetadata,PhaseResult Opening,StrategyPlan Plan,PhaseResult FirstRacePhase);
record Evidence(string Schema,string Circuit,string Field,int[] Seeds,RunRecord[] Runs);
