using Game.Domain;

namespace SimulationRunner;

internal record RunnerOptions(int Laps, int Seed, bool Mixed, bool Technical, string? Script,
    string? Strategy, string? Alternative, string Target, int Runs, string Output, string? Configuration,
    PaceInstruction Pace, bool Experiments, string? Replay, string? Suite, string? BaselineBenchmark)
{
    public static RunnerOptions Parse(string[] args)
    {
        var positional = new List<string>();
        var values = new Dictionary<string, string>();
        var flags = new HashSet<string>();
        var named = new[] { "--script", "--strategy", "--compare", "--driver", "--runs", "--output", "--config", "--replay", "--suite", "--baseline-benchmark" };
        for (var i = 0; i < args.Length; i++)
        {
            if (named.Contains(args[i]))
            {
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--") || values.ContainsKey(args[i]))
                    throw new ArgumentException($"{args[i]} needs one value and may appear once.");
                values[args[i]] = args[++i];
            }
            else if (new[] { "mixed", "technical", "push", "conserve", "experiments" }.Contains(args[i])) flags.Add(args[i]);
            else if (args[i].StartsWith("--")) throw new ArgumentException($"Unknown option {args[i]}.");
            else positional.Add(args[i]);
        }
        if (positional.Count > 2 || flags.Contains("push") && flags.Contains("conserve"))
            throw new ArgumentException("Expected optional distance and seed, and one baseline pace.");
        if (values.ContainsKey("--replay") && (positional.Count != 0 || flags.Count != 0
            || values.Keys.Any(k => k is not "--replay" and not "--output")))
            throw new ArgumentException("Replay accepts only --replay and --output; recorded inputs own the run.");
        if (values.ContainsKey("--baseline-benchmark") && values.GetValueOrDefault("--suite") != "benchmark")
            throw new ArgumentException("Baseline benchmark input belongs to --suite benchmark.");
        if (values.TryGetValue("--suite", out var suite) && (suite is not "track-passing" and not "tyre-pit" and not "form-noise" and not "benchmark" || flags.Count != 0
            || values.Keys.Any(k => k is not "--suite" and not "--output" and not "--runs" && (k != "--baseline-benchmark" || suite != "benchmark"))
            || positional.Count > 0 && positional[0] != "40"))
            throw new ArgumentException("Segment suite owns its 40-lap fixtures and plans; accepts only --suite, --runs, --output and optional 40/seed.");
        var laps = positional.Count > 0 ? int.Parse(positional[0]) : 50;
        var seed = positional.Count > 1 ? int.Parse(positional[1]) : 7;
        var runs = int.Parse(values.GetValueOrDefault("--runs", "20"));
        if (laps < 1 || runs < 1 || (long)seed + runs - 1 > int.MaxValue
            || values.ContainsKey("--script") && values.ContainsKey("--strategy"))
            throw new ArgumentException("Invalid distance, run count or conflicting strategy sources.");
        return new RunnerOptions(laps, seed, flags.Contains("mixed"), flags.Contains("technical"),
            values.GetValueOrDefault("--script"), values.GetValueOrDefault("--strategy"),
            values.GetValueOrDefault("--compare"), values.GetValueOrDefault("--driver", "driver-01"), runs,
            values.GetValueOrDefault("--output", "reports"), values.GetValueOrDefault("--config"),
            flags.Contains("push") ? PaceInstruction.Push : flags.Contains("conserve") ? PaceInstruction.Conserve : PaceInstruction.Standard,
            flags.Contains("experiments"), values.GetValueOrDefault("--replay"), values.GetValueOrDefault("--suite"), values.GetValueOrDefault("--baseline-benchmark"));
    }
}
