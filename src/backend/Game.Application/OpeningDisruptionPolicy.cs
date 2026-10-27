using Game.Domain;
namespace Game.Application;
// Explicit server composition gate. Production registers a policy only through a versioned opt-in.
// A circuit needs an authored route; aggregate PitLaneLoss is never interpreted as lane travel.
public sealed record OpeningDisruptionConfiguration(PitRoute Route,IncidentTuning Incidents,string SourceVersion="authored-disruption-fixture-v1");
public interface IOpeningDisruptionPolicy
{
    OpeningDisruptionConfiguration? ForCircuit(string circuitId);
}
