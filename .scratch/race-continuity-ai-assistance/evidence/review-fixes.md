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

## Residual integrated fixtures, 12 October

Root's integrated backend rerun exposed five remaining API fixture failures (97 passed/5 failed, simulation111/111 passed). The five affected classes now pass25/25 in `review-residual-api-green.json` / `trx/review-residual-api-green.trx`; initial focused diagnostics remain in `review-residual-api.json` / `trx/review-residual-api.trx`.

The crowded first window now checks the actual21 pit entries/exits/tyre changes, all22 completed-stop counts, a12-event cap and exact reported/omitted totals. Later events must retain fresh IDs/times rather than carry over earlier feedback. The strict tie preset deliberately eliminates lap-time differences from tuning/randomness while retaining legitimate observation-policy decisions and real stops. Its actual recorded accepted plan proves identical driver01/02 lap completion times and public fastest-lap selection; recent history length follows each driver's completed laps without inventing lap1. Open spectator schema permits the intended current Assistance field, retains the private-data exclusions and does not invent a ControlStatus field absent from that DTO. Designer-only diagnostics compare unchanged public state except server time. Production tests clear and restore only the inherited Auth/Firestore emulator environment in an isolated nonparallel collection; the production startup guard is unchanged, and negative configuration cases now exercise their own unsafe setting.

Root's browser run exposed three further old fixture assumptions (19 passed/3 failed): an AI-populated phase called quiet, whole public equality including server time, and old-session eviction after creation of another race. Focused UI4499/API4599 checks passed3/3 in `review-residual-browser.json`. Reduced-motion checks retain immediate completed feedback/no Skip action, verify every actual published event is visible, and ensure replay shows the same event list. Lost acknowledgement still causes exactly one submission and unchanged public state except time. Creating another race now checks retained finished choices and state, distinct IDs/capabilities, reciprocal private-access denial, and an independent unstarted new race.

Both real queued/ongoing stop browser checks passed2/2 in `review-residual-boundary.json` after removing only serverTimeUtc from their public comparison; exact team-state equality and rejected two-car draft assertions remain. Browser tests share a clock-only comparison helper. API TRX/command records and the focused browser command records are preserved under root ignored `.tools/reports/s06/review-fixes/`. No product reduced-motion behavior or production guard was changed. Root continues the integrated verification and owns remaining browser/shared/dedicated-suite results. The worktree remains available through final verification.

## Shared presentation fixtures

Root's shared run found four timing assumptions (4 passed/4 failed) after the durable8-second result presentation was integrated. The four affected handoff, delayed acknowledgement, host control and multiteam cases now pass4/4 in `review-residual-shared.json` (105.8 seconds including startup), using owned UI4499/API4599, Auth19349/Firestore19340 and the configured isolated hub/log ports.

Tests open an authenticated host lobby and use its visible Continue action before approving the next window. Replay/Skip is checked while feedback is visible, rather than assuming an ephemeral Skip remains after handoff. Original Ready-only private access, discarded drafts, late private wire exclusion, accepted per-team locks, delayed HTTP acknowledgement preserving a newer Push draft, host pause/resume/revocation, guest authority limits and independent22-row public display checks remain. Shared fixtures explicitly use manual wakeups so automatic completion cannot race capture/replay/late-ack setup; the deadline/progression acceptance tests cover due/automatic transitions separately. This is test configuration, not a product authority change. Shared emulator port/config overrides are documented for concurrent focused isolation.
