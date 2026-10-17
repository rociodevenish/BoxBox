using Game.Application;
using Game.Domain;
namespace Game.TestFixtures;
// Opt-in developer laboratory only. Never registered by Game.Api.
public sealed class SafetyCarWindowPresets:IRacePresets
{
    public RacePreset Get(string name,int laps)=>new(new(laps,new("Observed incident laboratory",PitRoute:new(.2,38)),
        Tyres:new(DegradationPerLap:0,WarmupPenalty:0),Traffic:new(Enabled:false),
        Incidents:new(ErrorRate:name=="restricted-equal"?0:.01,FailureRate:name=="restricted-equal"?.1:0,ComponentWearPerSecond:0,InterruptMajorIncidents:true)),
        Enumerable.Range(1,22).Select(i=>new Driver($"driver-{i:00}",$"team-{(i+1)/2:00}",Gap:name=="restricted-equal"&&i>2?600+i:i-1,IncidentProfile:new(ErrorControl:1))).ToArray());
}
