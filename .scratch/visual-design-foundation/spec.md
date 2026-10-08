# Visual direction and design-system foundation

Status: resolved

## Problem Statement

The game has a simulation foundation and a selected driver-card visual direction, but no integrated visual system for the shared display and race controller. Implementing these surfaces independently would make cards, attributes and race decisions inconsistent, and leave TV readability and two-car management untested.

The recovered card prototype is illustrative documentation. Its portrait binaries are missing locally, its recovered build has not been verified, and its mobile and TV presentation still needs visual review. It cannot yet serve as an assumed production foundation.

## Solution

Deliver a small reusable Angular design foundation and an executable representative-screen gallery for the browser race milestone. Extend the selected Broadcast direction into driver cards, a public timing tower, a two-car private controller, race decision controls and phase-result feedback. Document the visual rules and verify representative phone, tablet and shared-display layouts.

Broadcast elements, especially the timing tower, should look recognisably similar to the current F1 television broadcast. Use its visual hierarchy, compact race-order rows, position and driver identification, team-colour cues, gap presentation and tyre/status graphics as reference points, adapted for this game's readability and interaction needs.

The result should provide production-usable UI primitives and a clear visual baseline without implementing race orchestration or the full game's card catalogue.

## User Stories

1. As a player, I want a coherent broadcast-inspired visual language, so that cards and race screens feel like one game.
2. As a player, I want a driver's name and identity readable at a glance, so that I can distinguish the two cars I manage.
3. As a player, I want compact driver cards, so that I can scan multiple drivers without losing essential information.
4. As a player, I want expanded driver details, so that I can inspect attributes without crowding the compact card.
5. As a shared-display viewer, I want a TV-scale driver treatment, so that identity remains readable at living-room distance.
6. As a player, I want team ownership shown separately from personnel role styling, so that team colour does not change the meaning of a driver card.
7. As a player, I want live text and attribute displays, so that content remains readable, responsive and accessible.
8. As a player, I want unknown or unimplemented attributes clearly distinguished from real ratings, so that illustrative data cannot mislead me.
9. As a player, I want preference tags distinguishable from quality ratings, so that I understand their different meanings.
10. As a player, I want the public timing tower to show Classification and readable gaps, so that I understand track position.
11. As a player, I want lap deficits clearly distinguished from time gaps, so that lapped cars are understandable.
12. As a player, I want Tyre Compounds identified by text as well as colour, so that their meaning is accessible.
13. As a player, I want estimated tyre condition labelled as an estimate, so that I do not mistake it for an exact measurement.
14. As a team controller, I want both drivers' decision areas visible and distinct, so that I can confidently manage two cars.
15. As a team controller, I want Conserve, Standard and Push choices with short trade-off explanations, so that I understand Pace Instructions.
16. As a team controller, I want Stay Out and Pit plus compound controls, so that my Pit Instruction is explicit.
17. As a team controller, I want contextual Neutral, Attack and Defend controls, so that irrelevant battle options do not clutter decisions.
18. As a team controller, I want selected, submitted and locked states distinguished, so that I know whether a choice is committed.
19. As a team controller, I want a clear summary of both cars before locking, so that I can catch a mistaken choice.
20. As a player, I want loading, resolving, disabled and error states, so that waiting or rejected input is understandable.
21. As a viewer, I want concise overtake and pit event treatments, so that I understand important changes during a Race Phase.
22. As a player, I want an unambiguous finished-race treatment, so that results are distinguishable from an intermediate phase.
23. As a keyboard user, I want visible focus and operable controls, so that I can make the same choices without a mouse.
24. As a touch user, I want controls with adequate target size and spacing, so that neighbouring options are not easy to select accidentally.
25. As a player sensitive to motion, I want reduced-motion presentation, so that I can follow the same race information comfortably.
26. As a player, I want useful cards when a portrait fails to load, so that identity and decisions remain available.
27. As a developer, I want reusable semantic tokens and components, so that later screens can follow the same rules.
28. As a designer, I want a gallery with realistic edge cases, so that long names, errors and dense race states are visible before integration.
29. As a developer, I want a lightweight controller bundle, so that controller devices need not load a 3D race renderer.
30. As a designer, I want recorded visual review results, so that validated layouts and unresolved problems are clear.
31. As an F1 viewer, I want the timing tower and related race graphics to resemble the current television broadcast, so that race information feels familiar and is easy to scan.

## Implementation Decisions

