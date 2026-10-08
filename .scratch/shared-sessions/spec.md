# Shared sessions and controller ownership

Status: ready-for-agent
Blocked by: browser dry-race vertical slice and its session contracts; visual foundation for controller and display integration.

## Problem Statement

The browser race supports one human team. A household cannot yet join the same race, choose teams, share a tablet or make decisions openly on the TV. Treating a device as a player would prevent pass-and-play and risk exposing private team information. Multiple controllers also require a reliable simultaneous decision barrier rather than resolving when the first team submits.

## Solution

Extend the browser race into a family session with a host, authenticated accounts or guests, up to eleven controlled teams and a shared public display. Support separate private controllers, one device serving several players, and open-family TV decisions. The server owns team assignment, authorisation and the complete phase decision set.

Keep dry-race mechanics and scripted opponents unchanged. Deadlines, durable recovery, adaptive AI and assistance are delivered by the following continuity spec.

## User Stories

1. As a host, I want to create a household session, so that everyone can join the same race.
2. As a host, I want a join link, code and QR code, so that joining is convenient on different devices.
3. As a family player, I want guest access, so that I do not need a permanent account.
4. As a host, I want my identity to carry administrative authority, so that spectators cannot change the session.
5. As a player, I want a clear list of available teams, so that I can choose an unclaimed identity.
6. As a player, I want a team claim acknowledged by the server, so that competing claims cannot both succeed.
7. As a player, I want to control both cars of my assigned team, so that team play remains consistent.
8. As a host, I want to approve or change assignments before the race, so that family members can use the intended teams.
9. As a host, I want unclaimed teams filled by scripted opponents, so that a full grid does not require eleven people.
10. As a spectator, I want a public display without controller permissions, so that I can watch safely.
11. As a player, I want to use a private phone controller, so that other teams cannot see my estimates or choices.
12. As a household, I want several players to use one tablet, so that everyone can participate with fewer devices.
13. As a tablet user, I want a neutral pass-to-team screen, so that the next player does not see the previous team's information.
14. As a tablet user, I want an explicit Ready action before private information appears, so that I can hand over the device first.
15. As a player, I want my team clearly identified while editing, so that I cannot submit for the wrong team.
16. As a host, I want open-family TV control, so that younger players can choose with help.
17. As a host, I want Competitive, Family and Mixed control modes, so that privacy matches the household's play style.
18. As a player, I want to know when my team's information is intentionally open, so that the privacy setting is clear.
19. As a host, I want device assignments separate from team ownership, so that a shared device can serve several people.
20. As a player, I want my two-car submission locked atomically, so that partial choices cannot enter the race.
21. As a player, I want opponent decisions hidden before resolution in private play, so that simultaneous choices are meaningful.
22. As a player, I want a public readiness indicator without private choices, so that I know who is still deciding.
23. As a player, I want the race to wait for all required team locks, so that an early submitter cannot advance everyone.
24. As a player, I want duplicate or stale submissions handled safely, so that retries cannot resolve the phase twice.
25. As a player, I want one authoritative lock state across my authorised devices, so that conflicting drafts cannot overwrite accepted choices.
26. As a host, I want to pause before the next decision window, so that the household can take a break.
27. As a player, I want an explicit explanation when I lose controller authority, so that I stop editing obsolete controls.
28. As a host, I want to revoke an unwanted controller assignment, so that private access can be removed.
29. As a player, I want controller refresh to retrieve the active session, so that ordinary browser reloads do not lose my team.
30. As a viewer, I want a phase presentation followed by exact race state, so that shared play uses the same race truth as single-team play.
31. As a host, I want a controlled continue action after presentation, so that a disconnected TV cannot stall the race.
32. As a playtester, I want private, shared-tablet and open-family scenarios documented, so that the first multiplayer playtest covers the intended audience.

## Implementation Decisions

