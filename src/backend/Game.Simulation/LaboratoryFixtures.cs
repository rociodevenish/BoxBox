using Game.Domain;

namespace Game.Simulation;

public static class LaboratoryFixtures
{
    public static Driver[] Field(bool mixed) => Enumerable.Range(1, 22)
        .Select(i => new Driver($"driver-{i:00}", $"team-{(i + 1) / 2:00}",
            Speed: mixed ? (22 - i) * .1 : 0, Cornering: mixed ? (i % 5) * .1 : 0,
            Gap: (i - 1) * .5)).ToArray();

    public static Track Track(bool technical) => technical
        ? new Track("Technical", 95, .2, PassingChance: .08)
        : new Track("Fast", 85, .8, PassingChance: .8);
}