- Establish a game frontend using the locked Angular direction. Put reusable UI and the representative-screen gallery in the same frontend workspace intended for the browser slice. Keep the gallery independently runnable with fixtures; do not turn the documentation prototype into the production app by assumption.
- Use the recovered Broadcast card direction as the baseline: charcoal surfaces, bold italic display typography, coral driver-role accent, quieter outlined preference tags and separate ownership badge/border. Preserve coral and secondary-gray diagonal portrait-background stripes behind the portrait, with the documented reference proportions. Record deliberate departures and their readability rationale.
- Audit and reuse sound prototype patterns rather than preserving its standalone build or illustrative state model. Record what was carried forward and what changed. Inspect local source and documentation; remote prototype availability is not required for completion.
- Define semantic colour, typography, spacing, radius, border, elevation and motion tokens. Include accessible text and status treatments and a hierarchy for primary choices, secondary details and disabled controls. Keep visual tokens independent of simulation coefficients.
- Use Tailwind CSS for layout, spacing and responsive utilities in the Angular frontend. Map the agreed semantic design tokens into its theme; use component CSS for distinctive Broadcast graphics and complex component treatments. Tailwind supports the visual system rather than defining its appearance. The user approved this styling choice during ticket publication.
- Implement compact, expanded and TV driver-card variants with separate portrait assets and live data-driven names, attributes, preferences and ownership. Define an explicit UI view model; sample ratings, prices and roster content are not production simulation attributes. Label fixture content in the gallery, not inside every future live-game card.
- Preserve the concept's five Simple driver groups as the display vocabulary. Sample detailed attributes may appear in expanded gallery fixtures but must not imply that the current engine implements them. Never invent a numerical conversion between card ratings and simulation inputs.
- Use neutral portrait placeholders or locally available assets with explicit provenance when recovered portraits are unavailable. A missing asset must not block the foundation or trigger dependency on remote assets. Full roster photography is deferred.
- Treat the repository's `assets` folder as the shared source of supplied design assets. It currently contains driver imagery, and the user will add track and other assets over time. Inspect its current contents when starting relevant design work, reuse suitable supplied assets, and document identity mapping, provenance and frontend packaging. Keep asset references data-driven so additional assets can be adopted without redesigning components; use accessible fallbacks when an asset is absent or unsuitable. Availability of track assets does not expand this milestone into full track-card or 3D-board implementation.
- Supply reusable compound badges, labelled estimate displays, buttons/choice groups, status/error messages and race event summaries. Provide a public timing-tower composition and a two-driver controller composition built from these parts.
- Reference the current F1 television broadcast when designing the timing tower and related public race graphics. At implementation time, inspect recent broadcast examples and record dated visual references so that “current” has a concrete baseline. Carry forward recognisable information hierarchy, row structure, typography treatment, team identification, gaps, tyres and status cues; adapt size and contrast for living-room viewing. Reconcile these elements with the selected Broadcast card direction. Similarity is a visual requirement, not a requirement to reproduce every broadcast feature or add unsupported race data.
- Include gallery states for the initial race, a decision phase, a pending/ongoing stop, contextual battle availability, a locked team, resolution, phase results and race finish. Add long names, missing portraits, lap deficits and rejected-input fixtures.
- The gallery uses explicit public and private fixtures. Do not include hidden tyre condition, random draws or opponent decisions in public examples. It demonstrates information hierarchy; actual server filtering belongs to the browser spec.
- Use responsive layouts at representative widths of 360px, 768px and 1920px, including a 1920×1080 shared-display view. Avoid clipped names and essential controls; scrolling is acceptable on phones and for secondary details. Test portrait and landscape where relevant.
- Use semantic HTML, labelled controls, visible focus, text alternatives for colour and reduced-motion support. Target WCAG 2.2 AA contrast and at least 44×44 CSS-pixel primary touch targets. Record manual checks and observed limitations.
- Cards, timing tower, menus and controls use Angular, HTML/CSS and appropriate SVG icons. Babylon.js and a 3D board are outside this foundation. Do not put game rules or race state transitions in UI components.
- Deliver concise documentation covering tokens, card anatomy, role versus ownership styling, attribute semantics, interaction states, asset provenance, responsive usage and gallery execution. Document extension points for later personnel/package cards without implementing them now.

## Testing Decisions

- User-confirmed highest testing boundary: the rendered Angular components through the representative-screen gallery, using visible labels, roles and interactions.
- Cover observable behaviour: expanded details, selected choices, two-car summaries, locked/disabled controls, contextual battle visibility, error recovery, keyboard access and missing-image fallback. Avoid assertions about private component methods or exact DOM nesting.
- Use visual inspection and a small stable set of screenshots for compact/expanded/TV cards, public timing tower and two-car controller at representative sizes. Record screenshot dimensions and review findings; do not consider screenshot generation alone a successful visual review.
- Compare the timing tower and related public race graphics against the recorded recent F1 broadcast references. Review recognisable visual similarity alongside accurate game data, accessible cues and shared-display readability; document deliberate adaptations.
- Check text contrast, focus, text alternatives, touch targets and reduced-motion behaviour. Assess shared-display readability at a documented viewing distance when a real TV playtest is available; desktop screenshots do not prove that outcome.
- Prior art is the recovered Angular driver-card prototype and its reported interactions. Its local build and visual QA are unverified; establish reproducible checks in the game frontend rather than inheriting that claim. Existing C# simulation tests are not a frontend testing boundary.
- Build and type-check the frontend and verify that gallery/controller entry points do not import a race-rendering bundle. Test only behaviour and representative layouts needed for this foundation.

## Out of Scope

- Authoritative race sessions, HTTP/SignalR integration, multiplayer ownership, persistence, authentication and deployment.
- Complete roster assets, final individual ratings/prices, new simulation attributes and personnel acquisition.
- Manager, Race Engineer, Technical Engineer, Race Package and development card implementations.
- Babylon.js scene, final race animation, qualifying, starts, incidents, weather and campaign screens.
- Race cards, qualifying cards, recommendations and AI strategy.

## Further Notes

This is the roadmap's visual foundation before or alongside the browser race. It extends the existing selected direction rather than reopening a visual-style interview. The browser slice consumes its usable components; both efforts can progress using fixtures while the engine evidence is reviewed.

Completion means a working gallery, reusable foundation, documented rules and reviewed representative layouts. It does not mean all graphics or the whole design system are final. Later milestones extend this baseline and validate it in real play.

## Comments

The user confirmed the proposed testing boundary and requested that broadcast elements such as the timing tower resemble the current F1 television broadcast.

## Completion evidence

All six tickets are resolved on integration branch codex/visual-design-foundation. Final standards and spec reviews reported zero actionable findings; all 34 rendered browser tests, production build and strict type checking passed. See [final review](review.md) and [integrated evidence](evidence/integrated-review.md). Real TV viewing-distance validation remains an explicit playtest limitation.

