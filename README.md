# F1Game
An online F1 game

## Dry-race strategy laboratory

Requires .NET SDK 10. Run from the repository root:

```powershell
dotnet run --project tools/SimulationRunner -- 50 7 mixed technical
dotnet test tests/Game.Simulation.Tests
```

Arguments currently select race distance, seed, optional `mixed` field and optional
`technical` track. The default field has equal performance on the Fast track.
Readable and structured results are written to `reports/race.txt` and
`reports/race.json`. Numerical coefficients are initial modelling hypotheses.

Add `push` or `conserve` to set the baseline pace. Scheduled per-driver overrides
use zero-based phase numbers:

```powershell
dotnet run --project tools/SimulationRunner -- 50 7 mixed technical --script examples/scheduled-strategy.json
```

Unspecified pace/pit choices are Standard/Stay Out. Results using fewer than two
compounds are marked ineligible and retained for diagnosis.

Conditional plans use `--strategy examples/conditional-strategy.json`. Scheduled
decisions override conditions; otherwise the first matching condition wins.
Conditions use team estimates or public leader lap, position and gap. A rule with
`once: true` is consumed only when selected. Pending or ongoing stops suppress
conditional pit rules. The default pace applies when no rule matches.
Exact tyre condition is diagnostic information and is excluded from team observations.
Plans are fixed for the duration of a run; replacing or mutating a bound plan is rejected.
