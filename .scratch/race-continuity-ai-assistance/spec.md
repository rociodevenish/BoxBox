# Race continuity, AI and assistance

Status: ready-for-agent
Blocked by: shared sessions and controller ownership; browser dry-race contracts and deterministic replay; reviewed race-model evidence.

## Problem Statement

Shared play currently waits indefinitely for missing decisions and keeps authoritative state only in the running server. A browser or server interruption can disrupt a family race, while scripted opponents do not react sensibly to evolving conditions. Less experienced players also lack useful advice.

The household needs races that can resume safely, continue predictably through missed decisions and offer fair help without changing simulation performance or revealing hidden information.

## Solution

Persist authoritative session transitions, add reconnect and server-restart recovery, introduce explicit server deadlines and controlled AI fallback/takeover, and provide per-player Standard, Assisted and Guided decision presentation. Host the resulting dry-race family playtest using the locked Firebase/Cloud Run architecture.

AI and recommendations use the same legal decisions and legitimate team observations as humans. Store enough replay information to recover the exact race, and make continuation behaviour visible to players.

## User Stories

1. As a host, I want an active race saved at meaningful transitions, so that it survives a server restart.
2. As a player, I want to reconnect to the current authoritative phase, so that stale screens cannot change the race.
3. As a player, I want accepted decisions to survive a disconnect, so that I do not need to submit them again.
4. As a viewer, I want to replay or skip a missed presentation, so that losing the TV connection does not lose the race.
5. As a host, I want to pause and resume a saved session, so that the family can return later.
6. As a host, I want a configurable decision time allowance, so that live sessions keep moving.
7. As a player, I want a server-based countdown, so that different device clocks do not give different deadlines.
8. As a player, I want the timeout policy shown in advance, so that I know what happens if I miss a decision.
9. As a host, I want shared-device timing to allow practical handoffs, so that tablet users are not penalised for sharing hardware.
10. As a player, I want timely accepted choices preserved at timeout, so that fallback does not replace my lock.
11. As an assisted player, I want optional automatic acceptance of the recommendation on expiry, so that I can rely on a known fallback.
12. As a host, I want AI to fill unclaimed teams, so that the race has a complete field.
13. As a player, I want AI to respond to tyres, traffic and pit opportunities, so that opponents make credible choices.
14. As a host, I want temporary AI control for an unavailable player, so that one absence does not prevent racing.
15. As a player, I want to return to my team at a clear phase boundary, so that takeover cannot overwrite already committed decisions.
16. As a host, I want explicit permanent AI takeover, so that an abandoned team can finish the race.
17. As a player, I want AI control indicated publicly, so that responsibility for a team's decisions is understandable.
18. As a Standard player, I want all legal choices without recommendations, so that I can plan independently.
19. As an Assisted player, I want a sensible recommendation with an explanation, so that I can learn while retaining all choices.
20. As a Guided player, I want a smaller set of sensible options where appropriate, so that decisions feel manageable.
21. As a player, I want help configured independently of other players, so that different experience levels can play together.
22. As a player, I want assistance to leave my ratings and results unchanged, so that help carries no championship or budget penalty.
23. As a player, I want tyre advice to acknowledge uncertainty, so that I do not mistake an estimate for exact knowledge.
24. As a player, I want recommendations to respect ongoing stops and legal battles, so that advice is executable.
25. As a player, I want help to use my team's information only, so that advice does not cheat against opponents.
26. As a host, I want progress through presentation even if a screen is disconnected, so that missing acknowledgements cannot stall play.
27. As a player, I want a recovered timeout or retry to avoid duplicate resolution, so that the race remains fair.
28. As a host, I want storage failures to stop progression clearly, so that the server cannot publish an unsaved race as durable.
29. As a designer, I want AI and assistance versions recorded with decisions, so that race stories can be reproduced.
30. As a designer, I want AI strength measured against representative strategies, so that midfield competitiveness is based on evidence.
31. As a household, I want a hosted join-and-play session, so that running a developer machine is unnecessary.
32. As a host, I want bounded hosting costs and clear operational controls, so that family use is sustainable.
33. As a playtester, I want interruption, timeout and mixed-assistance scenarios tested, so that dependable play is demonstrated.

## Implementation Decisions

