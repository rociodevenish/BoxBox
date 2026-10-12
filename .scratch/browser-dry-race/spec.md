# S04 — Browser dry-race vertical slice

Status: resolved
Blocked by: segment-model evidence review and integration readiness; visual foundation components for final UI integration.

## Problem Statement

The laboratory demonstrates dry-race strategy, but players cannot yet make decisions in a browser or see a shared display explain their consequences. We therefore do not know whether managing two cars is understandable, whether phase feedback is useful, or whether its timing feels pleasant for players.

The first playable integration needs to expose the proven race model through the intended authoritative architecture without introducing the season, multiplayer lobby or new race mechanics.

## Solution

Deliver a locally runnable dry race with one human-controlled team, two cars and ten scripted opponent teams. An Angular controller submits simultaneous per-driver choices to an ASP.NET Core session server. SignalR supplies a separate public display and legitimate team observations. A short presentation sequence explains important events before displaying the exact next Classification.

Prove one complete Race Phase first, then the same loop through a full 22-car race from the laboratory's fixed rolling-order start. Use the visual foundation and collect evidence about decision clarity, two-car workload and race pacing.

## User Stories

1. As a playtester, I want one documented local start procedure, so that I can run the browser race without cloud services.
2. As a playtester, I want a new race with a known preset, so that observations can be reproduced.
3. As a playtester, I want contrasting segmented track presets, so that different strategic situations can be tested.
4. As a player, I want to control both cars of one team, so that the slice represents the intended team experience.
5. As a player, I want ten opponent teams following fixed scripts, so that I have a complete field without recruiting other players.
6. As a viewer, I want a separate public display, so that the race can be watched alongside the controller.
7. As a player, I want accurate Classification and gap information, so that I can reason about track position.
8. As a player, I want lap deficits and final completed distances shown correctly, so that lapped results are understandable.
9. As a player, I want compound, tyre age and stop status, so that I know each car's current situation.
10. As a team controller, I want my legitimate tyre estimates, so that I can decide without exact hidden condition.
11. As a team controller, I want Conserve, Standard and Push per driver, so that I can choose pace independently for my two cars.
12. As a team controller, I want Stay Out or Pit plus compound per driver, so that my tyre plan is explicit.
13. As a team controller, I want the timing of a Pit Instruction explained, so that I understand it takes effect at the driver's next lap-end.
14. As a team controller, I want pending or ongoing stops indicated, so that I do not attempt an incompatible replacement.
15. As a team controller, I want only legal Battle Instructions offered, so that I do not see irrelevant tactical choices.
16. As a team controller, I want short explanations of pace and battle trade-offs, so that I can learn the decisions without a recommendation engine.
17. As a team controller, I want both cars' choices summarised before locking, so that I can catch mistakes.
18. As a team controller, I want one atomic team submission, so that both cars' instructions belong to the same Race Phase.
19. As a team controller, I want a server-confirmed lock, so that I know when my decisions are committed.
20. As a player, I want opponents' current choices hidden until resolution, so that simultaneous play is meaningful.
21. As a player, I want a resolving state, so that I understand why further decisions are temporarily unavailable.
22. As a viewer, I want a brief ordered sequence of important race events, so that I can follow overtakes and pit stops.
23. As a viewer, I want the exact phase-end state after presentation, so that animation cannot change race results.
24. As a player, I want my reviewed team plan to run the phase and automatically open the next choices, so that a separate continuation step does not interrupt play.
25. As a player, I want rejected commands explained without losing editable choices, so that I can correct them.
26. As a player, I want a repeated submission to avoid advancing the race twice, so that a double click or retry is harmless.
27. As a player, I want a refreshed browser to show the current session state, so that I can recover while the local server remains running.
28. As a player, I want a disconnected controller to show unavailable actions honestly, so that it does not claim to have submitted a decision.
29. As a player, I want a clear finished Classification and tyre-rule eligibility, so that I understand the result.
30. As a playtester, I want to restart with the same or another preset, so that I can compare experiences.
31. As a designer, I want decision, resolution and presentation durations recorded separately, so that I can identify pacing problems.
32. As a designer, I want a reproducible playtest record of inputs and decisions, so that reported race outcomes can be replayed in the laboratory.
33. As a designer, I want player comprehension and workload findings recorded, so that the next milestone addresses real problems.
34. As a developer, I want the browser and server to reuse the existing engine, so that there is one source of race truth.
35. As a developer, I want public and private projections checked at the wire boundary, so that diagnostic data cannot leak through realtime messages.

