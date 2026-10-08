# F1 Strategy Game — Project Roadmap

Status: proposed direction for discussion
Updated: 2026-10-09

## Destination

Deliver a browser-based, season-long team strategy game for family and friends. Each player controls two cars, makes simultaneous decisions during live Grand Prix sessions, and prepares and develops their team asynchronously between races. The shared TV tells the race story; private or shared controllers make decisions.

The finished game supports eleven teams, AI filling unclaimed seats, both championships, a full calendar, meaningful preseason and development choices, and Simple and Full views over the same simulation. A Grand Prix including qualifying should take approximately 30 minutes.

This roadmap groups work into playable milestones. It does not define implementation contracts, acceptance criteria, ticket breakdowns or delivery dates. Future spec names below are candidates, not approved specs. Existing locked concept and architecture decisions remain the baseline.

## Where we are

The two existing specs form the first milestone:

- [Dry-race strategy laboratory](../.scratch/dry-race-strategy-laboratory/spec.md): marked resolved. Establishes the standalone C# simulation, Race Phases, pace/tyre/pit/battle decisions, exact Classification, reproducible races and paired strategy evidence.
- [Segment race model and controlled variability](../.scratch/segment-race-model/spec.md): marked ready-for-agent; implementation is underway according to current project context. Adds spatial track demands, passing opportunities, explicit random scopes, and further evidence and performance measurements.

The laboratory demonstrates model behaviour. It does not yet demonstrate that people understand the choices, enjoy making them, or complete a race within the intended time. That is the next major uncertainty to resolve.

## Written spec index

All eighteen delivery specs currently written in the local tracker are linked below in milestone order. Each spec is the source of truth for its current status, blockers, scope and testing decisions. Future candidates in this roadmap are not written specs until their tracker documents exist.

| Stage | Written spec | Scope |
| --- | --- | --- |
| 1 | [Dry-race strategy laboratory](../.scratch/dry-race-strategy-laboratory/spec.md) | Standalone race simulation, strategy scripts, replay and paired evidence. |
| 1 | [Segment race model and controlled variability](../.scratch/segment-race-model/spec.md) | Spatial track demands, passing zones, controlled random scopes and modelling evidence. |
| 2 | [Visual direction and design-system foundation](../.scratch/visual-design-foundation/spec.md) | Broadcast visual language, driver cards, F1-style timing tower, reusable controls and representative-screen gallery. |
| 2 | [Browser dry-race vertical slice](../.scratch/browser-dry-race/spec.md) | One human team managing two cars through a complete authoritative browser race. |
| 3 | [Shared sessions and controller ownership](../.scratch/shared-sessions/spec.md) | Multiple players, team assignment, simultaneous locks, private controllers, pass-and-play and open-family control. |
| 3 | [Race continuity, AI and assistance](../.scratch/race-continuity-ai-assistance/spec.md) | Durable recovery, deadlines, fallback/takeover, fair AI and recommendations, and hosted family play. |
| 4 | [Weekend preparation and effective attributes](../.scratch/weekend-preparation/spec.md) | Preset effective attributes, team-wide setup, practice feedback and qualifying lock. |
| 4 | [Qualifying and automatic race start](../.scratch/qualifying-race-start/spec.md) | Simultaneous Q1/Q2/Q3 approaches, grid generation and automatic opening. |
| 4 | [Incidents, reliability and Safety Cars](../.scratch/incidents-safety-cars/spec.md) | Contextual risk, damage/retirement, interrupted phases and neutralised pit decisions. |
| 4 | [Weather strategy](../.scratch/weather-strategy/spec.md) | Imperfect forecasts, evolving wetness, weather tyres and reaction windows. |
| 5 | [Campaign, calendar and championships](../.scratch/campaign-championships/spec.md) | Saved preset campaign, both standings, reports and asynchronous preparation. |
| 6 | [Personnel acquisition](../.scratch/personnel-acquisition/spec.md) | Unique fixed-price offers, Scouts, priority and starting-budget ledger. |
| 6 | [Initial car construction](../.scratch/initial-car-construction/spec.md) | Equal-access car compromises, budget allocation and locked Base Car. |
| 6 | [Season economy and permanent development](../.scratch/season-economy-development/spec.md) | Modest income, one development opportunity, personnel effects and permanent gains. |
| 6 | [Race Package library and development outcomes](../.scratch/race-packages/spec.md) | Library, selection/refinement, richer outcomes and Rush using the shared development opportunity. |
| 7 | [Full-calendar content and card coverage](../.scratch/full-calendar-content/spec.md) | Validated calendar, unique personnel pools, content versions, cards and supplied asset coverage. |
| 7 | [Race-board presentation and Simple/Full views](../.scratch/race-presentation/spec.md) | Babylon.js display, event playback, accessible fallback and detail views over one simulation. |
| 7 | [Season balance and release readiness](../.scratch/release-readiness/spec.md) | Integrated balance, household campaigns, onboarding, accessibility, device and operational evidence. |

