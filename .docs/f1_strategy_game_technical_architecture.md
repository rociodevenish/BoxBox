# F1 Strategy Game — Technical Architecture

**Status:** Foundation / Locked Technical Direction  
**Audience:** Developers and future contributors  
**Purpose:** Define the initial architecture for building the F1 strategy game described in the companion game-concept document.

> This document captures technical decisions that are considered **locked for the first implementation** unless later modelling or prototyping demonstrates a clear reason to change them.

---

## 1. Architectural Goals

The technical architecture must support the core playstyle of the game:

- Web-native play with no required native app install.
- Primary living-room experience on a shared TV or large screen.
- Optional phones/tablets/laptops as private controllers.
- Shared-device / pass-and-play support for younger players.
- Optional open-family mode where decisions can be made directly on the TV.
- Rich 2D photographic-style cards and event presentation.
- Stylised, primitive 3D tracks and cars with a premium digital-board-game aesthetic.
- Server-authoritative simulation.
- Deterministic, testable race modelling.
- Asynchronous between-race decisions.
- Live synchronous qualifying and Grand Prix sessions.
- Family-first deployment that can run at approximately $0/month under modest usage.
- Clean upgrade path if the project ever becomes larger than a family game.

The most important architectural principle is:

> **The simulation creates the race. The server owns the truth. The TV tells the story. The controller makes decisions.**

---

# 2. High-Level System Architecture

```text
                                  ┌─────────────────────────┐
                                  │     Firebase Hosting    │
                                  │  Angular / PWA clients  │
                                  └────────────┬────────────┘
                                               │
                    ┌──────────────────────────┼─────────────────────────┐
                    │                          │                         │
                    ▼                          ▼                         ▼
          ┌──────────────────┐       ┌──────────────────┐      ┌──────────────────┐
          │   TV / Display   │       │ Player Controller│      │ Shared Controller │
          │                  │       │                  │      │ / Pass-and-Play   │
          │ Babylon.js       │       │ Angular UI       │      │ Angular UI        │
          │ Broadcast UI     │       │ Private Info     │      │ Multiple Teams    │
          └────────┬─────────┘       └────────┬─────────┘      └────────┬─────────┘
                   │                          │                         │
                   └──────────────────────────┼─────────────────────────┘
                                              │
                                        HTTPS / SignalR
                                              │
                                              ▼
                                  ┌─────────────────────────┐
                                  │    Google Cloud Run     │
                                  │                         │
                                  │ ASP.NET Core / .NET     │
                                  │ SignalR                 │
                                  │ Game Application Layer  │
                                  │ Authoritative State     │
                                  │ Deterministic Simulation│
                                  └────────────┬────────────┘
                                               │
                           ┌───────────────────┼────────────────────┐
                           │                   │                    │
                           ▼                   ▼                    ▼
                 ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
                 │ Cloud Firestore  │  │ Firebase Auth    │  │ Static Assets     │
                 │ Persistent State │  │ Accounts/Guests  │  │ Hosting / Storage │
                 └──────────────────┘  └──────────────────┘  └──────────────────┘
```

---

# 3. Client Surfaces

The game has multiple client surfaces, but they all communicate with the same authoritative backend.

## 3.1 Shared TV / Display Client

The TV is the primary shared game board.

It is responsible for:

- Grid presentation.
- Qualifying session presentation.
- Track view.
- Timing tower.
- Driver positions and gaps.
- Tyre graphics.
- Race-start animation.
- Phase-resolution animation.
- Overtakes.
- Pit-stop events.
- Incidents.
- Safety Cars.
- Weather events.
- Championship results.
- Shared pre-season presentation.
- Shared development / weekend presentation where appropriate.

The TV should feel closer to a **live F1 broadcast merged with a premium digital board game** than to a traditional management-game dashboard.

The TV client may also become a controller in open-family mode.

---

## 3.2 Private Controller Client

A phone, tablet, laptop, or similar browser-based device may act as a private pit-wall controller.

It is responsible for displaying information such as:

- Private tyre estimates.
- Engineer advice.
- Available race decisions.
- Qualifying decisions.
- Development decisions.
- Race-package selection.
- Weekend setup.
- Help / Guided recommendations.
- Private budget and personnel details where appropriate.

