# Controller playtest follow-up — 9 October 2026

Status: resolved

Completed on 10 October 2026 on `codex/shared-sessions`, implementation `edd0e57`. The new frozen preview is [http://127.0.0.1:4460/race/new](http://127.0.0.1:4460/race/new). Earlier previews remain available and predate these changes.

## Approved flow and wording

- Primary action: **Approve Plan**. The modal confirms both cars' choices; accepting submits the complete plan atomically.
- After submission: **Waiting for other teams…** while required teams remain, then **Resolving play…**. Solo play need not manufacture a wait.
- During resolving presentation, hide decision/driver panels and show recorded events as they play out. Show the resolved outcome before automatically returning to decisions. No separate Continue action.
- Use **Decisions for the next X laps** instead of player-facing Phase terminology, including the shorter final window. Attack availability must derive from the updated positions and gaps.
- Server resolution and automatic next-window creation remain independent of presentation. A disconnected public display must not stall the server. Local playback can be skipped/reduced-motion; replay must not relock an already revealed decision window.

## Controller information

- Include driver portraits and the actual final player-card design/data presentation during testing, but label identities Driver 1, Driver 2, etc., to make changes easy to follow.
- Map presentation identities explicitly; preserve synthetic preset personnel/performance. Do not silently substitute real-driver ratings for the existing simulation fixture or convert headline stars into physics.
- Show a subset of public Classification centered on each controlled driver: the driver plus two above and two below where available. Include position, displayed driver name and interval, with row drill-down into driver cards for analysis. Handle field edges, lapped gaps and unavailable intervals explicitly.
- Show the last several lap times for each controlled car to convey current pace. Use recorded timing; no invented values. Choose and document a bounded recent-history length.
- Track the fastest lap and show its marker, with a documented tie rule and recorded lap timing rather than approximate gap calculations.

## Public and shared presentation

- Public Classification should show pit stops taken. Define a completed-stop count consistently with recorded pit events; avoid double counting entry/exit or retry/replay.
- Tyre selection should visually use the compound card/badge treatment, e.g. **M — Medium**, consistently with the existing design system. Preserve keyboard, focus and accessible selection behavior.
- Use existing driver assets and established card components, with readable missing-image fallbacks. Keep original assets and provenance.

## Verification and integration

Use the approved public API/SignalR and real-browser boundaries. Add regression coverage for outcome-first decisions, fresh battle options, the short final lap window, recorded lap/pit/fastest-lap projection, classification subset/drill-down, and confirmation/retry behavior. Preserve public/private filtering and exact engine replay. Inspect phone and public-display layouts.

Shared ticket 01 was merged as `d02c93b`. The shared parent remains incomplete; ticket 02 has not started. It must use the approved waiting/resolving and automatic outcome-first controller flow. The user requested stopping after this controller follow-up.

## Answer

Implemented the approved controller presentation and race-information changes. Verified 140 backend tests, 21 live browser scenarios, 34 gallery scenarios and two stop-boundary browser scenarios, plus frontend typecheck/build. Final Standards and Spec re-reviews each report zero actionable findings; the keyboard-focus finding is resolved. The frozen 4460 preview independently passed a real-browser check of portraits, outcome-first decisions, recent timing, Stops, fastest-lap marker and finish.

See [verification and captures](evidence/controller-followup.md) and [timing, card mapping and limitations](../../docs/controller-playtest.md). Production build retains the documented bundle/CommonJS warnings. No further ticket was started.
