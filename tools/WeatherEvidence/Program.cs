using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Application;
using Game.Domain;
using Game.Simulation;

var json=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase};json.Converters.Add(new JsonStringEnumConverter());
if(args.Length==2&&args[0]=="--replay")
{
    using var file=File.OpenRead(args[1]);using var gzip=new GZipStream(file,CompressionMode.Decompress);
    var saved=JsonSerializer.Deserialize<Archive>(gzip,json)!;
    if(saved.Schema!="weather-weekend-evidence-v1")throw new ArgumentException("Unknown evidence version.");
    foreach(var run in saved.Runs)
    {
        var qualifying=run.Attempts.Select(Qualifying.Resolve).ToArray();
        if(JsonSerializer.Serialize(qualifying,json)!=JsonSerializer.Serialize(run.Qualifying,json))throw new Exception("Qualifying replay differs.");
        var start=AutomaticRaceStart.Create(run.Opening);
        if(AutomaticRaceStart.OutcomeHash(start.Opening)!=run.OpeningHash)throw new Exception("Opening replay differs.");
        var state=start.Opening;
        foreach(var operation in run.Operations)
        {
            state=Advance(start.Engine,state,operation.Choices);
            if(AutomaticRaceStart.OutcomeHash(state)!=operation.Hash)throw new Exception("Operation replay differs.");
        }
        if(!state.Finished||JsonSerializer.Serialize(Compact(state),json)!=JsonSerializer.Serialize(run.Final,json))throw new Exception("Final replay differs.");
        if(JsonSerializer.Serialize(AssessForecasts(run.Opening.Configuration.Weather!,run.Seed),json)!=JsonSerializer.Serialize(run.Forecasts,json))throw new Exception("Forecast replay differs.");
    }
    Console.WriteLine($"Exact replay: {saved.Runs.Length} complete 22-car weekends without catalog access.");return;
}
if(args.Length!=1)throw new ArgumentException("Pass a new output directory or --replay frozen.json.gz.");
if(Directory.Exists(args[0])&&Directory.EnumerateFileSystemEntries(args[0]).Any())throw new ArgumentException("Output must be new or empty.");
Directory.CreateDirectory(args[0]);var watch=Stopwatch.StartNew();var runs=new List<Run>();
var roster=new[]{("lando-norris","oscar-piastri"),("charles-leclerc","lewis-hamilton"),("max-verstappen","isack-hadjar"),("george-russell","andrea-kimi-antonelli"),("fernando-alonso","lance-stroll"),("pierre-gasly","franco-colapinto"),("esteban-ocon","oliver-bearman"),("liam-lawson","arvid-lindblad"),("alexander-albon","carlos-sainz"),("nico-hulkenberg","gabriel-bortoleto"),("sergio-perez","valtteri-bottas")};
foreach(var circuit in new[]{"track-monza","track-monaco"})
foreach(var profile in new[]{"all-dry","steady-wet","rain-arrival","drying","changeable"})
{
    var weekends=roster.Select((pair,i)=>{
        var preset=WeekendPresetCatalog.Open(circuit,"driver-"+pair.Item1,"driver-"+pair.Item2);
        preset=preset with{Cars=preset.Cars.Select((c,j)=>c with{TeamId=$"team-{i+1:00}",EntryId=$"team-{i+1:00}-car-{j+1}"}).ToImmutableArray()};
        var weekend=WeekendPreparation.Prepare(preset,10,new(0,0,0));
        return weekend with{Configuration=weekend.Configuration.WithExplicitTuning() with{Weather=WeatherWeekendProfiles.Apply(weekend,profile),WetTyres=new(),WeatherReactions=new()}};
    }).ToArray();
    foreach(var seed in Enumerable.Range(101,8))
    foreach(var arm in new[]{"observed","fixed-Medium","fixed-Intermediate","fixed-Wet","delay-one-window","quality-low","quality-high"})
        runs.Add(Execute(circuit,profile,seed,arm,weekends));
    Console.WriteLine($"{circuit}/{profile}: {runs.Count} complete weekends; {watch.Elapsed.TotalSeconds:F1}s");
}
var archive=new Archive("weather-weekend-evidence-v1",WeatherDecisionPolicy.Version,WeatherWeekendProfiles.Version,runs.ToArray());
foreach(var group in runs.GroupBy(r=>(r.Circuit,r.Profile)))
{
    using var file=File.Create(Path.Combine(args[0],group.Key.Circuit+"-"+group.Key.Profile+".json.gz"));
    using var gzip=new GZipStream(file,CompressionLevel.SmallestSize);
    JsonSerializer.Serialize(gzip,archive with{Runs=group.ToArray()},json);
}
var groups=runs.GroupBy(r=>(r.Circuit,r.Profile,r.Arm)).Select(g=>new{g.Key.Circuit,g.Key.Profile,g.Key.Arm,weekends=g.Count(),meanClockSeconds=g.Average(r=>r.Final.Time),meanWeatherWindows=g.Average(r=>(double)r.Operations.Count(o=>o.Kind=="Weather")),distanceEligibilityGroups=g.SelectMany(r=>r.Final.Classification.Select(c=>new{c.Laps,eligible=r.Final.Eligibility[c.DriverId],retired=c.RetirementTime.HasValue})).GroupBy(x=>x).Select(x=>new{x.Key.Laps,x.Key.eligible,x.Key.retired,count=x.Count()}),targetPositions=g.Select(r=>r.Final.Classification.Where(c=>c.DriverId.StartsWith("team-01-",StringComparison.Ordinal)).Average(c=>c.Position)).ToArray()});
var pairs=runs.Where(r=>r.Arm!="observed").GroupBy(r=>(r.Circuit,r.Profile,r.Arm)).Select(g=>{
    var differences=g.Select(r=>{
        var baseline=runs.Single(b=>b.Circuit==r.Circuit&&b.Profile==r.Profile&&b.Seed==r.Seed&&b.Arm=="observed");
        double Position(Run x)=>x.Final.Classification.Where(c=>c.DriverId.StartsWith("team-01-",StringComparison.Ordinal)).Average(c=>c.Position);
        return new{r.Seed,positionDifference=Position(r)-Position(baseline),clockDifferenceSeconds=r.Final.Time-baseline.Final.Time,baselineGroup=TargetGroup(baseline),alternativeGroup=TargetGroup(r)};
    }).ToArray();var values=differences.Select(d=>d.positionDifference).ToArray();var mean=values.Average();var sd=Math.Sqrt(values.Sum(v=>(v-mean)*(v-mean))/(values.Length-1));
    return new{g.Key.Circuit,g.Key.Profile,g.Key.Arm,independentSeedPairs=values.Length,meanPositionDifference=mean,approximate95HalfWidth=2.365*sd/Math.Sqrt(values.Length),differences};
});
var forecasts=runs.Where(r=>r.Arm is "quality-low" or "quality-high").GroupBy(r=>(r.Circuit,r.Profile,r.Arm)).Select(g=>new{g.Key.Circuit,g.Key.Profile,g.Key.Arm,raceSeedSamples=g.Count(),correlatedForecasts=g.Sum(r=>r.Forecasts.Length),meanAbsoluteRainIntensityError=g.SelectMany(r=>r.Forecasts).Average(f=>Math.Abs(f.Probability-f.ActualMeanIntensity)),rangeCoverage=g.SelectMany(r=>r.Forecasts).Average(f=>f.ActualMeanIntensity>=f.Lower&&f.ActualMeanIntensity<=f.Upper?1d:0d),meanWidth=g.SelectMany(r=>r.Forecasts).Average(f=>f.Upper-f.Lower)});
File.WriteAllText(Path.Combine(args[0],"summary.json"),JsonSerializer.Serialize(new{archive.Schema,archive.OpponentPolicy,archive.ProfileVersion,runs=runs.Count,seededWeekendPairs=80,stochasticWeatherPaths=48,deterministicProfileGroups=4,elapsedWallSeconds=watch.Elapsed.TotalSeconds,groups,pairs,forecasts},json));
Console.WriteLine($"Saved {runs.Count} complete weekends, {watch.Elapsed.TotalSeconds:F1}s wall clock; human time unmeasured.");

