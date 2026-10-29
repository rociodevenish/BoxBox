using System.Text.Json;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public sealed partial class SessionCoordinator
{
 private PackageSelectionObservation ObservePackageSelection(PreparedTeam team)
 {
  var preset=RacePackages.Migrate(team.Preset);
  var incidents=team.Effective.Configuration.Incidents??openingDisruptions?.ForCircuit(preset.Circuit.Id)?.Incidents;
  return new(preset.Circuit.Id,preset.Circuit.Demands,preset.Cars[0].Car.Ratings,preset.PackageLibrary,incidents is {FailureRate:>0});
 }
 private PreparedTeam SelectAutomaticPackage(PreparedTeam team)
 {
  // A human selection, including explicit neutral, is a decision. Existing practice also owns its context.
  if(team.Practice is not null||team.PackageChoices?.Count>0
   ||team.Commands.Values.Any(c=>c.Fingerprint.Contains("\"Operation\":\"package-selection\"",StringComparison.Ordinal)))return team;
  var observation=ObservePackageSelection(team);var recommendation=PackageSelectionDecisionPolicy.Evaluate(observation);
  return team with{Preset=RacePackages.Select(team.Preset,recommendation.Selection),
   PackageChoices=[new("automatic",observation,recommendation,recommendation.Selection)]};
 }
 private static void ValidatePackageChoices(PreparedLobby state)
 {
  try
  {
   foreach(var (teamId,team) in state.Teams)
   {
    var preset=RacePackages.Migrate(team.Preset);
    foreach(var choice in team.PackageChoices??[])
    {
     var observation=choice.Observation;
     if(choice.Source is not("manual" or "assisted" or "automatic")
      ||observation.CircuitId!=preset.Circuit.Id
      ||!observation.CircuitDemands.OrderBy(p=>p.Key).SequenceEqual(preset.Circuit.Demands.OrderBy(p=>p.Key))
      ||!observation.SourceRatings.OrderBy(p=>p.Key).SequenceEqual(preset.Cars[0].Car.Ratings.OrderBy(p=>p.Key))
      ||observation.Library.Any(p=>!preset.PackageLibrary.Any(saved=>JsonSerializer.Serialize(saved)==JsonSerializer.Serialize(p)))
      ||JsonSerializer.Serialize(choice.Recommendation)!=JsonSerializer.Serialize(PackageSelectionDecisionPolicy.Evaluate(observation))
      ||choice.Source!="manual"&&choice.Selection!=choice.Recommendation.Selection)
      throw new ArgumentException("Saved package selection policy does not replay from its own public observations.");
     _=RacePackages.Select(preset with{PackageLibrary=observation.Library},choice.Selection);
    }
    if(team.PackageChoices?.LastOrDefault() is {} latest&&latest.Selection!=preset.SelectedPackage)
     throw new ArgumentException("Saved package selection differs from its recorded choice.");
    if(state.Frozen?.Teams.SingleOrDefault(t=>t.TeamId==teamId) is {} frozen
     &&JsonSerializer.Serialize(frozen.PackageChoices)!=JsonSerializer.Serialize(team.PackageChoices))
     throw new ArgumentException("Frozen package selection history differs from preparation.");
   }
  }
  catch(ArgumentException error){throw new SessionFault(SessionFailure.Conflict,error.Message);}
 }
}