The controller should be a lightweight Angular application and should **not** need to download the Babylon.js race-rendering bundle.

---

## 3.3 Shared Device / Pass-and-Play

A single controller device may control multiple teams.

Example:

```text
Family Tablet
 ├── Mercedes — Child 1
 ├── Ferrari — Child 2
 └── McLaren — Child 3
```

After one team locks a decision, the device shows a handoff screen:

```text
PASS TO FERRARI

[ READY ]
```

Only after `READY` is selected should the next team's private information appear.

This is a first-class feature, not an edge case.

---

## 3.4 Main-Screen Decisions

The TV may also be used to make decisions directly.

This is intended for:

- casual family play;
- younger players;
- households without enough personal devices;
- non-competitive sessions where hidden strategy is not important.

Privacy mode should therefore be a game/session setting.

Suggested modes:

### Competitive
All strategic decisions are private until locked.

### Family
Decisions may be shown and made openly on the TV.

### Mixed
Each team may use either private or shared/open control.

---

# 4. Player, Team, and Device Identity

A core rule is:

> **A player is not a device, and a team is not a device.**

The backend should model these concepts separately.

Example:

```text
User / Guest
    │
    └── Device Session
           ├── Team A
           ├── Team B
           └── Team C
```

A device session may be authorised to submit commands for one or more teams.

This enables:

- one phone per player;
- multiple players sharing a tablet;
- parent-assisted play;
- TV-controlled teams;
- pass-and-play;
- future remote multiplayer.

---

# 5. Frontend Technology

## 5.1 Angular

Use Angular for:

- application shell;
- routing;
- state presentation;
- cards;
- menus;
- timing tower;
- championship tables;
- setup/development UI;
- controller UI;
- accessibility;
- responsive layouts;
- PWA support.

The frontend should be structured as either:

- separate Angular applications for Display and Controller; or
- separate entry points/routes within a shared workspace.

Preferred initial structure:

```text
frontend/
  apps/
    display/
    controller/

  libs/
    game-ui/
    game-contracts/
    cards/
    timing-tower/
    session-client/
    shared-components/
```

The Display application may depend on the 3D rendering layer.

The Controller application should not.

---

# 6. 3D Rendering

## 6.1 Babylon.js

Use Babylon.js for the shared race board.

Babylon.js should render:

- stylised 3D circuit;
- cars;
- pit lane;
- simple trackside geometry;
- camera movement;
- weather effects;
- Safety Car;
- race-event animations.

The visual target is intentionally **not photorealistic F1 simulation**.

The intended style is:

> primitive / stylised 3D board-game pieces + highly polished 2D presentation.

This keeps rendering lightweight and gives the project a distinctive identity.

---

## 6.2 Do Not Render the Full UI in Babylon.js

Babylon.js is only responsible for the 3D board and race presentation.

Angular/DOM/CSS remains responsible for:

- text;
- cards;
- driver/manager photographs;
- timing tower;
- tyre information;
- overlays;
- menus;
- event banners;
- decisions;
- setup/development screens.

This preserves:

- crisp typography;
- responsive design;
- accessibility;
- maintainability;
- easy localisation;
- good mobile support.

---

## 6.3 Graphics Compatibility

WebGL compatibility is the baseline.

WebGPU may be used opportunistically where available, but it must not be required.

The game should remain practical on:

- desktop browsers;
- tablets;
- modern phones;
- low-power living-room PCs;
- browser-capable TV devices.

The graphics are intentionally simple enough that the experience should not depend on high-end GPU hardware.

---

# 7. Data-Driven Tracks

Tracks should be defined by data rather than hard-coded scene logic.

A track definition may contain:

```text
TrackDefinition
    Id
    Name
    CentreLine
    TrackWidth
    PitLane
    StartFinishLine
    SectorMarkers
    CameraPoints
    OvertakingZones
    SceneryAnchors
    GridSlots
    PitBoxes
```

The display builds the board from this definition.

This keeps new circuits primarily an asset/data task rather than a code task.

---

# 8. 2D Assets

Rich photographic content should be normal web assets.

Examples:

- driver images;
- manager images;
- engineer images;
- personnel cards;
- event artwork;
- championship screens;
- track artwork;
- weather/event overlays.

Preferred image formats:

- AVIF where appropriate;
- WebP;
- PNG where transparency is required.

