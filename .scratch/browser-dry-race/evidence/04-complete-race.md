# Complete dry race and event feedback

Reviewed 9 October 2026 against the real local HTTP/SignalR server in desktop Chromium. The six-lap open-equal race uses two authoritative windows: five leader laps followed by the final shortened phase. Driver 01 submits Push/Stay Out/Neutral first; Driver 02 requests Hard tyres. Continuation resets both visible drafts to Standard/Stay Out/Neutral for the last phase.

## Recorded behavior

The server publishes resolving, phase-result and ready-next, or finished at the last boundary. Only an authorized continuation opens the next window; identical retries return the original acknowledgement without advancing time. New opponent plans freeze from each team's legitimate starting observation. No display-completion message or client timer controls progress.

Each result uses the new suffix of cumulative engine events. Public events have opaque stable identities; their cursor is stable on refresh. At most 12 highlights are sent, with overtakes prioritized and chronological order restored. Exact allowlisted counts include omitted events. The first phase records one pit entry, one tyre change and one pit exit. The last phase records 20 of each, so its 12 highlights accompany a factual 60-event summary. Driver 02's earlier stop is not repeated as a new final-phase event. No diagnostic values, causal undercut claim or invented incident is sent.

Timed playback holds the previous published Classification under an explicit presenting label. It neither calls that table final nor reports a false quiet phase before the first highlight. Skip reconciles immediately; replay uses the same recorded highlights. Reduced motion reconciles immediately, including a quiet Standard/Stay Out phase. A refreshed result with no prior local table shows its current authoritative result. New decision windows show current phase readiness, not an initial grid claim. A delayed accepted-plan acknowledgement cannot relock a newer decision window.

The final Classification retains exact completed distance and eligibility. Driver 01 is first across the line but ineligible after using only Medium; Driver 02 is eligible after Medium and Hard. No strategy is corrected automatically. Both further submission and continuation are disabled. Same-preset restart creates fresh session links with the original hidden designer inputs; another-preset restart opens designer setup.

## Inspected captures

| Capture | Viewport | Actual image | Review |
| --- | --- | --- | --- |
| [Finished public display](04-finish-1920x1080.png) | 1920 × 1080 | 1920 × 1080 | All 22 rows fit, completed laps and eligibility remain legible, chronological highlights have a visible scroll cue. First across the line is a neutral label. |
| [Finished private controller](04-finish-controller-360.png) | 360 × 800 | 360 × 2077, full page | Both confirmed instructions and estimates readable, result eligibility explained, disabled primary action distinct, restart actions usable. |

No new imagery was mapped to synthetic identities. Existing readable foundation fallbacks preserve names and event facts; supplied original asset files remain unchanged.

## Verification

- Backend: 110 simulation and 15 API tests pass. API red/green coverage includes authorized/idempotent continuation, exact final short phase and eligibility, bounded new-event suffixes, stable opaque identity, counts and explicit event/presentation allowlists. Existing engine lapping/finish and replay tests remain green; coefficients and versions are unchanged.
- Real browser: 11 tests pass. Full race, held previous table, skip/replay, per-car defaults, ready-next, finished instructions, exact final fields, reduced motion, quiet feedback, restart and delayed prior-phase acknowledgement are exercised alongside all recovery checks.
- Foundation: 34 rendered gallery tests pass, including keyboard/focus, touch targets, stop availability, crowd scrolling, reduced motion and image failure.
- Angular typecheck and production build pass. Final bundle: 281.26 kB raw / estimated 72.42 kB transfer; portrait assets remain separate.

These are automated correctness and desktop layout checks. Human comprehension, workload and physical-device readability remained unproven at this point; ticket 05 records designer replay, metrics and playtest evidence. On 2026-10-09 the user retired the former fixed duration target; timing now informs pleasant pacing without a required duration.
