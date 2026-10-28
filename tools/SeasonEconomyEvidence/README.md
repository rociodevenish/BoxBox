# Short-season economy evidence

Uses public acquisition, construction, income, development, staff mapping, effective-car and race/scoring operations. The authored calendar is Monza → Monaco: two events, one opportunity. It does not simulate a fictional full calendar or measure people.

With the pinned SDK, from repository root:

```powershell
dotnet build tools/SeasonEconomyEvidence -p:NuGetAudit=false
dotnet tools/SeasonEconomyEvidence/bin/Debug/net10.0/SeasonEconomyEvidence.dll .tools/reports/season-economy-new
dotnet tools/SeasonEconomyEvidence/bin/Debug/net10.0/SeasonEconomyEvidence.dll --replay .tools/reports/season-economy-new/season-economy.json.gz
```

Use `DOTNET_EXE`/the absolute pinned SDK when it is not on PATH. Wrap commands with `tools/run-bounded.mjs` and a unique report path as described in the verification workflow. Generation refuses nonempty output directories. `--pilot <new-directory>` runs seed101 for a format/runtime check; ordinary generation predeclares seeds101–124. `node --test tools/SeasonEconomyEvidence/evidence.test.mjs` checks a pilot's exact replay, overwrite rejection and damaged receipts/race/version evidence. `SEASON_EVIDENCE_OUTPUT` can point to an existing generated archive for that check.

The matrix has two legal acquisition policies, three car contexts and three actions: 432 counterfactuals. Only team01 changes hiring policy; opponents choose their cheapest actual legal offer. The target chooses cheapest or highest-priced legal actual offers, preserving reserve and unique ownership. Staff price is not presumed strength, and later market/draw spillovers are retained. Every constructor confirms a minimum-cost car; the target instead attempts a named speed/mechanical philosophy in those contexts. An unaffordable philosophy explicitly records its rejected build and retains the minimum car. Philosophy costs differ; use the shared minimum context for personnel comparisons.

Development siblings share the same roster/build/event-one race, event seeds, opportunity/project IDs and hidden development-channel key. Save is compared with reinforcement (speed on speed/minimum, handling on mechanical) and complementary refinement (the opposite). Rejected investments retain their error and unchanged ledger, then explicitly choose Save; no credits are injected. Default production uncertainty remains active. Delay at the final opportunity stays pending beyond this supported calendar, with no invented completion or repayment.

The gzip archive freezes every source card, acquisition draw/offer/receipt, construction confirmation, quote/staff/controls, outcome/modifier, exact ledger, effective preset, race replay/classification and scoring award. Incident tuning is mapped once from source content; replay uses frozen tuning and public starting-driver composition without reloading catalogues. Replay reconstructs all 432 counterfactuals through public operations and compares complete evidence. `summary.json` includes every rejected/inconclusive row, paired seconds (with non-comparable finishes retained), credits and points margins. `manifest.json` pins seeds and archive SHA256.

Early-lead diagnostics retain the actual event-one leader and points margins before/after event two, plus final-margin differences against each identical Save sibling. Empty leading subgroups, unchanged points, slower investments and pending work remain visible. Two events cannot establish season-long runaway safety. Real workload/understanding, full-calendar content, release prices and fairness remain separate, unmeasured acceptance work.