- Extend the existing application session coordinator, identity/device assignments, Angular surfaces and SignalR projections. Keep persistence, deadline scheduling and control policy outside Game.Simulation. Domain types must not depend on Firestore or cloud SDKs.
- Use Firestore through a persistence adapter for session metadata, assignments, settings, accepted team locks, phase outcomes, important events and recovery records. Firebase Authentication supplies stable account/guest identities; restoring a race does not restore authority to an unauthorised device.
- Persist authoritative transitions rather than animation frames. A team lock acknowledgement means its validated choices are durably recorded. Before resolution, durably record the complete immutable phase decision set, including automatic choices and their source. Commit the resulting state/recovery record and presentation payload before announcing the new phase result.
- Persist command identities, expected revisions, control revisions and recovery/schema/model/randomness versions. Use conditional revision writes or transactions to prevent duplicate ownership of transitions. Bound stored record/document size by splitting phase/history records as appropriate; do not assume a complete race fits in one document.
- Recover the engine through its existing deterministic public race contract: initialise from saved configuration, immutable driver inputs and seed, then replay committed phase decisions. Verify recovered results against recorded outcomes. No private engine-field serializer or second physics implementation is required. Measure replay latency and retain a replaceable persistence boundary for later checkpoint optimisation.
- If resolution crashes after the decision set is committed but before the outcome is saved, recovery resolves that immutable set once logically and commits the same deterministic outcome. If a committed outcome exists, recover/rebroadcast its revision without generating a second outcome. Clients tolerate at-least-once notifications using phase/event identities.
- If persistence fails, do not acknowledge a durable lock or publish an uncommitted result. Show a recoverable paused/error state. Preserve accepted choices and retry the same revision; do not recalculate automatic choices under a newer policy version.
- Do not silently reinterpret races created under incompatible model/schema versions. Support explicit compatible recovery or report an actionable version limitation. Preserve the saved result and inputs. A deployment may not discard an active race as an implicit migration strategy.
- Add configured decision duration and a host pause/resume action. The server owns deadline instants; clients display them with estimated server-clock offset. Freeze remaining duration on pause and save it. Resuming creates a new deadline from the remaining duration; countdown animation is never authoritative.
- For a shared controller, configure an explicit per-phase handoff allowance based on assigned human teams and publish the resulting phase deadline before choices begin. Use one documented household timing policy and measure it in playtests; opening/refreshing a page must not extend deadlines or grant additional choices.
- Serialize submissions and deadline closure under the session transition/revision. Accept only a complete valid team lock processed before the deadline according to server time. At expiry, preserve accepted locks and fill missing teams using the saved fallback policy. Late commands are rejected with the current state. Test exact-boundary handling with a controllable server clock.
- Support per-player auto-accept-recommendation as an explicit option. Otherwise a missing human team's choices use temporary AI fallback. Standard mode must not expose recommendations just because fallback exists. Disclose fallback settings before play; an invalid client payload is not silently treated as a valid submission.
- Temporary fallback affects a defined phase only; the player can act in the next open window. Host-requested temporary or permanent AI control takes effect at the next uncommitted phase boundary. A human reclaim is authorised and boundary-scoped; never replace an accepted lock. Preserve team/player identity and disclose control status separately from membership.
- Replace scripted fill-ins with bounded rules-based AI that evaluates the same legitimate team observation and legal instruction set as humans. It selects both cars' pace, pit compound and contextual battle instructions, considering tyre estimates, public race distance/order/gaps, compound use and pending stops. No privileged hidden condition, form, opponent locked decisions, future draws or simulated future incidents are inputs.
- Produce automatic decisions from the phase-start observation and freeze them before human choices can influence them. Record policy version and concrete instructions. Repeated requests or recovery reuse the saved choices. Deterministic tie-breaking is preferred; any policy randomness must be explicitly versioned and independent of hidden race draws.
- Target roughly midfield AI against engaged human play, using evidence on representative fields and contrasting circuits rather than a promised finishing position. AI receives no free pace, tyre, information, budget or execution bonuses. Do not optimise it into a perfect solver.
- Build assistance as an observation-driven decision policy with structured recommendations and plain-language reasons. It may share observation-only evaluation with AI, but help level controls presentation, not race performance. Advice can cite estimated tyre life, visible gaps and legal stop/battle choices; it cannot claim knowledge beyond those inputs.
- Standard shows all legal choices without strategic recommendations. Assisted shows all legal choices, highlights the suggested two-car plan and explains it. Guided presents recommendations and may reduce a choice group to two sensible alternatives when supported by the policy; keep all legal choices when confidence does not justify pruning. Forced one-option legal states remain possible and should be explained.
- Assistance settings are per player and inherited by their team controller views, including shared devices. The host sets them at setup and may change them between phases. No championship or budget penalty applies. Personnel quality is preset for this milestone; do not implement purchased engineer effects or arbitrary bonuses.
- Show recommendation freshness, current phase and uncertainty. Repeated reads within a phase do not resample observations or change suggestions. Recommendations and private explanations travel only through authorised team channels, except intentionally open teams in Family/Mixed mode.
- Use a bounded server-owned post-result presentation window in live mode, followed by the next decision window unless the host pauses. The host may continue/skip early. Display playback acknowledgement is not required. Persist progression timing and process overdue transitions on recovery without creating a burst of multiple unattended phases: close an already expired decision window once, then pause recovered progression for host resume before opening further windows.
- Cloud Run may stop while idle, so timers must not rely on a browser tab or in-process task remaining alive. Supply a recoverable scheduled-wakeup mechanism for due active-session transitions with idempotent delivery and authenticated callbacks. Verify its family-scale costs and behaviour when implementing; keep live SignalR as the race transport, not Firestore listeners.
- Deploy the Angular frontend to Firebase Hosting, the ASP.NET Core/SignalR backend to Cloud Run, and Firestore/Auth for persistence and identity. Start with min instances zero and max instances one per the architecture. Use emulators locally and separate environments. Keep credentials server-side, validate origins and authenticated channel membership, and document deployment, rollback and recovery procedures.
- Configure cost protection, bounded diagnostic retention and useful transition/error logging without private credentials or hidden data in player-visible logs. Measure real storage, request, scheduling, reconnect and runtime behaviour. Treat approximately zero monthly family cost as a goal to verify, not a guarantee.
- Deliver a hosted family playtest and an evidence report covering downtime, retries, missing players, shared-device timing, assistance clarity and AI behaviour. Record resolution, decision, presentation and recovery latency separately, and retain designer replay diagnostics apart from player payloads.

