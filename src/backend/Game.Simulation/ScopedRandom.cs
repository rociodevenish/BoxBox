namespace Game.Simulation;

internal enum RandomChannel { RaceForm, LapVariation, PitService, Passing, EstimateError }
internal static class ScopedRandom
{
    public static double Unit(int seed, RandomChannel channel, string driver, int scope, string? encounter = null)
    {
        var kind = channel switch
        {
            RandomChannel.RaceForm => "form",
            RandomChannel.LapVariation => "lap",
            RandomChannel.PitService => "stop",
            RandomChannel.EstimateError => "estimate",
            RandomChannel.Passing => encounter ?? throw new ArgumentException("Passing needs an encounter identity."),
            _ => throw new ArgumentException("Unknown random channel.")
        };
        uint hash = 2166136261;
        foreach (var ch in $"{seed}:{driver}:{scope}:{kind}") hash = unchecked((hash ^ ch) * 16777619);
        hash ^= hash >> 16;
        hash = unchecked(hash * 0x7feb352d);
        hash ^= hash >> 15;
        return hash / (double)uint.MaxValue;
    }
    public static double Offset(int seed, RandomChannel channel, string driver, int scope, double fullRange)
        => fullRange == 0 ? 0 : (Unit(seed, channel, driver, scope) - .5) * fullRange;
    public static double PassingThreshold(int seed, string driver, int scope, string encounter, double fullRange)
        => fullRange == 0 ? .5 : .5 + (Unit(seed, RandomChannel.Passing, driver, scope, encounter) - .5) * fullRange;
}