Supporting design reference: [Driver card sample design specification](driver-card-design-spec.sample.md) preserves the selected card direction and prototype handoff. It is an illustrative design reference, not an additional delivery spec, and is excluded from the delivery-spec count.

## Milestones

| Stage | Player outcome | Main work | Evidence to move forward |
| --- | --- | --- | --- |
| 1. Prove the race foundation — current | Strategic choices produce coherent, explainable races. | Complete the two existing specs and interpret their evidence, including spatial racing, tyre/pit trade-offs and controlled variability. | Reproducible outcomes; useful competing strategies across contrasting tracks; remaining modelling limits are understood. |
| 2. Play a dry race in the browser | A player can manage both cars from start to finish and understand what happened. | Thin authoritative server, browser timing tower and controller, simultaneous decision locking, phase results, short event presentation, fixed-grid dry races and scripted opponents. Start with one complete phase, then extend to a full race. | People understand pace, pits and battles; feedback explains consequences; decision and presentation timing supports the race duration target. |
| 3. Play together reliably | Several people can finish a race using a TV, phones or a shared device. | Session joining, team ownership, public/private information, pass-and-play and open-family control; deadlines, reconnect/resume and durable state boundaries. Introduce rules-based AI for empty teams and missed decisions, plus initial Assisted/Guided recommendations. Establish a hosted family playtest. | A household can join and finish without developer intervention; disconnects and missed decisions do not stall play or expose private choices. |
| 4. Complete a Grand Prix weekend | Preparation and qualifying influence the race, and unexpected events create decisions. | Effective car/driver attributes and circuit fit; weekend setup and practice feedback; Q1/Q2/Q3; automatic standing start; incidents, reliability and Safety Cars; simplified weather and suitable tyres. Extend advice and presentation as each system arrives. | A full weekend fits approximately 30 minutes; interrupted phases are clear; qualifying, setup and event responses add worthwhile strategy. |
| 5. Play a connected season | Races matter beyond their individual results. | Calendar progression, Constructors' and Drivers' standings, post-race reports, saved campaign state, asynchronous preparation and human/AI continuity between events. Initially use preset rosters and cars to isolate the campaign flow. | A short test campaign works across separate play sessions, then the full calendar can run without manual state repair. |
| 6. Build and develop a team | Players create a team identity and live with their spending choices. | Competitive fixed-price personnel acquisition, Scout tokens and rotating priority; equal-access initial car choices; starting budget and modest income; permanent development, Race Package library/selection/refinement, Rush and development outcomes. Personnel improve information, efficiency and execution as designed. | Different roster, car and investment strategies remain viable across the season; spending creates trade-offs without runaway accumulation. |
| 7. Deliver the complete family game | The whole campaign feels clear, varied, attractive and dependable. | Full calendar content and circuit profiles; richer cards and Babylon.js race board; Simple/Full views; mature assistance and AI; accessibility, onboarding, device compatibility, deployment operations and season-wide balancing. | Repeated family campaigns meet clarity, pacing, fairness, reliability and hosting-cost goals. |

These stages describe increasing product capability. They are not isolated waterfalls: presentation, accessibility, testing and balance improve throughout. Later systems should reach the laboratory before entering the live game, and each addition should be playtested in the browser.

## Cards, attributes and content across the milestones

Establish each card's purpose and attribute meanings before implementing the system it represents. Build representative cards early, then expand the roster and finalise numerical values through playtesting. Card presentation does not imply a new card-play mechanic.

