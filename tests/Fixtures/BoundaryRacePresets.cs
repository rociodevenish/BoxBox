using Game.Application;
using Game.Simulation;
namespace Game.TestFixtures;
/// <summary>Real-engine stop boundary fixture; never registered by the production host.</summary>
public sealed class BoundaryRacePresets(double gap) : IRacePresets
{
    public RacePreset Get(string name,int laps)
    {
        var preset=new DryRacePresets().Get(name,laps);
        var drivers=preset.Drivers.Select(driver=>driver.Id=="driver-02"?driver with {Gap=gap}:driver).ToArray();
        return preset with {Drivers=Array.AsReadOnly(drivers)};
    }
}