## Testing Decisions

- Reuse the user-approved public session API/SignalR testing boundary and focused browser end-to-end tests. Exercise recovery with real persistence behaviour through the local Firestore/Auth emulators and restart the application against retained data. A controllable clock and scheduled-delivery adapter provide deterministic environment inputs, not a new domain testing seam.
- Prior art is the browser-slice replay/payload/idempotency coverage, shared-session decision-barrier and ownership tests, existing scoped-randomness/observation tests, and the visual gallery. Extend them with durable and automatic transitions rather than testing private timer queues or Firestore layout strings.
- Interrupt after accepted team locks, after the immutable complete decision set, during resolution, after commit/before broadcast and during presentation. Resume through public requests and assert the same locked decisions, Classification and result revision as uninterrupted play.
- Inject failed writes and duplicate scheduled callbacks; assert no false durable acknowledgement, no uncommitted broadcast and no double resolution. Verify retry command identities survive restart, incompatible versions fail clearly, and lost notifications are recoverable through snapshots.
- Test deadline races, before/at/after boundary commands, shared-device allowance, pause/resume, expired-window recovery and missing teams. Use simulated server time, not wall-clock sleeps. Recovered progression closes at most the pending expired window and requires host resume before further windows.
- Verify temporary fallback, permanent takeover and authorised reclaim at boundaries. Accepted human decisions are never overwritten. Revoked devices cannot act or receive private updates after recovery.
- Test AI and assistance through public observations and displayed recommendations/committed outcomes. Change hidden diagnostic values and opponents' locked choices while holding legitimate observations fixed; the policy output must remain unchanged. Repeat observations, reconnect and recovery without rerolling advice or automatic choices.
- Cover compound eligibility planning, pending stops, contextual battle legality, Standard/Assisted/Guided presentation and auto-accept settings. Recommendations must be executable and explanations must not make unsupported certainty claims. Use deterministic fixtures for correctness, and multi-seed runs for strength evidence.
- Run short and full-distance races with AI-filled teams on contrasting tracks, comparing representative human/scripted strategies across published seeds. Report uncertainty and failed/inconclusive hypotheses; do not assert tight statistical finishing thresholds in unit tests.
- Browser tests cover a disconnect/rejoin with a committed lock, a shared-device timeout, Guided choices and recovery to the current phase. Verify public/private filtering for persisted and newly broadcast data as well as initial snapshots.
- Run an actual hosted smoke/playtest for join, SignalR reconnect, scheduled wakeup after idle, server restart and final result persistence. Record measured family cost/latency limits and operational findings rather than inferring them from emulator or local benchmark success.

## Out of Scope

- Season persistence and standings, asynchronous development, preseason economy and personnel-driven assistance quality.
- New simulation mechanics, qualifying, standing starts, Safety Cars, weather and final numerical balance.
- Perfect strategy search, learning AI, AI difficulty bonuses, shared hidden-information access and assistance penalties.
- Public matchmaking, multi-instance realtime scaling, formal high-availability guarantees and arbitrary migration of incompatible race histories.
- Full graphical polish and advanced hosting infrastructure beyond what the family session and recoverable deadlines require.

## Further Notes

This completes the Stage 3 capability after shared sessions. It intentionally extends the earlier slice's host-controlled presentation into a configurable live progression policy and replaces in-memory-only recovery with durable recovery. Those are explicit milestone changes, not corrections to the preceding spec.

Persistence and replay support should be proven before enabling automatic progression. Then add deadlines/fallback, observation-driven AI and assistance, and finally validate the hosted family path. The next milestone is weekend preparation, qualifying and starts.

The approved API/SignalR and browser testing boundaries apply unchanged. Emulator persistence and controlled time are implementation environment dependencies used behind that same public testing boundary.
