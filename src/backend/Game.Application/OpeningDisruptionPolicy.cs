using Game.Domain;
namespace Game.Application;
// Explicit server composition gate. Production leaves this unregistered until rule/strategy evidence is accepted.
// A circuit needs an authored route; aggregate PitLaneLoss is never interpreted as lane travel.
public sealed record OpeningDisruptionConfiguration(PitRoute Route,IncidentTuning Incidents);
public interface IOpeningDisruptionPolicy
{
    OpeningDisruptionConfiguration? ForCircuit(string circuitId);
}