Do not bake card faces into 3D textures unless there is a specific visual reason.

---

# 9. Backend Technology

Use:

- ASP.NET Core;
- current supported .NET runtime;
- SignalR;
- strongly typed domain/application services.

The backend is responsible for:

- game state;
- session state;
- permissions;
- player/team/device mapping;
- timers;
- hidden information;
- recommendations / Help Privileges;
- qualifying resolution;
- race simulation;
- development resolution;
- championship state;
- persistence coordination;
- realtime broadcasting.

The client must never be authoritative for race outcomes.

---

# 10. Simulation Engine

The simulation engine must be a separate deterministic C# library.

Suggested solution structure:

```text
src/
  Game.Domain/
  Game.Simulation/
  Game.Application/
  Game.Api/

tools/
  SimulationRunner/
```

`Game.Simulation` must not depend on:

- Angular;
- HTTP;
- SignalR;
- Firebase;
- Firestore;
- SQL;
- Babylon.js;
- browser APIs.

Conceptually:

```csharp
PhaseResult ResolvePhase(
    RaceState state,
    IReadOnlyList<DriverDecision> decisions,
    SimulationContext context);
```

This allows the same simulation engine to power:

- the production game;
- automated race tests;
- balance testing;
- Monte Carlo runs;
- replay/debug tooling.

---

# 11. Deterministic Hidden Randomness

Visible dice mechanics are not part of the game.

Randomness still exists internally.

Use seeded deterministic randomness.

Given:

```text
InitialState
+ PlayerDecisions
+ SimulationSeed
```

the engine must produce the same outcome.

Benefits:

- reproducible bugs;
- exact race replay;
- deterministic automated tests;
- simulation regression testing;
- safe server recovery;
- easier balancing.

The seed remains hidden from players.

---

# 12. Simulation Time vs Presentation Time

The simulation should resolve a phase quickly.

The TV then presents the result over a longer animation sequence.

Example:

```text
Resolve 6 laps
    ↓
PhaseResult returned immediately
    ↓
TV spends 10–15 seconds presenting:
    overtakes
    pit stops
    lock-ups
    incidents
    gap changes
    weather event
```

The presentation timeline must never become authoritative game logic.

---

# 13. Phase Result Contract

A resolved phase should conceptually return two products.

```text
PhaseResult
│
├── AuthoritativeState
│     Classification
│     ExactGaps
│     TyreStates
│     CarStates
│     Weather
│     Damage
│     RaceControlState
│
└── PresentationTimeline
      OvertakeEvent
      PitStopEvent
      LockupEvent
      IncidentEvent
      MechanicalEvent
      WeatherEvent
      FastLapEvent
```

The Controller application mostly consumes authoritative/team-specific state.

The TV consumes both the public state and the presentation timeline.

---

# 14. Explicit Game State Machine

The backend should model the game as an explicit state machine.

Approximate flow:

```text
PRESEASON
    ↓
BETWEEN_RACES
    ↓
RACE_PACKAGE_SELECTION
    ↓
WEEKEND_SETUP
    ↓
QUALIFYING_Q1
    ↓
QUALIFYING_Q2
    ↓
QUALIFYING_Q3
    ↓
RACE_START
    ↓
RACE_DECISION
    ↓
PHASE_RESOLUTION
    ↓
RACE_DECISION
      ↕
SAFETY_CAR_DECISION
      ↕
WEATHER_DECISION
    ↓
RACE_FINISHED
    ↓
BETWEEN_RACES
```

This state machine should be enforced server-side.

Examples of invalid commands:

- changing a race package during Q2;
- modifying weekend setup after qualifying lock;
- submitting race pace during phase animation;
- making a development decision during a Grand Prix.

---

# 15. Race Start

The start contains no player strategy decision.

The server resolves it automatically using the simulation model.

The TV presents it as a special animated sequence.

Example:

```text
LIGHTS OUT

Russell launches well from P4.
Leclerc is slow away.
Verstappen attacks into Turn 1.
Contact further back — no significant damage.

END OF LAP 1
```

The exact classification and gaps are then shown before normal race-phase decisions begin.

A major opening-lap incident may immediately trigger a Safety Car / special-event decision state.

---

# 16. Race Phases

A normal race phase represents approximately 5–8 laps.

Players make a small number of decisions.

