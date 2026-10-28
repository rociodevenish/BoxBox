# Construction evidence

Build with the repository's pinned .NET SDK:

```powershell
dotnet build tools/ConstructionEvidence -p:NuGetAudit=false
dotnet tools/ConstructionEvidence/bin/Debug/net10.0/ConstructionEvidence.dll NEW_EMPTY_DIRECTORY
dotnet tools/ConstructionEvidence/bin/Debug/net10.0/ConstructionEvidence.dll --replay PATH/construction.json.gz
node --test tools/ConstructionEvidence/evidence.test.mjs
```

`DOTNET_EXE` and `CONSTRUCTION_EVIDENCE_DLL` override the test runtime and built assembly. Commands must be wrapped with `tools/run-bounded.mjs` for verification records.

The compact archive contains the frozen catalogue, each materialized source preset, effective race configuration, full 22-entry grid, strategy plan and seeds. Retained raw public outcomes include every classification, tyre state, eligibility decision, target lap/pit/tyre event and every incident event. Each run also records the SHA256 of its **complete** final public PhaseResult, including the high-volume traffic/event stream omitted from storage. Replay recreates that complete result and checks its hash as well as all retained outcomes. It reconstructs effective inputs through WeekendPreparation and replays RaceEngine, checking catalogue/cost/balance, scenario coverage, source car ratings, grid, strategy and model versions. Current embedded catalogue files are not consulted during replay.

Five builds (cheapest legal 18-credit baseline, low-drag, cornering, tyre and reliability) are tested on supported Monza and Monaco fixtures. The baseline is a selected purchasable build, distinct from the legacy neutral-82 preset and from the unselected source baseline (82, Reliability 84). Only team-01 changes. All other 20 entries retain the same baseline car and all 22 supplied drivers retain their ratings and preferences. Target team starts first and second with 0.8-second grid gaps. Neutral Weekend Setup, no development or Race Package, dry default conditions, Standard pace and a Hard pit at Race Phase 2 are fixed. This is a 20-lap laboratory race strategy, not qualifying or a full Grand Prix/calendar.

Normal strata use 30 paired seeds (101–130), with production/default incidents unchanged. Designer-only diagnostic strata use 100 paired seeds (101–200) and explicitly enable existing `local-incidents-v1` failure hazards at .00003 per running second and the supported circuit mechanical stress mapping. No forced incident or future incident observation informs a choice. These are separate strata, not a combined model improvement estimate. Paired initial seeds do not imply identical incident draw paths once race states diverge.

Each cost is a real integer-credit catalogue price. The declared 24-credit post-personnel envelope isolates construction opportunity cost; it is not a new starting grant, actual acquired household balance, or a personnel experiment. Remaining credits are 24 minus build cost. The source's advisory 20-unit envelope is superseded by the shared actual ledger (valid complete builds 18–24). `bounded-detailed-deltas-v1` applies the first two modifier points fully, further points at half strength, capped at six; no card-star physics conversion.

`summary.json` reports signed alternative-minus-baseline paired finishing seconds, mean classification position and retirement differences. Seconds are absent if either target car fails to finish in either arm. Conditional finishing comparisons can be selection-biased; raw classifications/retirements are retained. Negative seconds/positions mean faster/better. A zero effect, weakness or apparent dominance is a finding to report, not evidence that all choices are balanced. One roster, two circuits, one grid and one strategy cannot establish full-calendar balance; that comparison remains deferred until validated content exists. Human choice comprehension is not measured by this tool.

Generate all ten pairwise build comparisons within each of the four strata directly from the frozen outcomes with `node tools/ConstructionEvidence/summarize.mjs construction.json.gz NEW-pairwise.json`. To reuse an already-created archive for the contract test without generating another 1300 races, set `CONSTRUCTION_EVIDENCE_OUTPUT` to its containing directory. Corruption checks fail early and the no-overwrite check preserves the original bytes.
