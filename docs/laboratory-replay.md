# Versioned laboratory replay

Run a normal race with a configuration and strategy, then use the exported inputs:

```powershell
dotnet run --project tools/SimulationRunner -- --config config.json --strategy strategy.json --output reports/design
dotnet run --project tools/SimulationRunner -- --replay reports/design/race-replay.json --output reports/replayed
```

Comparisons export `comparison-replay.json`, accepted by the same `--replay` option. Replay accepts only an input path and output directory; distance, seed, field, plans and tuning come from the recorded inputs. Unknown properties or missing required replay fields, omitted seed/version/explicit tuning, malformed layouts and incompatible model/randomness contracts fail before reports are written. Model versions must match the recorded layout. A historical seed is not claimed equivalent across contracts.

Machine reports retain existing phases/results and add a self-contained replay, metadata with units/distributions/scopes/magnitudes, and designer-only resolved channel diagnostics. Readable race and paired reports include segment, cumulative distance and encounter identities, plus scoped diagnostic values. Race event flows carry actual spatial/battle outcomes, never raw random draws or cached future success. Team observations remain unchanged.

Replay snapshots ordered layouts, drivers, plan arrays/dictionaries and paired seeds. Exported replay configuration explicitly contains tyre and traffic tuning. Byte-identical machine replay is checked by `tools/verify-runner.ps1` for both a segmented race and paired comparison. Public contract tests also compare ordered events, decisions, Classification and diagnostics after mutating the original input arrays.

Paired reports preserve corresponding driver/race and driver/lap draws. Segmented service uses ordinal stops; changed strategies may introduce/remove stop and encounter identities. Opponent plans stay fixed, while their traffic outcomes and completed distances may change. Position differences and eligible equal-distance time differences remain separate. Eligibility/distance groups include sample counts, mean, sample standard deviation and extrema, including incompatible/ineligible groups as diagnostics. All published seeds and full replay inputs are retained.

Generated full histories live under ignored `reports/`; evidence summaries can be published separately without committing duplicate phase histories.