Typical choices:

- pace;
- pit / stay out;
- tyre compound if pitting;
- attack / defend only where contextually relevant.

All competitive decisions are submitted simultaneously.

The server then resolves the phase and returns:

- exact classification;
- exact time gaps;
- tyre states;
- race events;
- any special-event interruption.

Major events may terminate the phase early.

Examples:

- Safety Car;
- significant weather change;
- Red Flag if implemented later.

---

# 17. Realtime Communication

Use SignalR for live game communication.

Do not use Firestore realtime listeners as the primary race transport.

Suggested conceptual channels/groups:

```text
game:{gameId}:public

game:{gameId}:team:{teamId}

game:{gameId}:host
```

### Public channel

May include:

- classification;
- exact visible gaps;
- public tyre data;
- weather;
- phase state;
- countdowns;
- public events;
- championship state.

### Team-private channel

May include:

- private tyre estimates;
- engineer advice;
- help recommendations;
- decision state;
- development information;
- private budget;
- hidden technical estimates.

### Host channel

May include:

- session administration;
- player/device assignment;
- pause/resume;
- team control changes;
- assistance settings.

---

# 18. Help Privileges

Help Privileges are per-player/team settings.

Suggested levels:

### Standard
No strategic recommendations.

### Assisted
All choices shown, but recommended options are highlighted and explained.

### Guided
Recommendations are given on every major decision. Clearly weak options may sometimes be removed, leaving two sensible choices.

Important rule:

> The Help system may only use information the player/team could legitimately know.

It may not use:

- hidden opponent decisions;
- future Safety Cars;
- exact hidden tyre data;
- hidden simulation outcomes.

If the timer expires, the game may optionally auto-accept the recommended decision.

---

# 19. Firebase Authentication

Use Firebase Authentication for identity.

Support:

- permanent accounts for game owners/hosts;
- anonymous/guest identities for family players.

Children should not require permanent accounts.

A guest joins via:

- QR code;
- join code;
- direct session link.

Example:

```text
JOIN CODE: A7FC
```

The backend then maps the authenticated/guest session to authorised team(s).

---

# 20. Deployment Topology

## 20.1 Frontend

Use Firebase Hosting for:

- Angular/PWA application;
- display client;
- controller client;
- standard static assets.

Reasons:

- globally distributed static hosting;
- automatic HTTPS;
- simple deployment;
- suitable for SPA/PWA hosting;
- very low family-scale cost.

---

## 20.2 Backend

Deploy ASP.NET Core to Google Cloud Run.

The Cloud Run service hosts:

- HTTP API;
- SignalR;
- game application services;
- authoritative state coordination;
- simulation engine.

SignalR should connect **directly to Cloud Run**.

Do not route the persistent SignalR/WebSocket connection through Firebase Hosting.

---

## 20.3 Family Cloud Run Configuration

Initial family configuration:

```text
Minimum instances: 0
Maximum instances: 1
CPU/RAM: modest
```

Benefits:

- scale-to-zero when not playing;
- effectively no idle compute cost;
- no distributed SignalR complexity;
- simple authoritative runtime.

Cold starts are acceptable for a private family game.

---

# 21. Firestore Persistence

For the family-first implementation, use Cloud Firestore instead of Cloud SQL.

Primary reason:

> Avoid a continuously running relational database and its baseline monthly cost.

Firestore is persistence, not the realtime race engine.

Suggested high-level structure:

```text
games/{gameId}

games/{gameId}/players/{playerId}

games/{gameId}/teams/{teamId}

games/{gameId}/season/{seasonId}

games/{gameId}/races/{raceId}

games/{gameId}/races/{raceId}/phases/{phaseId}

games/{gameId}/events/{eventId}
```

Exact structure may evolve during implementation.

The domain model should not depend directly on Firestore types.

Persistence must be abstracted so another store can replace it later.

---

# 22. Authoritative Persistence Boundaries

Do not continuously persist every animation frame or every visual update.

Persist at meaningful authoritative transitions.

Example phase flow:

```text
RaceState
+
LockedDecisions
    ↓
ResolvePhase()
    ↓
Persist:
    decisions
    simulation seed
    phase result
    new authoritative state
    important events
    ↓
Broadcast
```

This means that if the display disconnects halfway through an animation, the race state is already safe.

