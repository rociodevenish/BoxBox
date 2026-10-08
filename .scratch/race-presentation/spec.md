# Race-board presentation and Simple/Full views

Status: ready-for-agent
Blocked by: complete weekend event/projection contracts; visual foundation; representative validated circuit/assets. Full coverage depends on full-calendar content.

## Problem Statement

The timing tower and concise event list expose correct outcomes but do not yet provide the intended stylised shared race board. Enthusiasts also need supported detailed information without burdening casual controllers or introducing another simulation.

## Solution

Add a Babylon.js shared-display race scene driven by authoritative presentation events and snapshots, alongside Angular cards and controls. Provide Simple and Full information views over the same state, with accessible DOM alternatives, reduced motion and independent lightweight controllers.

## User Stories

1. As a viewer, I want a stylised track and cars, so that I can follow the race spatially.
2. As a viewer, I want team identification, so that cars and the timing tower agree.
3. As a viewer, I want starts, overtakes and stops highlighted, so that key events are understandable.
4. As a viewer, I want incidents and weather visible, so that disruption has context.
5. As a player, I want exact results after playback, so that animation cannot alter race truth.
6. As a viewer, I want skip and replay, so that presentation remains comfortable.
7. As a viewer, I want reduced motion, so that I receive the same information without unnecessary animation.
8. As a player, I want Simple grouped attributes, so that decisions remain approachable.
9. As an enthusiast, I want supported detailed attributes and explanations, so that I can inspect the same model more deeply.
10. As a player, I want view switching to retain my draft, so that changing detail cannot lose choices.
11. As a player, I want Full view to receive no hidden information, so that detail is not a competitive privilege.
12. As a phone user, I want a lightweight controller, so that I do not download the race renderer.
13. As a viewer, I want fallback when graphics fail, so that the race remains playable.
14. As a player, I want readable DOM text and controls, so that information does not depend on 3D labels.
15. As a viewer, I want refresh to recover current playback/state, so that disconnect does not block progression.
16. As a host, I want bounded presentation duration, so that visuals do not overwhelm the weekend target.
17. As a maintainer, I want new track assets mapped through data, so that circuits do not require bespoke code.
18. As a designer, I want performance measured on household devices, so that graphical quality reflects actual constraints.

## Implementation Decisions

- Babylon.js owns stylised track geometry, cars and race-event animation; Angular/HTML/CSS/SVG and Tailwind own cards, text, timing tower, menus and decision controls. Lazy-load the renderer only for display routes.
- Consume allowlisted authoritative presentation contracts, never raw diagnostic histories. Use stable event identifiers, phase/revision and authoritative final snapshots; client playback has no physics, random draws or permission to resolve races.
- When meaningful recorded position/time anchors exist, animate between them. Where a timeline is a compressed highlight, make its illustrative nature clear and reconcile exact Classification afterward; never fabricate precise continuous positions or causal undercut claims.
- Keep simulation time distinct from presentation time. Bounded highlights prioritise important events, summarize dense phases and describe quiet phases. Skip/reduced motion shows equivalent text/results; the server progresses without display acknowledgement.
- Include automatic start, passing, pit entry/service/exit, retirements, Safety Cars and weather cues only for supported events. Playback can replay committed outcomes without opening another decision window or applying effects twice.
- Map circuit profiles and supplied track art/geometry through stable asset identities. Inspect new repository assets, validate orientation/scale/provenance and use a consistent stylised fallback when absent; do not infer simulation segments from artwork without explicit configuration.
- Simple and Full are presentation/detail settings over the same observation and effective-input model. Full exposes supported attribute breakdowns and explanations, never exact hidden tyres/form/future draws or opponent locked choices. Inactive attributes stay labelled.
- Do not add per-driver setup decisions in this spec: retain existing team-wide rules for both views. Such a later rule extension would need its own explicit contract; Full detail alone does not imply new actions.
- Keep assistance level and privacy mode independent of detail view. Changing view preserves draft choices, locks, membership and recommendations and cannot grant additional decisions or resample observations.
- Provide semantic DOM event summaries and exact timing tower as permanent alternatives to the scene. Graphics failure, unavailable WebGL or asset failure falls back without blocking player decisions or server progression.
- Recover display subscriptions from current revision and event cursors, clearing obsolete playback queues. Older/duplicate notifications do not replay as new events or reconcile to stale state.
- Verify shared-display viewing distance and household-device performance. Establish measured frame-time, load-size and memory targets during representative implementation, with documented quality tiers rather than assuming desktop performance.

## Testing Decisions

- Reuse approved API/SignalR projections and browser/gallery boundaries. Test playback using real committed session events and observable displayed text/state; avoid private scene graph or animation-method assertions.
- Browser checks cover view switching, draft preservation, reduced motion, replay/skip, graphics fallback, reconnect and exact final-state reconciliation. Verify controller bundles exclude Babylon.js.
- Test public/private filtering for both detail views and event payloads with hidden-value fixtures. Existing simulation/replay tests remain the source of physics correctness.
- Inspect a stable screenshot/video set for representative circuits and events on phone/tablet/shared display; record dimensions, hardware, viewing distance and findings. Asset generation or screenshots alone do not prove readability.
- Profile representative scene/load workloads and dense events, then verify graceful quality/fallback behaviour; avoid tight frame-rate unit tests.

## Out of Scope

- Photorealistic racing, steering/collision physics, live continuous multiplayer position streaming and a second Full-mode engine.
- New per-driver setup rules, hidden-information access, final release balance and complete operational certification.

## Data and asset references

Review these repository inputs before implementing this spec:
- [f1_strategy_track_cards_2026.json](../../.docs/data/f1_strategy_track_cards_2026.json) - circuit identities, event-to-track calendar mapping, demands and race characteristics.
- [f1_strategy_driver_cards_2026.json](../../.docs/data/f1_strategy_driver_cards_2026.json) - driver identities, headline/detailed ratings and compatibility preferences.
- [f1_strategy_car_attribute_spec_2026.json](../../.docs/data/f1_strategy_car_attribute_spec_2026.json) - car attribute vocabulary, scales, headline derivation, setup and development/package domains.

Use [driver portraits](../../assets/driver%20images/) and [track imagery](../../assets/track%20images/) wherever this feature presents those identities. Inspect the current [assets collection](../../assets/) at implementation time, including later additions; map files through stable content identities, record provenance, preserve originals and package suitable runtime variants. Keep readable text and missing-image fallbacks. Simulation-only work uses imagery only for associated reports or galleries.

The JSON files are provisional design inputs. Preserve schema/data versions, validate references, units and bounds, and document mappings to supported runtime attributes; do not silently convert headline stars into simulation values. Data availability does not expand the mechanics in this spec. Keep active campaign/race inputs stable when files or assets change.

Map art to stable circuit/person identities and reuse headline/detail derivations for Simple/Full displays. Track PNGs are presentation references, not ready-made 3D geometry or simulation segments.

## Further Notes

The race board explains existing outcomes. Exact Classification and usable controllers remain available throughout. The user's approved public API/SignalR and focused browser testing boundaries apply; no new simulation seam is needed for rendering.
