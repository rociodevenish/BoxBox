using System.Collections.Immutable;
using Game.Domain;
namespace Game.Simulation;

/// <summary>Materialises completed roster inputs from the campaign's frozen pool; never reloads current content.</summary>
public static class PersonnelWeekendMapping
{
    public static WeekendPreset Apply(AcquisitionState acquisition, string teamId, WeekendPreset preset)
    {
        ArgumentNullException.ThrowIfNull(acquisition);
        ArgumentNullException.ThrowIfNull(preset);
        PersonnelAcquisition.Validate(acquisition);
        if (!acquisition.TeamIds.Contains(teamId) || !PersonnelAcquisition.RosterReady(acquisition, teamId)
            || preset.Cars.Length != 2 || preset.Cars.Any(car => car.TeamId != teamId))
            throw new ArgumentException("A complete acquired roster and matching two-car constructor are required.");
        PersonnelCard Card(string slot) => acquisition.Pool.People.Single(person => person.Id ==
            acquisition.Hires.Single(hire => hire.TeamId == teamId && hire.Slot == slot).PersonId);
        var cars = preset.Cars.Select((car, index) =>
        {
            var driver = Card($"driver-{index + 1}").Driver
                ?? throw new ArgumentException("Frozen acquired driver inputs are missing.");
            var engineer = Card($"race-engineer-{index + 1}");
            if (engineer.Attributes is null || !engineer.Attributes.TryGetValue("Setup", out var setup))
                throw new ArgumentException("Frozen Race Engineer Setup is required.");
            return car with { Driver = driver, Engineer = new(engineer.Id, engineer.Name, "authored-engineer-v1", setup) };
        }).ToImmutableArray();
        var result = preset with { Cars = cars };
        _ = WeekendPreparation.Prepare(result, 1);
        return result;
    }
}