On reconnect, the display may:

- replay the presentation timeline; or
- jump directly to the latest authoritative state.

---

# 23. Event / History Records

The initial architecture does not require full event sourcing.

However, maintain an append-only history of significant game events.

Examples:

```text
DriverSelected
ManagerSelected
DevelopmentChosen
DevelopmentResolved
RacePackageSelected
WeekendSetupLocked
QualifyingSessionResolved
RaceStarted
RacePhaseStarted
DecisionSubmitted
RacePhaseResolved
SafetyCarDeployed
WeatherChanged
RaceCompleted
ChampionshipUpdated
```

This supports:

- audit/debug;
- race replay;
- season history;
- analytics;
- future storytelling.

---

# 24. Static Assets

Initially, static assets may live on Firebase Hosting.

Examples:

```text
/assets/tracks/monza.glb
/assets/tracks/monaco.glb
/assets/cars/car-01.glb
/assets/drivers/...
/assets/events/...
```

If the library becomes large or needs independent publishing, move heavyweight media to:

- Google Cloud Storage;
- CDN-backed asset delivery.

This should not require architectural changes to the game.

---

# 25. Environment Strategy

If/when needed, use separate Firebase/GCP projects:

```text
f1game-dev
f1game-staging
f1game-prod
```

Prefer separate environments over large numbers of environment flags inside a single project.

For a purely private family prototype, it is acceptable to begin with only:

```text
dev
prod
```

or even a single non-critical environment until deployment becomes stable.

---

# 26. CI/CD

Preferred CI/CD system:

**GitHub Actions**

Suggested pipeline:

```text
Pull Request
 ├── dotnet test
 ├── simulation regression tests
 ├── frontend tests
 ├── Angular build
 └── optional Firebase preview deploy

Main
 ├── build/test
 ├── build backend container
 ├── push to Artifact Registry
 ├── deploy Cloud Run
 └── deploy Firebase Hosting
```

Later, release workflows may add:

- versioning;
- production approvals;
- database migration steps if a relational database is introduced.

---

# 27. Simulation Regression Testing

The simulation runner should be usable in CI.

Example:

```text
simulate-races --runs 10000
```

Potential assertions:

- pole-to-win rate stays within expected range;
- overtaking frequency by track remains plausible;
- Safety Car frequency remains within target ranges;
- tyre strategies remain varied;
- stronger drivers outperform weaker drivers over large samples;
- no setup choice becomes universally dominant;
- AI remains approximately midfield strength.

This is a core reason to keep the simulation independent of the web application.

---

# 28. Family-Scale Cost Target

The initial deployment should be designed to be effectively free under modest family usage.

Expected family-scale costs:

| Service | Expected Cost |
|---|---:|
| Firebase Hosting | ~$0 |
| Firebase Authentication | ~$0 |
| Firestore | ~$0 |
| Cloud Run | ~$0 under modest usage/free allowance |
| SignalR | part of Cloud Run |
| Babylon.js | $0 |
| Domain name | optional |

A billing-enabled Firebase / Google Cloud project may still be required for Cloud Run even if usage remains within free allowances.

Budget alerts should be configured.

---

# 29. Cost Protection

For a private family deployment:

- Cloud Run min instances = 0;
- Cloud Run max instances = 1;
- modest CPU/RAM;
- Firebase/GCP budget alerts;
- authenticated access;
- avoid public anonymous API surfaces where unnecessary;
- avoid Firestore-driven live race subscriptions;
- avoid unnecessary logging volume;
- avoid oversized media bundles.

The game workload is favourable because rendering happens in the browser and the server resolves only discrete game-state transitions.

---

# 30. Scaling Path

If the project becomes larger than a family game, the architecture should be able to evolve without replacing the simulation/domain model.

Potential upgrades:

## Persistence

Replace Firestore with:

- PostgreSQL;
- Cloud SQL;
- another relational store.

The domain/application layers should interact through persistence abstractions so this remains an infrastructure change.

## Realtime Scaling

Move from one Cloud Run instance to multiple instances and introduce:

- Redis/backplane;
- equivalent shared realtime coordination;
- distributed locks/state coordination where necessary.

## Assets

Move large media libraries to:

- Cloud Storage;
- dedicated CDN.

## Authentication

Expand beyond anonymous/family identity to:

