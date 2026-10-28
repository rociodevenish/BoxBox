using System.Collections.Immutable;
using Game.Domain;
namespace Game.Simulation;
/// <summary>Materializes a separate event car from the original baseline. Construction metadata retains its handling axis.</summary>
public static class DevelopedCar
{
 public static WeekendPreset Apply(WeekendPreset baseline,IEnumerable<PermanentModifier> modifiers)
 {
  if(!baseline.DevelopmentProjects.IsDefaultOrEmpty)throw new ArgumentException("Permanent modifiers have already been materialized.");
  var own=modifiers.Where(m=>m.TeamId==baseline.Cars[0].TeamId).ToArray();
  if(own.Select(m=>m.ProjectId).Distinct().Count()!=own.Length)throw new ArgumentException("Duplicate permanent modifier.");
  if(own.Any(m=>m.Changes.Any(p=>!WeekendPreparation.CarKeys.Contains(p.Key)||!double.IsFinite(p.Value)||p.Value is < -3 or >6)))throw new ArgumentException("Unsupported permanent gain.");
  return baseline with{DevelopmentProjects=own.Select(m=>m.ProjectId).ToImmutableArray(),Cars=baseline.Cars.Select(c=>c with{Car=c.Car with{Ratings=c.Car.Ratings.ToImmutableDictionary(p=>p.Key,p=>Math.Clamp(p.Value+own.Sum(m=>m.Changes.GetValueOrDefault(p.Key)),1,100))}}).ToImmutableArray()};
 }
}
