using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public record RacePreset(RaceConfiguration Configuration,IReadOnlyList<Driver> Drivers);
public interface IRacePresets { RacePreset Get(string name,int laps); }
/// <summary>Server-owned configurations; public creation still accepts only four names.</summary>
public sealed class DryRacePresets : IRacePresets
{
    public RacePreset Get(string name,int laps)
    {
        (bool Restricted, bool Mixed) variant=name switch {"open-equal"=>(false,false),"restricted-equal"=>(true,false),
            "open-mixed"=>(false,true),"restricted-mixed"=>(true,true),_=>throw new ArgumentException("Unknown dry-race preset.")};
        return new(new RaceConfiguration(laps,LaboratoryFixtures.SegmentedTrack(variant.Restricted),LapVariation:.2).WithExplicitTuning(),
            Array.AsReadOnly(LaboratoryFixtures.SegmentedField(variant.Mixed)));
    }
}
