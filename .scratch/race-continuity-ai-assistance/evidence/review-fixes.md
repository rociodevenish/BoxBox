# S06 review fixes

Implemented against `5c4d4045d87e8eb5c119d48384cfa405a707201f` on `codex/s06-review-fixes`, 12 October 2026. This responds to the independent Spec and Standards findings recorded by the integration branch.

## Spec P2: assistance provenance

Each newly opened human window retains its phase/team help level, auto-accept setting, concrete two-car recommendation and the recommendation policy version in durable module state. Current private advice includes that policy version. Designer replay exports the history beside actual accepted decisions; it does not substitute advice for the accepted plan. Public displays and other player payloads do not expose historical recommendations.

A real Firestore/public API regression first failed for the missing recommendation policy version. It now verifies a Guided human's different accepted choice, changed Assisted settings in the following phase, and both recommendations after restart. The saved AI script is deliberately legacy `dry-opponents-v1` while retained assistance is observation policy v2. Old records without history or recommendation version remain readable, with no invented historical provenance. Recovery's intentional pause is explicitly resumed before opening the next window.

## Standards P3: resolution and control

One committed-resolution helper now records the original acknowledgement when there is a human command, clears pending/deadline state, applies phase-committed bookkeeping and persists. Live callers retain publication; recovery retains pause and no publication. Automatic timeout completion keeps its null human command. Existing real failure/restart/CAS checks and the dedicated timeout recovery check remain green.

Control intent and stored team modes use an internal enum; the existing serialized/public strings `Human`, `TemporaryAi` and `PermanentAi` remain unchanged. Both automatic plan provenance and public responsibility sources use one mapping. Membership and private access remain separate from control mode.

## Integration regressions

Repeated observation assertions compare all serialized tyre observation values rather than nested collection references. Public no-change assertions exclude only the advancing `serverTimeUtc` field. The standalone phase test checks both explicit human choices and all 22 recorded accepted inputs, independently resolves those inputs through the simulation engine, and compares the public time/classification and ordered SignalR notifications. Stop-boundary assertions still test both real queued and in-pit states. Durable/progression fixtures honor `F1_RECOVERY_HOST` instead of connecting to another run's fixed Firestore listener; lobby creation now reports failed response content.

Gallery discovery excludes assistance/control/deadlines/hosted integration folders. `tools/verify.mjs` runs their dedicated configs as four named sequential steps. Browser API launchers explicitly set Development so tests do not weaken production validation. The verification workflow documents the separation and required emulator ports.

## Focused verification

Owned real emulators: Auth19349, Firestore19340, hub19550, logging19600; Node24.19.0, Java21.0.12.1, .NET10.0.401. Existing preview listeners were preserved. Raw records remain under the review worktree's ignored `.tools/reports/`:

- `review-red.json`, `trx/review-red.trx`: new provenance regression failed for missing policy version; old-record compatibility passed.
- `review-focused-first.json`, `trx/review-focused-first.trx`: initial diagnostic run retained, including wrong fixed ports and legacy opponent expectation.
- `review-focused-second.json`, `trx/review-focused-second.trx`: 46/46 affected API cases passed (recovery including durable/AI, session, stop boundary, progression, assistance and provenance).
- `review-control-final.json`, `trx/review-control-final.trx`: 8/8 control-transfer/host-control cases passed after the final exhaustive typed source mapping. An empty `ControlTests` name filter was corrected to the actual `ControlTransferTests`/`HostControlsTests` classes; it is not counted as verification.
- `review-deadlines.json`, `trx/review-deadlines.trx`: 7/7 deadline cases passed, including null-command pending timeout recovery.
- `review-strategy.json`, `trx/review-strategy.trx`: 6/6 affected simulation cases passed.
- `node --check tools/verify.mjs` passed. Playwright discovery lists 34 gallery checks and one check in each of the four dedicated configs; no browser servers were started by discovery.

These focused sets are distinct (67 unique cases). No broad suite or browser execution was duplicated here. Root owns the integrated backend rerun and remaining browser steps. Hosted deployment and actual household evidence remain pending; local direct-origin browser tests do not satisfy hosted acceptance.
