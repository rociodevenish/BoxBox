using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Game.Domain;
namespace Game.Simulation;

/// <summary>One abstract timed attempt per eligible car, using frozen effective inputs only.</summary>
public static class Qualifying
{
    public const string ModelVersion="qualifying-attempt-v1";
    public const string RandomnessVersion="qualifying-keyed-v1";
    public const string RulesVersion="qualifying-format-v1";
    public static QualifyingResult Resolve(QualifyingAttemptInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if(input.ModelVersion!=ModelVersion||input.RandomnessVersion!=RandomnessVersion||input.Rules?.Version!=RulesVersion
            ||input.Session is not ("Q1" or "Q2" or "Q3")||input.Attempt!=1||input.Weekends is null||input.Choices is null||input.PreSessionOrder is null)
            throw new ArgumentException("Unsupported qualifying attempt, rules or replay version.");
        var cars=new Dictionary<string,(EffectiveWeekend Weekend,EffectiveWeekendCar Car)>();
        foreach(var weekend in input.Weekends)
        {
            _=WeekendPreparation.CreateRace(weekend,input.Seed);
            // Validate concrete saved detailed inputs as well as the effective model version.
            _=WeekendPreparation.Prepare(weekend.Source with{Cars=weekend.Cars.Select(c=>c.Source).ToImmutableArray()},weekend.Configuration.Laps,weekend.Setup);
            foreach(var car in weekend.Cars)
                if(!cars.TryAdd(car.Source.EntryId,(weekend,car)))throw new ArgumentException("Duplicate entry identity.");
        }
        if(cars.Values.Select(c=>c.Car.Source.Driver.Id).Distinct(StringComparer.Ordinal).Count()!=cars.Count||input.Choices.Any(c=>c is null))
            throw new ArgumentException("Unique driver identities and concrete choices are required.");
        var order=input.PreSessionOrder;
        if(order.Count==0||order.Distinct(StringComparer.Ordinal).Count()!=order.Count||order.Any(id=>!cars.ContainsKey(id))
            ||input.Choices.Count!=order.Count||input.Choices.Select(c=>c.EntryId).Distinct(StringComparer.Ordinal).Count()!=order.Count
            ||input.Choices.Any(c=>!order.Contains(c.EntryId)||c.Approach is not ("Safe" or "Push" or "All-Out")))
            throw new ArgumentException("Supply exactly one legal choice for every eligible entry.");
        if(cars.Values.Select(c=>c.Weekend.Source.Circuit.Id).Distinct().Count()!=1)
            throw new ArgumentException("All attempts must use the same circuit.");
        var times=input.Choices.Select(choice=>
        {
            var (weekend,car)=cars[choice.EntryId];
            // Engineer Setup and Setup Feedback supply information, never extra qualifying pace.
            var pace=(car.Source.Driver.Ratings["qualifyingPace"]-82)/10;
            var fit=car.RaceDriver.Speed+car.RaceDriver.Cornering;
            var compatibility=weekend.DriverFits.FirstOrDefault(f=>f.DriverId==car.Source.Driver.Id)?.Compatibility??100;
            var (gain,spread,exposure)=choice.Approach switch{"Safe"=>(-.3,.25,.015),"Push"=>(.1,.65,.055),_=>(.35,1.15,.14)};
            var mistakeChance=exposure*(1+(82-car.Source.Driver.Ratings["errorControl"])/100);
            var noTime=Unit(input,car.Source.Driver.Id,"error")<mistakeChance;
            var seconds=weekend.Configuration.Track.BaseLapTime-pace-fit+(100-compatibility)*.002-gain
                +(Unit(input,car.Source.Driver.Id,"pace")*2-1)*spread;
            return new QualifyingTime(choice.EntryId,car.Source.Driver.Id,car.Source.TeamId,noTime?"no-time":"valid",
                noTime?null:(int)Math.Max(1,Math.Round(seconds*1000,MidpointRounding.AwayFromZero)));
        }).ToArray();
        return new(input.Session,input.Attempt,ModelVersion,RandomnessVersion,RulesVersion,Rank(times,order,input.Rules));
    }
    /// <summary>Valid before no-time; equal times and no-times retain the fixed pre-session order.</summary>
    public static IReadOnlyList<QualifyingRank> Rank(IReadOnlyList<QualifyingTime> times,IReadOnlyList<string> preSessionOrder,QualifyingRules rules)
    {
        if(times is null||preSessionOrder is null||rules is null||rules.Version!=RulesVersion||rules.Retain<0||rules.Retain>times.Count
            ||times.Count==0||preSessionOrder.Count!=times.Count||preSessionOrder.Distinct().Count()!=times.Count
            ||times.Select(t=>t.EntryId).Distinct().Count()!=times.Count||times.Any(t=>!preSessionOrder.Contains(t.EntryId)
                ||t.Status is not ("valid" or "no-time")||(t.Status=="valid"?t.TimeMilliseconds is not >0:t.TimeMilliseconds is not null)))
            throw new ArgumentException("Complete explicit valid/no-time results and stable ordering are required.");
        var order=preSessionOrder.Select((id,i)=>(id,i)).ToDictionary(p=>p.id,p=>p.i);
        return times.OrderBy(t=>t.Status=="valid"?0:1).ThenBy(t=>t.TimeMilliseconds).ThenBy(t=>order[t.EntryId])
            .Select((t,i)=>new QualifyingRank(i+1,t.EntryId,t.DriverId,t.TeamId,t.Status,t.TimeMilliseconds,i<rules.Retain)).ToArray();
    }
    /// <summary>The three game-format groups are fixed by their own session, including no-times.</summary>
    public static QualifyingRank[] Grid(IReadOnlyList<QualifyingResult> results)
    {
        if(results is null||results.Count!=3||!results.Select(r=>r.Session).SequenceEqual(new[]{"Q1","Q2","Q3"})
            ||results[0].Ranking.Count!=22||results[1].Ranking.Count!=16||results[2].Ranking.Count!=10
            ||results.Any(r=>r.ModelVersion!=ModelVersion||r.RandomnessVersion!=RandomnessVersion||r.RulesVersion!=RulesVersion)
            ||!results[0].Ranking.Take(16).Select(r=>r.EntryId).Order().SequenceEqual(results[1].Ranking.Select(r=>r.EntryId).Order())
            ||!results[1].Ranking.Take(10).Select(r=>r.EntryId).Order().SequenceEqual(results[2].Ranking.Select(r=>r.EntryId).Order()))
            throw new ArgumentException("Supply complete consecutive Q1/Q2/Q3 results with unchanged eligible identities.");
        var grid=results[2].Ranking.Concat(results[1].Ranking.Skip(10)).Concat(results[0].Ranking.Skip(16))
            .Select((r,i)=>r with{Position=i+1,Advances=false}).ToArray();
        if(grid.Select(r=>r.EntryId).Distinct().Count()!=22||grid.Select(r=>r.DriverId).Distinct().Count()!=22)
            throw new ArgumentException("Every entry and driver must occupy exactly one grid slot.");
        return grid;
    }    private static double Unit(QualifyingAttemptInput input,string driver,string eventId)
    {
        var identity=System.Text.Json.JsonSerializer.Serialize(new object[]{RandomnessVersion,input.Seed,driver,input.Session,input.Attempt,eventId});
        return BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))/(double)uint.MaxValue;
    }
}