## Implementation Decisions

- Add an ASP.NET Core Game.Api and a small application-level session coordinator around Game.Domain and Game.Simulation. Reuse RaceEngine initialization, ResolvePhase, GetState and GetTeamObservation, plus existing fixtures and strategy evaluation where suitable. Keep networking, session state and presentation projection outside the simulation.
- Use the game Angular workspace and components from the visual foundation. Provide separate display and controller routes; no Babylon.js renderer is required. A shared browser can open both views for local testing, and another browser/device may connect to the local server with documented development networking settings.
- Support one active in-memory playtest session initially. Session creation accepts allowlisted presets, race distance and a designer seed; return a session identifier, a public display URL and a controller URL scoped to the one human team. Do not expose arbitrary configuration upload in the player UI.
- Keep player/team/device concepts separate even in this minimal slice. Use a server-issued capability scoped to the controlled team for private observations and commands; possession of a session identifier or display link alone grants neither. Keep credentials out of public messages and avoid returning them through public snapshots. Cloud authentication and lobby ownership are deferred.
- Start with the laboratory's 11-team/22-car fixtures, fixed rolling order, explicit gaps and fresh tyres. Prefer reviewed segmented presets from the segment milestone; retain equal and mixed fields on contrasting tracks. Do not advertise a standing start or qualifying as implemented.
- Opponents use fixed, versioned scheduled/conditional strategies operating on legitimate observations. Freeze their Race Phase choices at the phase's starting snapshot and keep them private until resolution. A human team lock completes the simultaneous decision set; there is no opponent timer or autonomous strategic AI in this slice.
- Provide explicit per-car Pace, Pit and Battle choices, defaulting visibly to Standard, Stay Out and Neutral for a new phase. Confirm both cars together. Offer Battle Instructions from current engine availability, suppress incompatible pit changes, and explain pending stops. Defaults must not silently correct malformed input or mandatory-compound eligibility.
- Server validation checks session, capability, phase/revision, driver ownership, complete two-car submission and engine legality before committing. Reject the entire submission on any invalid car choice, preserving the phase state. Opponent choices cannot be supplied by the controller.
- Commands carry a stable command identifier and expected phase/revision. Serialize mutations per session. A duplicate accepted command returns its recorded acknowledgement/result without resolving again; stale or conflicting submissions are rejected. Expose lock/resolution state authoritatively, not through optimistic client claims.
- Enforce awaiting decisions, resolving, phase result and finished states. Following the user's 2026-10-09 feedback, accepting both cars runs the phase and automatically opens the next decision window once. Retain the last bounded presentation with its resolved-phase identity while the current phase advances. Controllers present the resolved outcome before automatically revealing the next choices, using updated positions and gaps. Player-facing headings say "Decisions for the next X laps", including a short final window, rather than "Phase"; final finish remains terminal. The display presentation can finish, skip or be replayed without changing race state; a disconnected display cannot block progress. No player deadline is introduced yet.
- Reuse five-leader-lap Race Phase boundaries and existing pit, pace, lapping, finish and two-compound eligibility semantics. Preserve engine/model/randomness versions. Do not change race coefficients or passing physics to compensate for UI shortcomings.
- Define explicit public snapshot, team observation, submission acknowledgement and phase-presentation contracts. Include session identity, phase/revision and lifecycle state so clients can ignore duplicated/older updates. Initial loading and reconnect fetch a current authorised snapshot; SignalR supplies subsequent state notifications to separate public and private groups.
- Never serialize raw PhaseResult as a player payload. Its tyres and events contain diagnostic condition values, and other histories may contain hidden inputs. Project public facts and allowlisted presentation events explicitly; project own-team estimates from the engine's observation contract. Exclude exact hidden condition, race form, seed, random draws, raw diagnostic histories and future/opponent locked decisions from both public and private player payloads.
- Show authoritative Classification, exact gap to leader and available ahead-gap information, with explicit lap deficits and units. If an exact same-lap gap-to-ahead cannot be obtained from current classification semantics, extend the observation/projection using recorded timing evidence rather than converting track progress to time or fabricating precision. Do not subtract incompatible lap-gap records.
- Make Tyre Compound, age and public pit state visible; estimates belong to the controlled team's private view. The public display shows readiness/lifecycle rather than private estimates or current unexecuted selections.
- Build a bounded ordered presentation timeline from newly resolved events only. Use stable event identities/cursors to avoid replaying cumulative history as new events. Prioritise overtakes, pit entry/exit and tyre changes; summarise crowded phases and report quiet phases plainly. Do not send lap-by-lap diagnostic noise to the TV.
- Explanations must be supported by recorded events and observations. A pit gain or overtake is evidence; causal claims about a successful undercut require supporting timing evidence. Do not invent incidents, recommendations or counterfactual outcomes.
- The server resolves race time independently of client animation. Use short configurable presentation duration, skip/reduced-motion support and exact final-state reconciliation. Display playback never calculates physics or outcomes and is not an authoritative animation clock.
- Browser refresh/reconnect can recover the active session while the server process remains running, including current lock/result state. Server restart loses the session and shows a clear expired/unavailable state. Durable persistence, host migration and unattended race continuation belong to later work.
- Final results preserve eligibility and completed distance; explain compound requirements before decisions and mark ineligible results without silently altering strategies. Disable further phase submissions after finish and allow an explicit new playtest session.
- Record designer-only replay inputs with seed, configuration, model/randomness versions and accepted human/opponent decisions. Keep diagnostics separately accessible in local development and out of player payloads. Record decision time, server resolution time, presentation time and total race time separately.
- Supply a documented run procedure and playtest protocol. Include full-distance tests on contrasting tracks, both-car workload, a short race smoke test and a reproducible report with actual findings. Timing is informative: the user retired the fixed duration target on 2026-10-09. Automated clicks and fast server benchmarks do not establish pleasant pacing.

