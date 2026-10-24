using System.Text.Json.Serialization;
namespace Game.Domain;

public sealed record StartGridSlot(int Position,string EntryId);
// Authored geometry, never derived from circuit imagery. Aggregate tracks use the explicit reference length.
public sealed record StartTuning(double GridSpacingMetres=8,double AggregateLengthMetres=5000,
    double BaseLaunchLossSeconds=3,double AbilitySeconds=2,double VariationSeconds=1,double ErrorSeconds=2);
public sealed record AutomaticStartInput(RaceConfiguration Configuration,IReadOnlyList<EffectiveWeekend> Weekends,
    IReadOnlyList<StartGridSlot> Grid,int Seed,StartTuning Tuning,
    string ModelVersion="automatic-standing-start-v2",string RandomnessVersion="opening-keyed-v1",
    [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string,TyreCompound>? StartingCompounds=null);
public sealed record AutomaticStartReplay(AutomaticStartInput Input,string OutcomeHash,string PreparationContextId);
