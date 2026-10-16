# Q1 and preparation lock — local evidence

Implemented on `codex/s08-01`, based on S07 `cd7c61f` and merged integration documentation `67e6174` before commit. This slice ends at recorded Q1 results; Q2/Q3, final grid, expanded policies/advice and automatic opening belong to tickets 02–04.

## Behavior and replay

`Qualifying.Resolve` accepts frozen `EffectiveWeekend` inputs, concrete per-entry choices, fixed pre-session order and versioned rules. `Qualifying.Rank` makes the observable tie rule explicit: valid times first, ascending integer milliseconds, then fixed pre-session order; no-time entries have a null time and use that same order. The default game rule retains 16 of the 22 entries. This is authored game format, not a sporting-regulation claim.

Replay versions are `qualifying-attempt-v1`, `qualifying-keyed-v1` and `qualifying-format-v1`. Independent SHA-256 event keys include randomness version, seed, stable driver identity, Q1/Q2/Q3 session identity, attempt 1 and error/pace event identity. Iteration, diagnostics, retries and presentation consume no random stream. Saved results are checked against the public operation on access/recovery, using recorded inputs rather than current content files.

The host starts through `POST /api/lobbies/{id}/qualifying`. One conditional durable write saves all eleven locked preparation contexts together with the Q1 window, baseline choices, deadline and start receipt. Every human team must be final-ready; unfinished human preparation is rejected without replacing any accepted setup. S07 already records automatic preparation for unclaimed teams. Canonical GUID receipts are checked after current controller authority; changed payloads conflict. Complete team choices and the resolved field are saved before acknowledgement or SignalR notification. Interrupted writes before and after the underlying durable commit recover without resampling.

Unclaimed teams and frozen missing-team fallback plans use disclosed `safe-baseline-v1` Safe choices for both cars. Human locks remain intact at expiry. These concrete baseline choices are fixed before human submissions. The existing decision/handoff timing policy, indexed durable wakeup, local delivery and independent sweep are reused; there is no second timer. Pause stores remaining time and cancels the active wakeup; resume creates a new generation. Duplicate/stale/early deliveries cannot resolve Q1 twice. Expanded AI/assistance remains ticket 03.

Public HTTP/SignalR projections allowlist source identities, team lock/source status, deadlines and recorded outcomes. Seed, effective attributes, compatibility, diagnostics and unexecuted approaches stay private. Household streams are assignment-revision scoped; revocation removes obsolete subscriptions. Intentionally Open family streams still require an approved Open assignment.

## Provisional content and assets

Reviewed the supplied driver and track collections (`1.0.0/2026.10.08-initial-balance`), car vocabulary (`1.0.0/2026.10.08-initial-model`) and construction menu (`1.0.0/2026.10.09-preseason-car-build-v1`). Existing preparation maps detailed 1–100 values, stable content IDs, neutral Base Car and Race Package, setup compromise and circuit demand into frozen effective inputs. Q1 uses detailed Qualifying Pace, prepared straight/corner fit, setup compatibility and Error Control. Engineer Setup and Setup Feedback affect preparation information only, never a free pace bonus. No headline stars, constructor metadata, imagery or unverified calendar notes become simulation coefficients.

Attempt tuning is provisional: Safe has a lower ceiling/spread/error exposure, Push normal maximum effort, All-Out greater potential/spread/error exposure. No balance or universal-best claim is made; paired-seed evidence belongs to ticket 05.

The frontend worker inspected the current source asset folders and existing runtime manifests, visually reviewed the packaged Verstappen portrait and Monza track image, and reused existing `/portraits/{slug}-384.webp` and `/tracks/{slug}-960.webp` variants by backend source identity. Originals and their existing provenance manifests remain intact. Both driver and circuit views retain live text and missing-image fallbacks.

## Checks

The public simulation tests cover retry/iteration replay, a worked tied/no-time ordering, complete/duplicate/unknown choices, incompatible versions and malformed detailed Error Control. The malformed-input regression was observed red before saved-source validation made it green.

Six focused public API/SignalR tests use real Auth and Firestore emulators. They cover atomic start save failures before/after commit, complete-team rejection without mutation, private/public streams, the two-human barrier, canonical receipts, changed payloads, restart/nonresampling, revoked authority, lock-save failures, and saved pause/deadline/sweep behavior.

The focused browser flow runs at 360×800 with reduced motion and a 1920×1080 public display. It completes both human teams' preparation, starts Q1, chooses different approaches for teammates, aborts a submission, retries the identical command, refreshes the accepted lock, clears a shared tablet, resolves 22 exact results with 16 advancing and verifies the circuit fallback. It asserts no horizontal overflow and that the final public ranking row fits inside 1080px.

Commands use Node 24 from the cached Codex runtime, .NET SDK 10.0.401, cached Chromium and Java 21. Emulator configuration owns UI 4511, API 4611, Auth 19349, Firestore 19340, hub 19550, logging 19600 and Firestore websocket 9158. A bind preflight rejects any existing listener. The browser launches the API DLL built by the focused tests, avoiding a separate restore. The local Playwright configuration is `.tools/playwright-s08.config.cjs`; the committed test also runs through the existing shared Playwright configuration with the matching isolated environment variables.

`dotnet test ... --artifacts-path .tools/dotnet-s08 -p:NuGetAudit=false -p:BuildInParallel=false -m:1 --filter FullyQualifiedName~Game.Api.Tests.QualifyingTests -v:minimal` runs inside `firebase emulators:exec --only auth,firestore --project demo-f1game --config .tools/s08-emulators.json`. Simulation uses `.tools/dotnet-sim-s08` and filter `FullyQualifiedName~QualifyingTests`. Test invocations are bounded at 240s/60s; emulator/browser commands at 360s. VSTest's local testhost connection required sandbox escalation. Single-node MSBuild avoids a sandbox child-build failure. The existing SignalR 10.0.12 audit finding remains; `NuGetAudit=false` is command-scoped to behavioral tests and no package was changed.

Completed command/PID/exit records and inspected captures are recorded in `checks.json` and the reviewed PNGs below. Initial browser locator failures and the public-row layout correction were resolved before publishing. No broad suite was run by this ticket worker; integration owns the final broad pass. No hosted deployment, household comprehension or human four-to-five-minute duration measurement is claimed.

Reviewed captures: [phone choices](phone-two-car-choices.png), [recovered phone lock](phone-recovered-lock.png), [complete public Q1 results](public-q1-results.png), [public circuit fallback](public-q1-fallback.png). The initial public capture clipped the last row; the published result and fallback captures are the corrected versions with all 22 rows inside 1920×1080. Phone captures were reviewed at 360px width and intentionally scroll vertically.