## Testing Decisions

- User-confirmed highest main testing boundary: the public session API and SignalR flow through an in-process server, exercising accepted commands and observed messages. Add a small browser end-to-end suite for the controller/display loop.
- Reuse existing xUnit race, tyre, pit, battle, strategy, replay, comparison and segmented tests for engine rules. Do not duplicate private simulation formulas in API tests or create an alternate test engine.
- Exercise complete team submission, unavailable battles, incompatible pit changes, wrong-team commands, stale revisions, duplicate/concurrent submissions and finished-session rejection. Assert visible outcomes and that rejected submissions do not advance or partially lock the session.
- Test public and private HTTP/SignalR payloads with fixtures containing diagnostic values and hidden fields. Verify public subscribers cannot join the team channel or invoke its commands without the capability; own-team observations include legitimate estimates and no hidden exact values. Check all event projections as well as snapshots.
- Verify opponent decisions use the starting phase snapshot, remain hidden before resolution and combine with the accepted human choices. Replay the recorded complete decision set directly through the existing engine and compare authoritative phase/final results.
- Verify revision/event ordering, repeated notifications, fresh subscription and browser refresh during decision/result states. Assert that recovery within the server lifetime neither resolves twice nor lets an older acknowledgement relock a newer decision window. Final confirmed choices remain locked.
- Use a browser test with real server/SignalR to submit different instructions for the two cars, observe an event sequence and reconcile the display to the next snapshot. Include a short full-race finish and eligibility result. Use deterministic presets; avoid luck-dependent overtake expectations.
- Reuse visual-foundation gallery checks for component states. Add integration checks for server-driven battle availability, pit state, disabled resolving controls, errors and finished results. Inspect representative phone and shared-display layouts with real session data.
- Existing engine events and replay tests provide backend prior art; the recovered card prototype provides presentation prior art. There is no established application API/SignalR or production browser test harness, so create these at the session boundary rather than exposing new physics seams.
- Record manual player feedback and measured pacing separately from automated correctness. Passing tests does not establish enjoyment, comprehension or actual player workload.

