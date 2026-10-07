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
