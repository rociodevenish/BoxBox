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
    public static Driver[] SegmentedField(bool mixed) => Enumerable.Range(1, 22)
        .Select(i => new Driver($"driver-{i:00}", $"team-{(i + 1) / 2:00}",
            Speed: mixed && i % 2 == 1 ? 4 : 0, Cornering: mixed && i % 2 == 0 ? 4 : 0,
            Pace: mixed && i > 2 ? (i % 5 - 2) * .1 : 0, Gap: (i - 1) * .5)).ToArray();

    public static Track SegmentedTrack(bool restricted, double difficulty = .4) => new(
        restricted ? "RestrictedSegment" : "OpenSegment", 90, .5, PassingChance: .4, Segments: restricted
        ? [new("corner-1", 1000, 30, .1, .9, 1.15, false, difficulty),
           new("straight", 3000, 30, 1, 0, .9, true, difficulty),
           new("corner-2", 1000, 30, .4, .6, 1.15, false, difficulty)]
        : [new("straight-1", 1250, 22.5, 1, 0, .9, true, difficulty),
           new("corner-1", 1250, 22.5, 0, 1, 1.1, false, difficulty),
           new("straight-2", 1250, 22.5, 1, 0, .9, true, difficulty),
           new("corner-2", 1250, 22.5, 0, 1, 1.1, false, difficulty)]);
}
