# S13 review remediation

Base: integration `818ad34e362314c1cd015e6bd1af507bc6f39ff6`. One implementer fixed both P3 findings and the intermittent recovery-fixture failure relayed from the root's broad verification. Public testing boundaries remain those approved in the parent spec. Actual human choice comprehension remains **UNMEASURED**; ticket 04 remains ready for human review.

## Changes

- Preview compatibility now calls public `CarConstruction.MechanicalAxis`, removing its duplicate selection/handling calculation. The arithmetic, rounding, mapping and preview versions are unchanged.
- Reopened personnel labels the `base-car-construction` ledger reason as **Car construction**. Its completed-roster sentence uses the same `baseCarReady(teamId)` state as the existing top-of-panel paid-car explanation, retaining protected-reserve wording before purchase. No extra API call or async/private-state change was introduced.
- The confirmation browser flow now reopens personnel after all-team construction handoff, verifies the 18-credit debit appears exactly once, seven personnel debits remain, zero credits remain, and the completed roster describes the paid car without claiming an unspent minimum reserve.
- The construction confirmation recovery fixture explicitly selects manual wakeups for all its servers. Its existing single-weekend `ConfirmationFailure.ListSessions` scope is retained. This prevents background delivery from consuming a deliberately injected persistence failure before the test's explicit sweep observes it. Production wakeup behavior, test parallelism, failure assertion and restart/debit checks remain unchanged.

## Red/green and diagnosis

The first browser attempt placed the reopen action during `constructing`, when personnel controls are intentionally absent. It was canceled; its unfinished ignored report (`browser-red.json`, parent PID 33616, started 2026-10-27T20:05:42.982Z) is not a completed result. Owned ports were checked clear before retry. The corrected flow reaches `created` before reopening. Its semantic red failed exactly because `-18 credits · Car construction` had zero matches, then passed after the copy fix. Dependency junction creation initially returned Access denied under the sandbox; the narrowly escalated creation succeeded.

The root's single broad pass found one intermittent recovery failure (`committed:true,handoff:false`, original line 99) among 224 API checks, while 239 simulation checks passed. This worker did not repeat broad verification. The isolated four-case baseline passed, indicating timing sensitivity. Ranked hypotheses were background delivery consuming the failure, qualifying recovery pausing progression, and cross-fixture sweeps. The existing Target-only session enumeration ruled out cross-fixture manual sweeps. A temporary 300ms delay between clock advance and explicit sweep allowed the background timer one tick: both `handoff:false` variants then failed at the original injection assertion. With manual wakeups and the identical timing probe, all four passed. The delay was removed before final verification; no timing probe or debug instrumentation remains in the test.

| Check | Outcome | Bounded time | Launched PID | Record |
| --- | --- | ---: | ---: | --- |
| Browser semantic red | Expected failure, missing construction debit label | 37.930s | 37208 | [record](reports/browser-red-semantic.json) |
| Browser copy green, existing two flows plus new assertions | 2 passed | 44.335s | 19760 | [record](reports/browser-green.json) |
| Public construction/domain/preset regressions | 17 passed | 5.486s | 30736 | [record](reports/domain.json) |
| Fresh Release ConstructionEvidence build | Passed, zero warnings/errors | 2.421s | 37812 | [record](reports/evidence-build.json) |
| Frozen archive exact replay | 1,300 public race outcomes passed | 74.547s | 35516 | [record](reports/replay.json) |
| Angular template/typecheck | Passed | 2.799s | 30296 | [record](reports/typecheck.json) |
| Focused API unmodified scheduling baseline | 4 passed, 33s test duration | 49.631s | 23768 | [emulator wrapper](reports/api-red.json), [test record](reports/api-tests.json), [results](reports/api-baseline-results.json) |
| API background-tick probe | Expected 2 failed, 2 passed | 50.205s | 21588 | [emulator wrapper](reports/api-probe.json), [test record](reports/api-probe-tests.json), [results](reports/api-probe-red-results.json) |

| API identical timing probe with manual wakeups | 4 passed, 36s test duration | 47.268s | 8152 | [wrapper](reports/api-manual-probe.json), [tests](reports/api-manual-probe-tests.json), [results](reports/api-manual-probe-results.json) |
| Final construction API, no probe or delay | 8 passed, 34s test duration | 46.147s | 13736 | [wrapper](reports/api-final.json), [tests](reports/api-final-tests.json), [results](reports/api-final-results.json) |
| Final browser, explicit zero balance and both flows | 2 passed (14.7s confirmation, 8.7s drafting) | 40.608s | 9312 | [record](reports/browser-final.json) |

The replay reads the original compact archive, not current catalogue files, and reconstructs public race outcomes from frozen source snapshots. Archive SHA256 remains `BE0A5D69FB5468923AE785EE5F0A4E2169C7DCB907635B8845EFDEF4C1609782`, matching the previously published hash. No new experiments, coefficient changes or balance claims were made.

## Viewport review

Inspected actual **360×800** [reopened personnel ledger](s13-remediation-personnel-360.png): all eight debit rows and starting credit are readable, the single `-18 credits · Car construction` row is correctly named, and wrapped provenance text stays inside the panel. The screenshot scrolls to the ledger; paid-car roster and zero-balance behavior are asserted through live public DOM, not claimed to fit in this same viewport.

Inspected actual **1920×1080** [public campaign handoff](s13-02-handoff-1920.png): Campaign created, all 11 construction sources and both supplied circuit cards are legible. Private personnel was closed before capture. No horizontal clipping or overlap was observed. This is engineering readability evidence, not measured player comprehension.

## Isolation and replay

Node 24.22.1; .NET 10.0.401; Java 21.0.12.1; pinned cached dependencies and Chromium. Worktree `.tools/worktrees/s13-review-fixes`; UI 4701, API 4801; owned Auth 22909, Firestore 22900, WebSocket 22950, hub 22910 and logging 22960. The [owned emulator config](reports/firebase.remediation.json) is published alongside its [rules](reports/firestore.rules). Runtime scripts used a same-content temporary config beside the original rules. Dotnet browser/domain/API/evidence artifacts have separate ignored subdirectories; API and browser stacks ran sequentially on their owned ports. No root preview/default ports were touched.

All completed runs use the bounded wrapper, with exact commands, UTC timestamps, PIDs, elapsed milliseconds and exit status in the linked records. `NuGetAudit=false` is command scoped for the test/build invocations. The [browser replay script](reports/run-browser.ps1) records the runtime environment; its worktree-specific paths must be adjusted if replayed elsewhere. Historical evidence files were not replaced. Only explicitly named reviewed captures and records were published here.


Final owned listener check found no listeners on UI 4701, API 4801, Auth 22909 or Firestore 22900. The fixture timing probe is absent, and `git diff --check` passes. Integration tip remained `818ad34e362314c1cd015e6bd1af507bc6f39ff6` before the final commit. Test result summaries retain exact cases, durations, timestamps and failures; raw verbose TRX remains ignored under the worktree reports.
