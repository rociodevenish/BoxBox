using Game.Domain;
namespace Game.Application;

// Opt-in composition after S09 bounded laboratory evidence; coefficients remain authored hypotheses.
public sealed class AuthoredDisruptionPolicy : IOpeningDisruptionPolicy
{
    public const string ContentVersion="disruption-live-opt-in-v1";
    public OpeningDisruptionConfiguration? ForCircuit(string circuitId)=>circuitId switch
    {
        "track-monza"=>new(new(.2,38),Tuning,ContentVersion),
        "track-monaco"=>new(new(.15,32),Tuning,ContentVersion),
        _=>null // Other supplied circuits remain incident-free until a route is authored and reviewed.
    };
    private static readonly IncidentTuning Tuning=new(ErrorRate:.0003,ContactProbability:.02,FailureRate:.00003,
        ComponentWearPerSecond:.00001,InterruptMajorIncidents:true);
}