| Card or information type | Establish | Build and validate | Finalise for initial release |
| --- | --- | --- | --- |
| Driver | Stage 1: validate race-relevant attributes. Stage 2: establish card anatomy, grouped attribute display and information hierarchy. | Stage 4: qualifying, starts, preferences, adaptability and setup feedback. Stage 6: acquisition and pricing. | Stage 7: roster ratings, prices and presentation after season testing. |
| Car | Stages 1–2: establish performance vocabulary and readable grouped attributes. | Stage 4: attributes needed for setup, circuit fit and reliability. Stage 6: equal-access construction choices and permanent development. | Stages 6–7: viable specialisation and season-wide performance balance. |
| Race Engineer | Stages 2–3: establish how tyre estimates, uncertainty and advice appear. | Stage 4: setup and tyre-reading effects using preset personnel. Stage 6: acquisition, differentiated personnel, communication effects and prices. | Stage 7: personnel value and recommendation quality across campaigns. |
| Manager | Before Stage 6: establish attribute meanings alongside economy modelling. | Stage 6: acquisition, leadership, operations, development and resource-efficiency effects. | Stage 7: prices and value relative to drivers and engineering staff. |
| Technical / Design Engineer | Before Stage 6: establish specialisations and development effects. | Stage 6: acquisition, development direction, quality, risk and efficiency. | Stage 7: viable staffing and investment strategies across the calendar. |
| Race Package and development choice | Before Stage 6: establish the player-facing choices and their trade-offs. | Stage 6: package creation, selection, refinement, permanent development, Rush and outcomes. | Stage 7: costs, strengths, compromises and long-term balance. |
| Circuit information | Stage 1: model contrasting track demands. Stage 2: establish the player-facing circuit summary. | Stage 4: preparation, weather, mechanical stress and Safety-Car information. | Stage 7: complete calendar content and validated circuit tuning. |

Pit Crew remains a team attribute rather than a competitively drafted personnel card. Qualifying has no cards. Race cards remain deferred until the core race is proven fun.

For every attribute, distinguish three decisions:

- **Meaning:** what the attribute represents and how players understand it. Agree this before the relevant system is specified, using the root glossary and established concept vocabulary.
- **Effect:** which choices, information or outcomes it changes. Validate through laboratory experiments and browser playtests as that system arrives.
- **Numbers:** scales, coefficients, individual ratings and prices. Treat these as tuning hypotheses until the system and its campaign interactions have been tested.

Stage 4 establishes the working attribute set needed for a complete weekend. Stage 6 establishes the personnel, construction and development attribute set needed for the complete team-management loop. Stage 7 finalises initial-release values; further tuning remains possible after release. Do not wait for the whole attribute catalogue before testing the browser race.

## Graphical direction and design system

Begin visual design before or alongside the Stage 2 browser slice. Use representative cards and playable screens to establish a consistent system; expand it as new capabilities arrive.

| Stage | Design and graphical work | Evidence to move forward |
| --- | --- | --- |
| 2. Browser race | Establish visual direction, typography, colours, spacing, reusable card anatomy, attribute displays, tyre/gap graphics, timing tower, decision controls and public/private information styling. Define loading, selected, locked, disabled and error states. Use representative driver cards, controller screens and a display view. | Players can read the race state, distinguish available choices and understand both cars' decisions. The same visual vocabulary works on the shared display and controller. |
| 3. Shared play | Validate TV viewing distance, phone layouts, tablet handoff, touch targets, contrast and accessible cues. Standardise interaction states across private, pass-and-play and open-family modes. | A household can use the interfaces on its actual devices, with readable information and clear handoffs. |
| 4. Full weekend | Establish the stylised track/car language and incremental Babylon.js race board. Develop presentation for qualifying, starts, overtakes, pits, incidents, Safety Cars and weather. Keep simulation time independent of animation time. | Graphics explain strategically important events and maintain the weekend duration target. |
| 5. Connected season | Extend the same system to calendar, standings, reports and asynchronous preparation. | Players understand campaign progress and what they need to do before the next event. |
| 6. Team management | Extend card and screen patterns to personnel offers, Scouts, budgets, car construction, development and Race Packages. Produce representative assets for every implemented card type. | Players can compare options and understand costs, uncertainty and compromises without excessive visual complexity. |
| 7. Complete game | Complete asset coverage, photographic-style card imagery, circuit visuals, animation polish, Simple/Full information views, onboarding and consistency checks. Validate graphical performance and browser/device compatibility. | Presentation is coherent across the whole campaign and works on supported household devices. |

The design system starts as a small reusable foundation, not an exhaustive component catalogue. Keep cards, text, menus and controls in Angular/DOM/CSS, with Babylon.js responsible for the race scene. The lightweight controller should not require the race-rendering bundle. Visual changes should be checked in representative TV, phone and shared-tablet flows throughout delivery.