## Out of Scope

- Multiple human teams, public lobby, team claiming, pass-and-play handoffs and full open-family privacy modes.
- Durable persistence, cloud deployment, Firebase authentication, scheduled play, decision deadlines, AI takeover and recommendations.
- Qualifying, standing-start simulation, setup, weather, incidents, reliability, Safety Cars and new race mechanics.
- Personnel acquisition, final card attributes/prices, car construction, budgets, development and campaigns.
- Full calendar assets, Babylon.js race board, continuous car animation and final graphical polish.
- Production multi-session capacity, large-scale multiplayer and final race/season balancing.

## Data and asset references

Review these repository inputs before implementing this spec:
- [f1_strategy_driver_cards_2026.json](../../.docs/data/f1_strategy_driver_cards_2026.json) - driver identities, headline/detailed ratings and compatibility preferences.
- [f1_strategy_track_cards_2026.json](../../.docs/data/f1_strategy_track_cards_2026.json) - circuit identities, event-to-track calendar mapping, demands and race characteristics.
- [f1_strategy_car_attribute_spec_2026.json](../../.docs/data/f1_strategy_car_attribute_spec_2026.json) - car attribute vocabulary, scales, headline derivation, setup and development/package domains.

Use [driver portraits](../../assets/driver%20images/) and [track imagery](../../assets/track%20images/) wherever this feature presents those identities. Inspect the current [assets collection](../../assets/) at implementation time, including later additions; map files through stable content identities, record provenance, preserve originals and package suitable runtime variants. Keep readable text and missing-image fallbacks. Simulation-only work uses imagery only for associated reports or galleries.

The JSON files are provisional design inputs. Preserve schema/data versions, validate references, units and bounds, and document mappings to supported runtime attributes; do not silently convert headline stars into simulation values. Data availability does not expand the mechanics in this spec. Keep active campaign/race inputs stable when files or assets change.

Map reviewed records into allowlisted dry-race presets and card view models; preserve reproducible fixtures and supported engine attributes.

## Further Notes

This is Stage 2 of the master roadmap. The visual foundation can proceed using fixtures, and API orchestration can be developed against stable engine contracts, while the segment milestone finishes. The completed browser slice must use reviewed simulation evidence and the selected visual foundation.

Implement and verify the one-phase path before extending it to a full race. Completion requires a locally playable complete race, reproducible checks and a playtest findings report. Where timing or clarity needs improvement, record the measured limitation and the next corrective work.

## Comments

The user confirmed the proposed API/SignalR and browser end-to-end testing boundaries. The remaining blockers are implementation dependencies.


## Completion evidence

All five implementation tickets are resolved on codex/browser-dry-race. Actual player feedback corrected dropdown carets, Gap/Interval names and controller return navigation, then removed the separate Continue step: accepting both cars runs the phase and automatically opens the next decision window while feedback remains independent. Final Standards and Spec re-reviews each report zero actionable findings; the pending-stop coverage finding is resolved by real-engine API/SignalR and browser fixtures.

Validation: 132 backend tests, 34 gallery tests, 19 ordinary browser tests plus two stop-boundary browser tests, Angular typecheck/build, and exact ten-phase laboratory replay. Five 50-lap runs include equal/mixed contrasting presets and lapped eligible/ineligible finishes. Actual physical-device readability and sustained human workload remain explicitly unmeasured; no fixed race-duration target remains. See [review and evidence](review.md), [playtest protocol](../../docs/browser-playtest.md), and [automatic-flow measurements](evidence/review-automatic-windows.json).
