# S10 independent review remediation

Both P2 findings are resolved through the approved public RaceEngine boundary. Fractional reaction identities now use invariant round-trip formatting. Deferred suitability crossings evaluate both outward edges of the remembered band at the first separation-eligible time in the current interval, regardless of rain slope, before predicting a later crossing.

## Red and green

- Culture regression failed with a comma in the German identity, then passed with identical en-US/de-DE identities and full AutomaticRaceStart outcome hashes at the literal 73.33333333333333-second window.
- Seed 14 gives full rain until 60 seconds, then no rain until 180 seconds. Flat and reversing cases first open Intermediate at 14 seconds. Both reproduced the original defect (next result at 180 instead of Wet at 84) before the scheduling fix, then passed. An initial exploratory seed 9 assertion was incorrect and corrected before the valid red run; it is not counted as defect evidence.
- Retained public cases verify reversal back below the upper edge waits for Dry at 99 seconds; a weather window at the ordinary five-lap boundary opens once at 84; a coincident Safety Car deadline ends control before the deferred window at 84; finish at 84 suppresses the window.
- 24 unique focused simulation checks passed: 23 across WeatherReactionTests, ReplayTests and WetWeekendStartTests, followed by the one additional ordinary-boundary check. Seven cases are new. Earlier overlapping green runs are not added to this count.

The bounded command records and launched PIDs are preserved in completion.json. The initial sandboxed build returned before tests; focused .NET build/VSTest IPC was rerun with narrow escalation. NuGetAudit=false was invocation-only. No dependency, security policy, network listener or preview process was changed.

## Verification isolation

The root broad run reached backend after tooling/typecheck/build, but backend tests did not launch because an existing unrelated preview owns the default recovery emulator ports (Auth 19109, Firestore 19100, hub 19410, logging 19460, websocket 9150; owner record .tools/reports/personal-qualifying/processes.json). That preview remains running. Backend now accepts F1_BACKEND_EMULATOR_CONFIG; deadlines accept matching F1_DEADLINES_AUTH_PORT/FIRESTORE_PORT/EMULATOR_CONFIG; control accepts F1_CONTROL_WEBSOCKET_PORT. Defaults are preserved. Node 24 syntax checking of tools/verify.mjs passed. Root owns affected backend and remaining named stages with its custom owned emulator configuration; this worker did not repeat the broad run or launch UI services.

## Acceptance limits

No new UI or imagery was needed. Human household pacing, forecast-understanding and simultaneous-trigger clarity measurements remain pending in ticket 06; these simulation checks do not fulfill that acceptance item. No claims of CI execution, calibrated weather probabilities or final balance are added.

## Frozen replay compatibility

All 560 published complete 22-car weekends replayed exactly across all ten original archives: full qualifying results, opening hash, every operation hash, compact final outcomes and forecast results. Replay took 161410 ms across ten bounded commands. The archived files were not rewritten; their SHA-256 values are retained in completion.json. Neither scheduling semantics nor invariant formatting changed any saved outcome, so no archive migration or additional reaction version was needed. The authored weather-reactions-v1 contract already promises the repaired first still-applicable crossing behavior. Integration tip a2d7097d7ff74fee8aaaae0322eb6a3246bcb608 was merged (already up to date) before this commit. Worktree retained for integration.