## Balancing and validation

Balancing is a continuing workstream with increasing scope:

| Stages | Main balancing questions | Validation approach |
| --- | --- | --- |
| 1 | Do pace, tyres, track fit, traffic and pit timing produce competing strategies? Does controlled variability preserve explainable outcomes? | Deterministic controls, paired seeds, parameter sweeps and reproducible evidence reports. |
| 2–3 | Can people understand and execute those strategies? Is two-car management comfortable? Are decisions, handoffs and presentation paced appropriately? | Browser and household playtests alongside race experiments; measure decision and presentation time separately. |
| 4 | Do setup, qualifying risk, starts, reliability, Safety Cars and weather add meaningful choices without overwhelming the race? | Isolate each mechanic in the laboratory, then test complete weekends and representative player skill levels. |
| 5–6 | Do circuit variety, personnel prices, budgets, permanent gains and package choices support different season strategies? Can an early advantage become runaway accumulation? | Short campaign playtests, full-season simulations and comparisons of roster, car and investment philosophies. |
| 7 | Is the integrated game fair, varied and engaging across the full calendar? Are AI, assistance and information quality appropriate for the intended audience? | Repeated complete campaigns, broader season experiments and release tuning against clarity, pacing and strategic-diversity goals. |

Finalise individual card prices and ratings only after their effects and opportunity costs exist. Race-level tuning alone cannot establish personnel or development value. Each milestone should record supported conclusions and remaining uncertainties; Stage 7 consolidates the initial-release balance rather than starting balancing work.

## Suggested next specs

The clearest next move is **Browser dry-race vertical slice**, after the segment-model evidence is reviewed, supported by a small **Visual direction and design-system foundation** effort. Establish representative cards and screens before or alongside implementation, then use the playable loop to validate both the visual language and the strategic experience.

| Priority | Candidate spec | Purpose and boundary |
| --- | --- | --- |
| Before / alongside the slice | Visual direction and design-system foundation | Establish the visual direction and reusable card, attribute, timing-tower and decision-control patterns. Review representative TV and controller screens, using a driver card as the first card template. Keep the scope sufficient for the browser race and extend it through later milestones. |
| Next | Browser dry-race vertical slice | Connect Game.Simulation to an ASP.NET Core server and Angular display/controller via SignalR. Play one Race Phase, then a complete fixed-grid race with existing pace, pit and contextual battle choices. Control both team cars; use scripted opponents. Present exact public Classification, legitimate team estimates and concise causal event feedback. |
| Following | Shared sessions and controller ownership | Let multiple players join, claim teams, lock simultaneous decisions and use private, shared or open-family control. Establish player/team/device separation and public/private channels. |
| Following | Race continuity, AI and assistance | Persist and resume sessions; handle deadlines, reconnects and unavailable players; replace scripted fill-ins with fair rules-based AI; provide initial per-player recommendations and Guided choices using legitimate observations only. |
| Then | Weekend preparation, qualifying and start | Establish the car/driver/track attributes needed for meaningful setup, practice feedback and qualifying, then start the race from the resulting grid. Keep preset rosters and cars at first. |
| Then | Incidents, reliability and Safety Cars | Add contextual disruption, early phase termination and the simultaneous cheap-pit decision window. Prove the behaviour in laboratory experiments before presenting it live. |
| Then | Weather strategy | Add imperfect forecasts, evolving track conditions, weather tyres and reaction windows. Build on the interrupted-phase flow proven by Safety Cars. |

Review the boundary between shared sessions and continuity/AI after the browser slice. If it is smaller and clearer as one deliverable, combine them; if the slice exposes a pacing or comprehension problem, address that before adding new race systems.

The Stage 2 and Stage 3 specs are published in the local tracker and included in the written spec index above. Their status and implementation blockers are recorded in each spec.

## Remaining spec plan and estimate

Eighteen specs have been written, as listed in the written spec index. **No additional feature specs are currently required for the planned initial release.** The full planned scope is documented; the existing specs still have to be delivered according to their status and dependencies. Implementation and playtests may warrant splitting large specs or creating corrective work.

The former remaining areas are now published:

| Stage | Area | Additional specs | Published deliverables |
| --- | --- | --- | --- |
| 6 | Race Packages and richer development outcomes | 0 | Package library, creation/refinement, selection, compromises and Rush. |
| 7 | Full-calendar content, presentation and release readiness | 0 | Three specs cover content/cards, Babylon.js and information views, and integrated release validation. |
| | **Total still to write** | **0** | Optional splits and corrective work depend on implementation/playtest evidence. |