- Extend the existing Game.Api session coordinator, Angular display/controller and public/private SignalR contracts. Reuse the race engine and its per-team observation projection. Do not create a second multiplayer simulation.
- Model host/player identities, teams, device sessions and controller assignments separately. A team has one responsible human player or scripted control, while an authorised device can serve one or more teams. A shared-tablet participant record remains associated with its player identity even when the device acts for several participants.
- Use Firebase Authentication for permanent host accounts and anonymous/guest players, with a local emulator development path. Validate identity server-side. Join codes/links locate a session and request membership; they do not themselves grant host or team permissions. Do not place a private controller credential in the public QR code.
- Scope the first release to family sessions, not matchmaking or a public session directory. Support up to eleven team slots and twenty-two cars per race. All commands and SignalR subscriptions are scoped to the authenticated session membership and current assignment.
- Let a host create the lobby and set control/privacy mode. Joining players choose an available team through an atomic server claim. The host may assign shared-device participants and approve or correct assignments before starting. Freeze the human/scripted participant set when the race begins; mid-race takeover is deferred to continuity.
- Teams retain constructor identity independently of their preset drivers and performance. Claiming an identity does not acquire personnel or change the simulation fixture's attributes.
- Competitive mode keeps all strategic decisions on private controllers. Family mode allows designated teams' decisions and team information to appear on the shared screen. Mixed mode assigns private, shared-tablet or open control per team. Display intentional openness clearly in the lobby and active controller.
- Open-family presentation includes only information legitimately available to that team, never hidden simulation diagnostics. A spectator display has no command authority; TV control requires a separate authorised device assignment. Competitive teams in Mixed mode must not leak through an open team's interface.
- Shared-device authorisation covers an explicit list of teams. The UI shows only the active team's private view and uses a neutral handoff screen before fetching/subscribing to the next view. On handoff, clear rendered private data, editable drafts and private notifications, and unsubscribe the old team stream. Exclude private views from offline/service-worker caches. An authorised shared device remains a trusted household device; the handoff screen is a privacy courtesy, not separate authentication against a malicious device user.
- After a team lock, move to a neutral handoff with the next required team and a Ready action. Provide an explicit neutral exit/handoff action before lock, discarding an unsubmitted draft with clear feedback. Never display previous choices behind the handoff screen.
- Resolve controller permissions on every command and private subscription, not just at initial join. Reassignment/revocation closes obsolete subscriptions and invalidates prior assignment authority. Private updates carry team identity and assignment revision so the UI cannot apply one team's message to another.
- Preserve atomic two-driver lock commands with command identity and expected Race Phase/revision. Serialize session transitions. The first valid lock for a team is final for that phase; another authorised device can see the accepted state but cannot replace it. Rejected commands do not change any driver or team lock.
- Freeze scripted choices from their phase-start observations before any human lock. Resolve only when every required human team has locked and all scripted decisions are present. Publish per-team readiness without exposing private selections; membership or revocation must not silently change an active phase's required set.
- This spec introduces no decision timer or unattended progress. If a player is absent, the phase waits. The host can pause or resume a decision window but cannot force arbitrary hidden choices for an absent private team. The following spec supplies explicit fallback and takeover rules.
- Replace the single-team continue action with an authorised host continue after the bounded phase presentation. The display can replay or skip locally without advancing race state. Host action opens the next window exactly once; host pause prevents the next opening and cannot cancel a committed resolution.
- Provide current snapshots for refresh and resubscription within the server lifetime. Include membership, assignment, readiness and lifecycle revisions. Full server-restart recovery remains outside this spec; do not advertise durability yet.
- Preserve the Broadcast design system and F1-style timing tower. Extend it with lobby, joining, team ownership, readiness, handoff, privacy-mode and host-control states. Maintain phone/tablet/TV readability and accessible labels.
- Deliver local multi-device setup using authentication emulators, a representative household playtest and findings about handoff time, two-car management, privacy understanding and joining friction. The cloud deployment comes with continuity.

## Testing Decisions

- Reuse the user-approved main boundary: public session API and SignalR behaviour through the server, with focused browser end-to-end tests. Authorisation, claims and the decision barrier are exercised through real requests/subscriptions rather than private coordinator methods. The existing simulation suite remains responsible for race rules.
- Prior art is the browser-slice command/revision, payload filtering and reconnect coverage, plus the visual foundation's representative-screen checks. Extend those harnesses rather than adding new physics or ownership helper seams.
- Verify simultaneous team claims, capacity, unauthorised host commands, cross-session requests, wrong-team submissions and unauthorised private subscription attempts. Concurrent claims have one winner and clear outcomes; failed claims leave assignments coherent.
- Lock teams in different orders and concurrently; assert no resolution before the complete required set and exactly one phase outcome afterwards. Repeated locks, stale revisions and conflicting submissions do not replace accepted decisions.
- Exercise public, private and open-family wire payloads. Readiness must not reveal private choices, and open access must not expose hidden engine condition or another team's private data. Revoked devices receive no further private updates and cannot submit.
- Use browser tests for two private controllers, several participants sharing a tablet, open TV control and Mixed mode. Check that handoff hides previous private state and requires Ready before revealing the next team, including refresh and late messages.
- Verify host pause/continue, spectator restrictions, missing-player waiting and refresh within server lifetime. Do not claim durable restart support from these tests.
- Build/type-check the frontend, run relevant server/simulation regression tests and document actual multi-device visual/privacy playtest results. Avoid testing internal group-name strings or reproducing authorisation implementation in tests.

## Out of Scope

- Decision deadlines, absent-player fallback, mid-race takeover, strategic AI and assistance.
- Durable state, server restart recovery, production hosting and authentication account-management screens.
- Campaigns, new race mechanics, qualifying, weather, personnel acquisition and development.
- Public matchmaking, chat, social features, large-scale concurrency, adversarial privacy between users sharing one trusted device and independent guest sign-in on every tablet handoff.

## Data and asset references

Review these repository inputs before implementing this spec:
- [f1_strategy_driver_cards_2026.json](../../.docs/data/f1_strategy_driver_cards_2026.json) - driver identities, headline/detailed ratings and compatibility preferences.
- [f1_strategy_track_cards_2026.json](../../.docs/data/f1_strategy_track_cards_2026.json) - circuit identities, event-to-track calendar mapping, demands and race characteristics.
- [f1_strategy_car_attribute_spec_2026.json](../../.docs/data/f1_strategy_car_attribute_spec_2026.json) - car attribute vocabulary, scales, headline derivation, setup and development/package domains.

Use [driver portraits](../../assets/driver%20images/) and [track imagery](../../assets/track%20images/) wherever this feature presents those identities. Inspect the current [assets collection](../../assets/) at implementation time, including later additions; map files through stable content identities, record provenance, preserve originals and package suitable runtime variants. Keep readable text and missing-image fallbacks. Simulation-only work uses imagery only for associated reports or galleries.

The JSON files are provisional design inputs. Preserve schema/data versions, validate references, units and bounds, and document mappings to supported runtime attributes; do not silently convert headline stars into simulation values. Data availability does not expand the mechanics in this spec. Keep active campaign/race inputs stable when files or assets change.

Use stable driver/circuit references in shared presets. Constructor ownership remains independent of driver reference-team metadata and performance; a complete constructor catalogue still needs authoring.

## Further Notes

This is the first Stage 3 spec. It deliberately establishes membership, privacy and the simultaneous decision barrier before deadlines can change the decision set. It retains scripted fill-ins until the following spec supplies AI.

The approved API/SignalR and browser testing boundaries apply unchanged. All additional ownership and device behaviour is tested through those same public boundaries.
