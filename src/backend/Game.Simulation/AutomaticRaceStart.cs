using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Domain;
namespace Game.Simulation;

public sealed record AutomaticStartResult(RaceEngine Engine,PhaseResult Opening,PhaseResult GridState);

/// <summary>Creates one standing start; returns the same live engine at the common leader-lap-one crossing.</summary>
public static class AutomaticRaceStart
{
    public const string ModelVersion="automatic-standing-start-v2";
    public const string LegacyModelVersion="automatic-standing-start-v1";
    public const string RandomnessVersion="opening-keyed-v1";
    public static string OutcomeHash(PhaseResult result)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(result)));
    public static AutomaticStartResult Replay(AutomaticStartReplay replay)
    {
        if(replay is null||string.IsNullOrWhiteSpace(replay.PreparationContextId))throw new ArgumentException("Opening replay requires frozen preparation identity.");
        var result=Create(replay.Input);
        if(OutcomeHash(result.Opening)!=replay.OutcomeHash)throw new ArgumentException("Opening replay outcome does not match its recorded hash.");
        return result;
    }
    public static AutomaticStartResult Create(AutomaticStartInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if((input.ModelVersion!=ModelVersion&&input.ModelVersion!=LegacyModelVersion)||input.RandomnessVersion!=RandomnessVersion||input.Configuration is null
            ||input.Weekends is null||input.Grid is null||input.Tuning is null||input.Weekends.Count==0)
            throw new ArgumentException("Explicit compatible opening inputs are required.");
        var tuning=input.Tuning;
        if(new[]{tuning.GridSpacingMetres,tuning.BaseLaunchLossSeconds,tuning.AbilitySeconds,tuning.VariationSeconds,tuning.ErrorSeconds}.Any(v=>!double.IsFinite(v)||v<0||v>100)
            ||!double.IsFinite(tuning.AggregateLengthMetres)||tuning.AggregateLengthMetres is <100 or >20000)
            throw new ArgumentException("Opening tuning must be finite and bounded.");
        var field=new Dictionary<string,(EffectiveWeekend Weekend,EffectiveWeekendCar Car)>();
        foreach(var weekend in input.Weekends)
        {
            _=WeekendPreparation.CreateRace(weekend,input.Seed);
            var rebuilt=WeekendPreparation.Prepare(weekend.Source,weekend.Configuration.Laps,weekend.Setup);
            if(JsonSerializer.Serialize(rebuilt.Cars)!=JsonSerializer.Serialize(weekend.Cars)||weekend.Configuration.Laps!=input.Configuration.Laps
                ||JsonSerializer.Serialize(weekend.Configuration.Track)!=JsonSerializer.Serialize(input.Configuration.Track))
                throw new ArgumentException("Frozen effective inputs and circuit must match their source.");
            foreach(var car in weekend.Cars)
                if(car.RaceDriver.Id!=car.Source.EntryId||car.RaceDriver.Team!=car.Source.TeamId||!field.TryAdd(car.Source.EntryId,(weekend,car)))
                    throw new ArgumentException("Opening entry identities must be unique and match their source.");
        }
        if(input.Weekends.Select(w=>w.Source.Circuit.Id).Distinct().Count()!=1
            ||field.Values.Select(v=>v.Car.Source.Driver.Id).Distinct().Count()!=field.Count
            ||input.Grid.Count!=field.Count||input.Grid.Any(g=>g is null||!field.ContainsKey(g.EntryId))
            ||input.Grid.Select(g=>g.EntryId).Distinct().Count()!=field.Count
            ||!input.Grid.Select(g=>g.Position).Order().SequenceEqual(Enumerable.Range(1,field.Count)))
            throw new ArgumentException("A complete unique contiguous grid and frozen field are required.");
        var ordered=input.Grid.OrderBy(g=>g.Position).ToArray();
        var drivers=StartingDrivers(input);
        var length=input.Configuration.Track.Segments?.Sum(s=>s.Length)??tuning.AggregateLengthMetres;
        var firstLength=input.Configuration.Track.Segments?[0].Length??length;
        var launches=ordered.Select(g=>
        {
            var (weekend,car)=field[g.EntryId];var ratings=car.Source.Driver.Ratings;
            var adjusted=weekend.Setup is null?car.Source.Car.Ratings:WeekendSetupTuning.Apply(car.Source.Car.Ratings,weekend.Setup);
            var demands=weekend.Source.Circuit.Demands;
            var launchFit=(adjusted["traction"]*demands["traction"]+adjusted["accelerationDeployment"]*demands["accelerationDeployment"])
                /Math.Max(1,demands["traction"]+demands["accelerationDeployment"]);
            var ability=(ratings["starts"]+ratings["overtaking"]+ratings["defending"]+launchFit)/400;
            var loss=tuning.BaseLaunchLossSeconds+tuning.AbilitySeconds*(1-ability)
                +tuning.VariationSeconds*Unit(input,car.Source.Driver.Id,"launch")
                +tuning.ErrorSeconds*(1-ratings["errorControl"]/100)*Unit(input,car.Source.Driver.Id,"error");
            return (Offset:(g.Position-1)*tuning.GridSpacingMetres/firstLength,Loss:loss);
        }).ToArray();
        return RaceEngine.CreateStandingStart(input.Configuration,drivers,input.Seed,launches,ResolveOpeningPace(input.ModelVersion));
    }
    public static Driver[] StartingDrivers(AutomaticStartInput input)
    {
        var drivers=input.Weekends.SelectMany(weekend=>weekend.Cars.Select(car=>input.Configuration.Incidents is null?car.RaceDriver:car.RaceDriver with{
            IncidentProfile=new(car.Source.Driver.Ratings["errorControl"],car.Source.Car.Ratings["reliability"],
                SourceVersion:$"local-profile-v1/{car.Source.Driver.Version}/{car.Source.Car.Version}/{weekend.Source.Circuit.Version}")
        })).ToDictionary(driver=>driver.Id);
        return input.Grid.OrderBy(slot=>slot.Position).Select(slot=>drivers[slot.EntryId] with{Gap=0}).ToArray();
    }
    // V1 retains its historical Conserve opening for exact saved-input replay.
    private static PaceInstruction ResolveOpeningPace(string model)=>model==LegacyModelVersion?PaceInstruction.Conserve:PaceInstruction.Standard;
    private static double Unit(AutomaticStartInput input,string driver,string eventId)=>
        BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new object[]{RandomnessVersion,input.Seed,driver,"opening",eventId}))))/(double)uint.MaxValue;
}