All planned milestone areas now have published specs in the written spec index. The next work is ticket breakdown and implementation along their actual dependency frontier, followed by measured validation and necessary corrections.

Implement the written specs around stable browser contracts and the relevant simulation evidence. Use browser playtest findings to sharpen the campaign and economy specs before publishing them. Correct pacing or comprehension problems before adding complexity when the findings warrant it.

Attribute definitions and effects, card treatments, design-system extensions, legitimate information and relevant balancing belong in each system's spec. The final content/release work completes their coverage and integrated validation; it does not postpone them until Stage 7. This avoids separate speculative attribute or balancing specs disconnected from playable systems.

Revisit the estimate at each milestone. Splitting a large deliverable may increase the document count without adding product scope; merging tightly related work may reduce it. Additional corrective work discovered in playtests is not included in this estimate.

## Later spec families

After the playable weekend, likely spec families are:

- **Campaign and championships:** saved season lifecycle, calendar, results, standings and asynchronous preparation.
- **Personnel and initial car construction:** unique personnel acquisition, limited offers and Scouts, team budget allocation and car philosophy.
- **Development and Race Packages:** one development decision per GP, permanent gains versus packages, refinement, Rush, outcomes and weekend locks.
- **Information and personnel effects:** richer engineer estimates, setup feedback, manager and technical staff contributions, fair recommendations and AI. Introduce each effect alongside the system it informs rather than adding bonuses at the end.
- **Content and presentation:** circuit data, roster data, photographic-style cards, stylised tracks/cars and event storytelling. Grow representative content first, then fill the calendar.
- **Design-system extensions:** add campaign, personnel, economy and development patterns as those systems arrive; complete visual coverage and graphical polish for release.
- **Season balance and release readiness:** full-season experiments, human playtests, onboarding, accessibility, supported devices and hosting operations.

These are broad areas to split into specs once their prerequisite milestones provide evidence.

## Delivery principles and dependencies

Keep the standalone simulation and evidence tooling as the foundation for every new race mechanic. Maintain deterministic replay and the distinction between designer diagnostics, public facts and team estimates throughout.

Use the locked technical stack: C# simulation, ASP.NET Core and SignalR, Angular client surfaces, Babylon.js race presentation, and the planned Firebase/Cloud Run persistence and hosting. The browser milestone starts locally; family hosting follows once shared play and recovery are useful. Verify operational limits and cost assumptions when deployment is specified.

Bring two-car management and readable decision feedback into the first browser slice. Bring pass-and-play, absence handling and initial assistance into shared play. These features are central to the intended audience and cannot wait for final polish.

Prove interrupted Race Phases with Safety Cars before adding weather. Establish effective attributes and setup before relying on qualifying or purchased upgrades. Prove saved campaign progression with presets before introducing personnel scarcity and development finances.

The full season is the destination, but a short campaign is the test vehicle. Apply season-wide balancing once the economy exists, while continuing race-level experiments and playtests at every stage. Introduce the Full information view after the Simple experience is understandable; both use the same engine.

Race cards, Red Flags, detailed driving physics and infrastructure for large-scale public multiplayer remain outside the initial delivery path. Revisit them only when the core game provides evidence that they improve the experience.

## Questions to resolve at milestone boundaries

- After the segment model: which effects materially improve strategic choices, and which remain numerical hypotheses?
- After the browser race: are five-lap laboratory boundaries appropriate for the player-facing cadence, and is managing two cars comfortable?
- After the first visual foundation: are cards, attributes, race information and controls readable on both the shared display and controller?
- After shared play: how do real players divide decision time, animation time and handoff time within the race target?
- Before qualifying: what attribute depth is needed now to make preparation and risk choices meaningful?
- Before the economy: what short campaign and representative circuits best expose specialisation, budget pressure and runaway advantages?
- Before release: do complete campaigns sustain engagement across days, and do the real deployment measurements support the family cost target?

These are validation and scope questions. They do not reopen locked concept decisions by default.

## Source baseline

- [Concept and systems foundation](f1_strategy_game_concept.md)
- [Locked technical architecture](f1_strategy_game_technical_architecture.md)
- [Dry-race evidence](../.scratch/dry-race-strategy-laboratory/evidence.md)
- The two current specs linked above.

The milestone ordering and future spec groupings are recommendations in this roadmap, not existing commitments.
