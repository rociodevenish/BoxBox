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

Compare one driver's alternative over paired seeds:

```powershell
dotnet run --project tools/SimulationRunner -- 30 7 mixed --strategy examples/conditional-strategy.json --compare examples/conditional-strategy.json --driver driver-01 --runs 20
```

The alternative file may contain only the selected driver's rules. Its default pace
applies only to that driver; all opponent scripts and defaults remain fixed.
Negative differences favour the alternative. Eligible equal-distance time results
are separate from raw diagnostic and eligibility/distance groups. Variation is
sample standard deviation. Comparing a plan with itself should give zero differences.
Use `--output <directory>` to keep experiments separate and `--config <file>` to
load a complete RaceConfiguration with experimental numerical coefficients.

Generate the strategy evidence suite (20 paired seeds per experiment):

```powershell
dotnet run --project tools/SimulationRunner -- experiments --runs 20 --output reports/evidence
powershell -File tools/verify-runner.ps1
```

The suite writes paired inputs and results for each experiment, full race reports
for both field/track fixtures, and `evidence.md` with interpretation and timings.
Some isolation controls deliberately omit mandatory stops and are labelled
ineligible; legal pit-window comparisons are reported separately.

The current model uses track-weighted whole-lap pace, with linear movement between
catch, lap-crossing and pit-exit events. Cornering changes aggregate lap time;
there are no individual corners or passing zones. Sector-specific pace and passing
remain a modelling question before treating this as the final race model.

## Verification

Run `node tools/verify.mjs` after installing the locked dependencies and Chromium. See [verification workflow](docs/agents/verification-workflow.md) for setup, isolated ports, focused reruns, bounded command records, CI and explicit evidence publishing.