- full user accounts;
- social sign-in;
- league ownership;
- remote multiplayer invites.

None of these upgrades should require rewriting `Game.Simulation`.

---

# 31. Suggested Repository Structure

```text
/
├── docs/
│   ├── game-concept.md
│   └── technical-architecture.md
│
├── src/
│   ├── backend/
│   │   ├── Game.Domain/
│   │   ├── Game.Simulation/
│   │   ├── Game.Application/
│   │   ├── Game.Infrastructure.Firebase/
│   │   └── Game.Api/
│   │
│   └── frontend/
│       ├── apps/
│       │   ├── display/
│       │   └── controller/
│       │
│       └── libs/
│           ├── game-contracts/
│           ├── session-client/
│           ├── game-ui/
│           ├── cards/
│           ├── timing-tower/
│           └── shared-components/
│
├── tools/
│   └── SimulationRunner/
│
├── tests/
│   ├── Game.Domain.Tests/
│   ├── Game.Simulation.Tests/
│   ├── Game.Application.Tests/
│   └── Simulation.RegressionTests/
│
└── .github/
    └── workflows/
```

---

# 32. Recommended First Technical Milestone

Do **not** begin by building the full multiplayer UI.

The first milestone should prove the architecture from the inside out.

## Milestone 1

Create:

- `Game.Domain`;
- `Game.Simulation`;
- `SimulationRunner`.

Define initial:

- Driver;
- Car;
- Track;
- RaceState;
- PhaseDecision;
- PhaseResult.

Then implement:

```text
Car + Driver + Track
        ↓
Expected Pace
        ↓
Single Lap
        ↓
Multi-Lap Phase
        ↓
Exact Classification / Gaps
```

Run this from the command line with no frontend.

Only once the underlying simulation is useful should the project add:

1. backend API;
2. SignalR;
3. basic timing-tower display;
4. 3D board;
5. controller interfaces.

This keeps graphics and networking from hiding simulation problems.

---

# 33. Recommended Early Integration Milestone

After the simulation foundation:

```text
Display Browser
      │
      │ SignalR
      ▼
Cloud Run / local Game.Api
      │
      ▼
Game.Simulation
```

Implement one minimal Grand Prix phase:

1. display timing tower;
2. controller chooses `Conserve / Standard / Push`;
3. decisions lock;
4. server resolves five laps;
5. display receives exact new gaps;
6. display animates a small presentation timeline.

If this feels good, the core architecture is validated.

---

# 34. Locked Technical Decisions Summary

The following are considered locked for the initial build:

- Web-native/PWA first.
- Shared TV is the primary public game board.
- Phones/tablets/laptops are optional controllers.
- Shared-device pass-and-play is first-class.
- TV decisions are supported in open/family mode.
- Player/team/device identity are separate.
- Angular for frontend application/UI.
- Babylon.js only for stylised 3D race presentation.
- DOM/CSS for cards, text, menus, overlays and decision UI.
- ASP.NET Core/.NET backend.
- SignalR for live communication.
- Server-authoritative game state.
- Deterministic standalone C# simulation library.
- Hidden seeded randomness.
- Simulation time separated from animation/presentation time.
- Explicit server-side game state machine.
- Public and private realtime channels.
- Firebase Hosting for frontend.
- Firebase Authentication for permanent and guest users.
- Cloud Run for ASP.NET Core + SignalR.
- SignalR connects directly to Cloud Run.
- Firestore for family-scale persistence.
- SignalR, not Firestore listeners, drives live race state.
- Persist authoritative state at phase/state boundaries.
- Cloud Run min instances = 0.
- Cloud Run max instances = 1 initially.
- Expected normal family hosting cost approximately $0/month.
- GitHub Actions preferred for CI/CD.
- Architecture must preserve a clean upgrade path to relational persistence and horizontally scaled realtime infrastructure.

---

# 35. Guiding Principle

Whenever an implementation decision is ambiguous, prefer the solution that preserves these boundaries:

> **Domain rules do not know about infrastructure.**

> **Simulation does not know about presentation.**

> **Presentation does not determine results.**

> **Devices do not own teams.**

> **The server is authoritative.**

> **The TV is a shared board.**

> **Controllers are optional decision surfaces.**

Maintaining those boundaries is more important than any individual framework or cloud-service choice.