Run Execute(string circuit,string profile,int seed,string arm,EffectiveWeekend[] baseWeekends)
{
    var quality=arm=="quality-low"?0:arm=="quality-high"?1:.5;
    var weekends=baseWeekends.Select(w=>w with{Configuration=w.Configuration with{Weather=w.Configuration.Weather! with{InformationQuality=quality}}}).ToArray();
    var attempts=new List<QualifyingAttemptInput>();var qualifying=new List<QualifyingResult>();var order=weekends.SelectMany(w=>w.Cars).Select(c=>c.Source.EntryId).ToArray();
    foreach(var (session,retain) in new[]{("Q1",16),("Q2",10),("Q3",0)})
    {
        var input=new QualifyingAttemptInput(session,1,seed,weekends,order.Select(id=>new QualifyingChoice(id,"Push")).ToArray(),order,new(retain),Qualifying.WetModelVersion,Conditions:Qualifying.SessionConditions(weekends[0].Configuration,seed,session));
        var result=Qualifying.Resolve(input);attempts.Add(input);qualifying.Add(result);order=result.Ranking.Where(r=>r.Advances).Select(r=>r.EntryId).ToArray();
    }
    var configuration=weekends[0].Configuration with{Weather=weekends[0].Configuration.Weather! with{TimeOriginSeconds=540}};
    var grid=Qualifying.Grid(qualifying).Select(g=>new StartGridSlot(g.Position,g.EntryId)).ToArray();
    var initial=WeatherDecisionPolicy.StartingCompound(CircuitWeather.Observe(configuration.Weather!,seed,540));
    var compounds=grid.ToDictionary(g=>g.EntryId,g=>g.EntryId.StartsWith("team-01-",StringComparison.Ordinal)&&arm.StartsWith("fixed-",StringComparison.Ordinal)?Enum.Parse<TyreCompound>(arm[6..]):initial);
    var opening=new AutomaticStartInput(configuration,weekends,grid,seed,new(),AutomaticRaceStart.WetModelVersion,StartingCompounds:compounds);
    var start=AutomaticRaceStart.Create(opening);var state=start.Opening;var operations=new List<Operation>();var delayed=new HashSet<string>();
    for(var count=0;!state.Finished&&count<100;count++)
    {
        var safety=state.WeatherReaction?.SafetyCarActive==true||state.Interruption is not null;
        var choices=weekends.SelectMany(w=>WeatherDecisionPolicy.Evaluate(new(count,configuration.Laps,start.Engine.GetTeamObservation(w.Cars[0].Source.TeamId),start.Engine.GetTimingObservation(),true,safety)).Cars).ToDictionary(c=>c.DriverId,c=>c.Decision);
        foreach(var id in choices.Keys.Where(id=>id.StartsWith("team-01-",StringComparison.Ordinal)).ToArray())
        {
            if(arm.StartsWith("fixed-",StringComparison.Ordinal))choices[id]=choices[id] with{PitCompound=null};
            if(arm=="delay-one-window"&&choices[id].PitCompound is not null&&delayed.Add(id))choices[id]=choices[id] with{PitCompound=null};
        }
        var kind=state.WeatherReaction is not null?"Weather":state.Interruption is not null?"SafetyCar":"Phase";
        state=Advance(start.Engine,state,choices);operations.Add(new(kind,choices,AutomaticRaceStart.OutcomeHash(state)));
    }
    if(!state.Finished||state.Classification.Count!=22)throw new Exception("Incomplete bounded weekend.");
    return new(circuit,profile,seed,arm,attempts.ToArray(),qualifying.ToArray(),opening,AutomaticRaceStart.OutcomeHash(start.Opening),SimulationReplay.Metadata(configuration),operations.ToArray(),Compact(state),AssessForecasts(configuration.Weather!,seed));
}
PhaseResult Compact(PhaseResult state)=>state with{Events=state.Events.Where(e=>e.Kind is RaceEventKind.LapCompleted or RaceEventKind.PitEntry or RaceEventKind.TyreChanged).ToArray(),Decisions=[]};
PhaseResult Advance(RaceEngine engine,PhaseResult state,Dictionary<string,DriverDecision> choices)=>state.WeatherReaction is not null?engine.ResolveWeatherReaction(choices):state.Interruption is not null?engine.ResolveSafetyCar(choices.ToDictionary(p=>p.Key,p=>new SafetyCarDecision(p.Value.PitCompound))):engine.ResolvePhase(choices);
ForecastSample[] AssessForecasts(WeatherTuning tuning,int seed)=>Enumerable.Range(0,9).SelectMany(i=>{
    var time=i*120d;var observed=CircuitWeather.Observe(tuning,seed,time);var forecast=CircuitWeather.Forecast(observed,"team-01",tuning.InformationQuality,tuning.ForecastRefreshSeconds);
    return forecast.Horizons.Select(h=>new ForecastSample(observed,forecast,h.ToSeconds-h.FromSeconds,h.RainProbability,h.RainLower,h.RainUpper,Enumerable.Range(0,(int)((h.ToSeconds-h.FromSeconds)/10)).Average(j=>CircuitWeather.Observe(tuning,seed,h.FromSeconds+j*10+5).RainIntensity)));
}).ToArray();
object TargetGroup(Run run)=>run.Final.Classification.Where(c=>c.DriverId.StartsWith("team-01-",StringComparison.Ordinal)).Select(c=>new{c.DriverId,c.Laps,eligible=run.Final.Eligibility[c.DriverId],c.RetirementTime}).ToArray();
record ForecastSample(CurrentWeather Observed,TeamWeatherForecast Forecast,double HorizonSeconds,double Probability,double Lower,double Upper,double ActualMeanIntensity);
record Operation(string Kind,Dictionary<string,DriverDecision> Choices,string Hash);
record Run(string Circuit,string Profile,int Seed,string Arm,QualifyingAttemptInput[] Attempts,QualifyingResult[] Qualifying,AutomaticStartInput Opening,string OpeningHash,ReplayMetadata Versions,Operation[] Operations,PhaseResult Final,ForecastSample[] Forecasts);
record Archive(string Schema,string OpponentPolicy,string ProfileVersion,Run[] Runs);
