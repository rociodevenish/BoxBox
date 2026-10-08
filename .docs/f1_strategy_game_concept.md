# F1 Strategy Game — Concept & Systems Foundation

**Status:** Concept design baseline / pre-modelling specification  
**Version:** 1.0  
**Purpose:** Foundation document for implementation, simulation design, balancing, and future game design work.

---

## 1. Product Vision

This game is an online, season-long Formula 1 strategy game with a deliberate board-game feel, while using the computer to handle the complexity that would make a physical board game cumbersome.

The player does **not** directly drive a car. The player runs an F1 team across a full season and makes the important decisions that shape competitive performance:

- preseason personnel acquisition;
- initial car philosophy;
- development spending;
- permanent versus race-specific upgrades;
- weekend setup;
- qualifying risk;
- tyre strategy;
- pit timing;
- pace management;
- attack/defend decisions;
- reactions to Safety Cars, weather, traffic and incidents.

A single Grand Prix should allow a pleasant rhythm of decisions and feedback, with its duration determined by play rather than a fixed race or weekend time target. The full season unfolds over a number of days. Between races, players make asynchronous decisions about development and preparation.

The computer should handle exact lap-time simulation, probability and variability, tyre degradation, reliability, traffic, overtaking resolution, pit-stop timing, race gaps, weather evolution, incident generation, Safety Cars, and detailed attribute interactions.

The player should spend most of their time making meaningful choices rather than performing repetitive actions.

---

## 2. Core Design Principles

### 2.1 Decisions over actions
The game should favour a small number of consequential decisions over repeated management actions. A player who spends more time in the asynchronous phase should gain an advantage by **thinking more deeply and optimising better**, not by being allowed to perform more actions.

### 2.2 Controlled randomness, not randomness for its own sake
Random variability is important because races should not be deterministic, but the game should not use visible dice merely because it is board-game inspired. Randomness should emerge through race events such as lock-ups, slightly slow pit stops, successful or failed overtakes, tyre variation, component degradation and incidents.

### 2.3 Specialisation should be positive
Car construction should encourage players to ask **“Where can I be slightly better than most competitors?”** rather than **“How do I eliminate every weakness?”** Specialisation should generally create a larger advantage in its target area than the disadvantage it introduces elsewhere. Typical design shape: `+2 / +1 / -1`, not `+3 / 0 / -3`.

### 2.4 Relative performance matters
Because every team is making similar trade-offs, a small advantage can be strategically important. A car need not be dramatically better at Monza to benefit from a low-drag philosophy; it only needs to be a little better than the majority of the field in the characteristics Monza rewards.

### 2.5 One simulation, two information layers
There should be one simulation engine. Simple mode exposes broader grouped attributes and fewer setup decisions. Full mode exposes the detailed attributes underneath the same simulation. The two modes should not be separate games.

### 2.6 Personnel should improve choices and information
Better managers and engineers should frequently improve the quality of available choices, information accuracy, development efficiency, prediction confidence and execution reliability. They should not simply stack raw performance bonuses everywhere.

### 2.7 Avoid runaway accumulation
Development should create a mixture of small permanent gains, optional race packages and improved flexibility rather than allowing a leading team to accumulate huge advantages in every attribute.

---

# 3. Season Structure

A campaign represents **one full real-world Formula 1 calendar**.

The season loop is:

1. Preseason setup and personnel acquisition
2. Initial car construction
3. Grand Prix weekend
4. Post-race analysis
5. Between-race development decision
6. Race-package selection
7. Weekend setup
8. Next Grand Prix

This repeats through the full calendar.

## 3.1 Championships
Both championships matter:

- **Constructors' Championship** — primary player victory condition
- **Drivers' Championship** — important secondary championship

Each human controls both cars in their team.

## 3.2 Teams
Each player is assigned one of the eleven current F1 constructor identities. The constructor provides team identity, championship identity, livery/presentation and historical flavour. It does **not** force the real-world drivers, personnel or car characteristics.

---

# 4. Player Count and AI

The game should support up to 11 teams. Unclaimed teams are AI controlled. If a human becomes unavailable or fails to submit a required decision, the game should continue through recommendation auto-selection, temporary AI control or permanent AI takeover.

Default AI should target roughly **midfield competitiveness**. The championship front should mainly be contested by engaged human players. AI uses the same rules and systems as humans.

---

# 5. Game Modes

## 5.1 Simple / Race Mode
Designed for readability, mixed-age groups and fast decisions.

**Car:** Speed, Cornering, Mechanical Grip, Tyres, Reliability.  
**Driver:** Pace, Racecraft, Tyres, Consistency, Technical.

Weekend setup is **team-wide** and applies to both cars.

## 5.2 Full / Constructor Mode
Uses the same simulation but exposes detailed attributes and may allow per-driver weekend setup tuning.

---

# 6. Preseason Personnel Acquisition

The preseason personnel phase is intentionally **not** a full open auction economy. Instead it is fixed-price, somewhat randomised, competitive, limited-choice and easy to understand.

## 6.1 Competitive personnel
Real people exist as unique assets. If one team acquires a person, no other team can acquire them.

Competitive personnel include:

- Manager
- Drivers ×2
- Race Engineers ×2
- Technical / Design Engineers (approximately 2)

## 6.2 Limited-choice carousel
Players are shown a small number of available cards at a time, generally 2–3. Cards have printed prices within a relatively narrow band. The desired decision is **“Is this driver worth a couple more credits for my team?”**, not a high-friction open-market valuation exercise.

## 6.3 Scout tokens
Players receive limited Scout tokens that allow them to refresh poor options. Later draft positions may receive slightly more Scouts. Scouted cards return to the wider pool rather than being permanently destroyed.

## 6.4 Draft order
No player should have a permanent first-pick advantage. Starting priority should rotate or randomise across personnel categories. Randomised limited offers + rotating priority + modest Scout compensation should smooth draft-order effects.

---

# 7. Initial Car Construction

The car is **not competitively drafted**. Every player has access to the same car-construction choice set. Players build their initial car through compromise choices such as low drag, high downforce, mechanical grip, tyre preservation, reliability and aggressive power deployment.

No option should be objectively best. The starting car should usually remain viable everywhere while gaining small but meaningful advantages at selected circuit types. Extreme min-maxing should have diminishing returns.

---

# 8. Car Attributes

## 8.1 Simple Mode groupings

### Speed
- Top Speed
- Acceleration / Energy Deployment

### Cornering
- High-Speed Aero
- Low-Speed Aero

### Mechanical Grip
- Mechanical Grip
- Braking
- Traction

### Tyres
- Tyre Wear / Preservation
- Tyre Temperature Management

### Reliability
- Reliability

## 8.2 Detailed car attributes
The underlying simulation currently assumes approximately ten detailed car attributes:

1. Top Speed
2. Acceleration / Energy Deployment
3. High-Speed Aero
4. Low-Speed Aero
5. Mechanical Grip
6. Braking
7. Traction
8. Tyre Wear / Preservation
9. Tyre Temperature Management
10. Reliability

Exact ranges and weightings are deferred to programmatic modelling.

---

# 9. Driver Model

## 9.1 Simplified attributes
- Pace
- Racecraft
- Tyres
- Consistency
- Technical

## 9.2 Detailed attributes

### Pace
- Qualifying Pace
- Race Pace

### Racecraft
- Overtaking
- Defending
- Starts

### Tyres
- Tyre Preservation
- Tyre Warm-up

### Consistency
- Error Control
- Wet-Weather Skill

### Technical
- Setup Feedback
- Adaptability

## 9.3 Aggression
Aggression is **not** a separate rating. A driver's apparent aggression should emerge from underlying ratings and player tactical instructions.

## 9.4 Driver preferences
Preferences are compatibility traits, not quality ratings. Examples: Stable Rear, Strong Front End, Strong Braking Platform, High-Speed Confidence. Adaptability determines how strongly a driver is affected by a car/setup mismatch.

## 9.5 Unique traits
Use sparingly. Do not add a trait when existing stats already capture the behaviour.

---

# 10. Manager Model

Each team has one Manager card.

Simplified attributes:

- Leadership
- Operations
- Development
- Efficiency

Managers primarily improve choice quality, organisational execution and resource use rather than directly add raw car pace.

---

# 11. Race Engineer Model

Each team has two Race Engineers.

Simplified attributes:

- Strategy
- Setup
- Tyre Reading
- Communication

**Strategy:** improves tactical estimates such as pit windows and undercut viability.  
**Setup:** improves setup recommendation quality/confidence.  
**Tyre Reading:** improves tyre-condition and tyre-life estimates.  
**Communication:** reduces operational errors and improves instruction execution.

A strong engineer often provides **better information**, not free speed.

---

# 12. Technical / Design Engineer Model

Detailed attributes:

- Aerodynamics
- Mechanical / Chassis
- Power Systems
- Reliability
- Development Efficiency

They affect development direction, quality, risk, efficiency and information. A small number of technical personnel with different strengths should shape the team's engineering identity.

---

# 13. Pit Crew

Pit Crew is a **team attribute**, not a competitive draft card.

Underlying attributes:

- Pit Speed
- Pit Reliability

Simple Mode may expose these as one broad Pit Crew rating. They affect expected stop time, slow-stop probability, operational mistakes and double-stack execution.

---

# 14. Track Attributes

Tracks contain two information groups: **Performance Demands** and **Race Characteristics**.

## 14.1 Simplified performance demands
- Speed
- Cornering
- Mechanical Grip
- Tyres

## 14.2 Detailed performance demands

### Speed
- Top-Speed Demand
- Acceleration / Deployment Demand

### Cornering
- High-Speed Cornering Demand
- Low-Speed Cornering Demand

### Mechanical Grip
- Braking Demand
- Traction Demand
- Mechanical Grip Demand

### Tyres
- Degradation / Abrasion
- Thermal Demand

## 14.3 Race characteristics
- Overtaking Difficulty
- Pit-Lane Loss
- Safety-Car Risk
- Mechanical Stress
- Weather Volatility

Optional advanced characteristic: Track Evolution.

---

# 15. Development Philosophy

Between races, development is **decision-heavy rather than action-heavy**. Every player receives the same limited number of development decisions. More engaged players can spend more time analysing the same choices, but cannot perform more development actions simply by logging in more often.

## 15.1 Season budget
Teams receive a meaningful **starting budget** and only small budget increments during the season. Preseason spending therefore has major consequences. Expensive personnel reduce later development flexibility; economising preserves development potential.

---

# 16. Two Types of Development

## 16.1 Permanent Base-Car Development
Baked into the car and active at every race. Usually smaller, cheaper and lower-risk. Examples: +1 Top Speed, +1 Braking, +1 Tyre Preservation, +1 Reliability. Larger permanent concept changes may include compromises.

## 16.2 Race Packages
Specialised alternative specifications. A team may own multiple packages but may select **exactly one Race Package per Grand Prix**. A package represents a coordinated circuit-specific specification, not one physical component.

Example themes:

- Low-Drag Package
- High-Downforce Package
- Tyre Management Package
- Cooling / Reliability Package
- Mechanical Grip Package

The weekend car is:

> **Base Car + Permanent Development + ONE Race Package + Weekend Setup**

## 16.3 Package library strategy
Budget should support different philosophies: many modest packages, a few powerful packages, a strong base car with few packages, or a flexible library with a weaker baseline.

## 16.4 Package refinement
Existing packages can later be refined by increasing strength, removing compromise or broadening useful circuit range. Refinement consumes the same limited development opportunity as building something new.

---

# 17. Between-Race Decision Cadence

Between each Grand Prix each team receives:

- **one development decision**;
- Race Package selection;
- Weekend Setup preparation.

The development decision may be a minor permanent refinement, major permanent concept change, new Race Package, refinement of an existing Race Package, or potentially saving/researching depending on later modelling.

---

# 18. Development Risk and Rush

Development can resolve as normal success, partial success, breakthrough, compromised result or delay rather than binary pass/fail.

A compromised package should usually remain usable but carry an extra cost or weakness.

**Rush** is a key compromise: complete a project in time for the next GP at higher budget/risk, potentially creating a compromised or less reliable package.

---

# 19. Weekend Setup

Weekend Setup is the final free optimisation layer before qualifying. It redistributes performance rather than permanently improving the car.

## 19.1 Simple Mode
One team-wide setup for both cars across three axes:

- **Aero:** Speed ↔ Downforce
- **Mechanical:** Responsive ↔ Stable
- **Tyres:** Warm-Up ↔ Preservation

## 19.2 Full Mode
May allow separate tuning per driver.

## 19.3 Practice feedback
Flow:

1. Provisional setup
2. Lightweight simulated practice
3. Feedback
4. Final setup lock

Practice is not a separate playable session.

Race Engineer Setup and Driver Setup Feedback improve recommendation quality/confidence. Driver Preferences and Adaptability affect fit.

Race Package and final Weekend Setup lock when qualifying begins.

---

# 20. Qualifying

Qualifying uses Q1 / Q2 / Q3 and targets **4–5 minutes** total.

For each remaining driver in each session the player chooses exactly one approach:

- **SAFE** — lower ceiling, lower risk
- **PUSH** — normal maximum effort
- **ALL-OUT** — higher potential, higher variance/error risk

There are **no qualifying cards**.

Qualifying performance combines car/track fit, Driver Qualifying Pace, Weekend Setup, driver/car compatibility, engineer preparation/information, selected approach, controlled hidden variability and mistakes/errors.

All players choose simultaneously.

---

# 21. Grand Prix Structure

The race is **phase-based**, not lap-by-lap from the player's perspective.

Each phase normally represents **5–8 laps**. A race typically contains **8–10 phases**. The computer simulates individual laps internally.

## 21.1 Exact race state
At phase end the game maintains/displays:

- exact order;
- gap to leader;
- gap to car ahead;
- tyre compound;
- tyre age;
- estimated tyre condition;
- pit status;
- relevant car condition;
- weather information.

Exact positions and time gaps are fundamental. Undercuts and overcuts emerge from actual timing simulation rather than special moves.

---

# 22. Race Start

There is **no player race-start strategy decision**.

The game automatically resolves and animates the opening using grid position, Driver Starts, Racecraft, Error Control, traction/launch characteristics, circuit characteristics and controlled randomness.

The start should be presented as a fun event sequence before showing the updated exact classification.

A major opening-lap incident can immediately trigger a Safety Car or other special-event state before Phase 1.

---

# 23. Core Race Phase Decisions

## 23.1 Pace
For each driver:

- CONSERVE
- STANDARD
- PUSH

Conserve trades pace for tyre/reliability benefit. Push trades tyre/component/error exposure for pace.

## 23.2 Pit
For each driver:

- Stay Out
- Pit + choose compound

Undercut and overcut are emergent outcomes, not buttons.

## 23.3 Battle instruction
Only when relevant:

- Neutral
- Attack
- Defend

Attack/Defend should not clutter phases when the driver is running alone.

## 23.4 Simultaneous decisions
All teams lock phase decisions simultaneously. Opponent choices are hidden until resolution.

---

# 24. Phase Simulation and Presentation

The computer simulates internal laps in detail. Routine randomness remains hidden. The player watches a short animated/key-event sequence highlighting overtakes, pit stops, slow stops, undercut/overcut outcomes, tyre changes, lock-ups, spins, contact, mechanical problems, weather changes and Safety Car triggers.

After the animation, the timing tower updates to the exact new race state.

---

# 25. Early Phase Termination

A normal phase may end early for strategically important events, including:

- Safety Car
- major weather change
- Red Flag if added later

If an incident occurs partway through a planned phase, the simulation can stop at that point so players can react immediately.

---

# 26. Safety Cars

Safety Cars create a special decision window.

When triggered:

- gaps compress according to simulation;
- track position remains important;
- effective pit loss falls substantially;
- players receive a quick Stay Out / Pit + compound decision.

Choices remain simultaneous.

---

# 27. Weather

Weather is deliberately simplified. High-level rules:

- forecasts are probabilistic;
- Weather Volatility belongs to the track;
- meaningful weather changes can end a phase early;
- tyre choice becomes a special decision point;
- information is imperfect but fair.

Detailed weather progression is deferred to modelling.

---

# 28. Systems Deferred to Programmatic Modelling

The following are intentionally not numerically specified yet:

- tyre compounds and exact degradation curves;
- tyre pace differentials;
- overtaking formulas;
- following/traffic effects;
- pit-stop distributions;
- component wear;
- incident probability;
- reliability failures;
- damage;
- Safety-Car frequency;
- detailed weather progression.

Their conceptual requirements are fixed: tyres matter heavily; exact gaps matter; undercuts/overcuts emerge; track overtaking difficulty matters; Push/Conserve have consequences; incidents arise from context; reliability matters across race and season.

---

# 29. Cards

Race cards are **parked for later**. The core race must first be fun without them.

Current baseline:

- no qualifying cards;
- no required race cards;
- no explicit dice mechanics.

If cards return later, they should modestly modify existing decisions rather than replace core actions.

---

# 30. Player Assistance / Help Privileges

Assistance is configured **per player** at game setup and changes how choices are presented, not simulation performance.

## Standard
Full choice set; no strategic recommendation.

## Assisted
All choices visible; sensible recommendation highlighted and explained.

## Guided
Recommendation for every major decision; clearly poor choices may be hidden; sometimes only the two most sensible options are shown; plain-language trade-offs are emphasised.

Assistance may use only information legitimately available to that player's team. It must not access opponents' locked decisions, exact hidden tyre condition, future incidents/Safety Cars or hidden competitor information.

Optional setting: **auto-accept recommendation if timer expires**.

Assisted players receive no championship/budget penalty.

---

# 31. Information Model

## Known facts
Examples:

- exact position;
- exact visible time gaps;
- tyre compound;
- tyre age;
- own package/setup;
- championship points.

## Estimated information
Examples:

- exact tyre condition;
- remaining competitive tyre life;
- weather progression;
- opponent pit window;
- competitor package details;
- component failure risk.

Personnel quality narrows uncertainty.

---

# 32. Budget Model

Exact numbers are deferred to balancing.

Locked philosophy:

- starting budget is strategically important;
- preseason spending strongly constrains later development;
- in-season budget increments are modest;
- teams cannot freely undo poor preseason finances;
- budget forces trade-offs between small permanent gains, package creation, package refinement, rushing and saving.

---

# 33. Accessibility and Time Targets

- Preseason should not require spreadsheet-like valuation work.
- Between races, a casual player can act quickly; an enthusiast can analyse the same limited choices more deeply.
- Qualifying target: **4–5 minutes**.
- Race pacing: aim for a pleasant experience; no fixed duration target. Measure decision, waiting, resolution and presentation time to identify friction.
- Full GP weekend duration follows the playable experience rather than a fixed total. The former 20–25 minute race estimate informed the design and was retired by the user on 9 October 2026.

---

# 34. High-Level Domain Model

## Season
Calendar, current event, teams, standings, configuration, assistance settings, AI configuration.

## Team
Constructor identity, Manager, Drivers ×2, Race Engineers ×2, Technical Engineers, Pit Crew, budget, Base Car, Permanent Developments, Package Library, selected Race Package, weekend setup, championship points.

## Driver
Detailed attributes, Simple Mode groupings, preferences, optional unique traits, season statistics.

## Manager
Leadership, Operations, Development, Efficiency, optional unique effect.

## Race Engineer
Strategy, Setup, Tyre Reading, Communication, optional unique effect.

## Technical Engineer
Aerodynamics, Mechanical/Chassis, Power Systems, Reliability, Development Efficiency, optional specialisation.

## Car
Base detailed attributes, grouped attributes, permanent modifiers, selected Race Package modifiers, weekend setup modifiers, reliability/component state.

## Race Package
Identity, theme/category, positive modifiers, compromises, development history, refinement state.

## Development Project
Type, cost, permanent/package classification, intended effects, risk profile, personnel influence, rush state, outcome.

## Track
Performance demands, race characteristics, pit-lane loss, weather parameters, mechanical stress, overtaking difficulty, Safety-Car risk.

## Race State
Current lap/phase, track/weather/Safety-Car state, classification, exact elapsed time per driver, gaps, tyre state, component state, pit history, incidents, pending decisions.

---

# 35. High-Level State Flow

`SeasonSetup`
→ `PreseasonPersonnel`
→ `InitialCarBuild`
→ `EventPreparation`
→ `RacePackageSelection`
→ `WeekendSetupInitial`
→ `PracticeFeedback`
→ `WeekendSetupLock`
→ `QualifyingQ1`
→ `QualifyingQ2`
→ `QualifyingQ3`
→ `RaceGrid`
→ `RaceStartAnimation`
→ `RacePhaseDecision`
→ `RacePhaseSimulation`
→ `RacePhaseResult`
→ repeat / special event
→ `RaceFinish`
→ `ChampionshipUpdate`
→ `PostRaceReport`
→ `DevelopmentDecision`
→ next event.

Special states may include `SafetyCarDecision`, `WeatherDecision`, later `RedFlag`, and `PlayerTimeoutResolution`.

---

# 36. Programmatic Modelling Roadmap

## Stage 1 — Static performance model
Build `Car + Driver + Track -> Expected Pace` and establish attribute scales/track weightings.

## Stage 2 — Single-lap simulation
Simulate 22 cars for one lap with performance, track fit, small variability and basic traffic.

## Stage 3 — Multi-lap phase simulation
Simulate 5–8 laps while preserving exact elapsed times and positions.

## Stage 4 — Tyres and pace instructions
Implement compounds, degradation, warm-up and Conserve / Standard / Push.

## Stage 5 — Traffic and overtaking
Implement following effects, Attack/Defend, overtaking difficulty, Racecraft interaction and exact position changes.

## Stage 6 — Pit stops
Implement pit loss, pit crew variation, tyre changes, re-entry traffic and emergent undercut/overcut behaviour.

## Stage 7 — Incidents and reliability
Implement lock-ups, spins, contact, damage, failures, Safety Cars and phase interruption.

## Stage 8 — Weather
Implement simplified forecast, weather/track state, tyre suitability and early phase termination.

## Stage 9 — Qualifying
Implement Q1/Q2/Q3, Safe/Push/All-Out, cut-line estimates and setup/driver interaction.

## Stage 10 — Development and economy
Implement starting budget, modest season income, permanent improvements, package creation/refinement, development risk, Rush and personnel influence.

## Stage 11 — AI and assistance
Implement midfield AI, recommendation engine, Guided/Assisted/Standard modes and timeout auto-selection.

## Stage 12 — Full-season simulation and balancing
Run large Monte Carlo samples to test championships, personnel value, car specialisation, development ROI, tyre strategy, undercut strength, Safety-Car impact, AI strength and budget choices.

---

# 37. Key Questions for Programmatic Balancing

The simulator should help answer:

- How much lap-time advantage should one detailed attribute point represent?
- What is a healthy spread between best and worst preseason cars?
- How much should an elite driver be worth relative to an elite engineer?
- How much faster should Push be than Standard?
- What tyre/reliability cost should Push carry?
- How often should a 1–2 second undercut succeed?
- How difficult should overtaking be at different circuits?
- How much stronger should a Race Package be than a permanent refinement?
- How quickly should tyres degrade?
- How often should Safety Cars occur?
- How much should reliability vary across a season?
- How much development budget should remain after an expensive preseason roster?
- How many meaningful developments should a team afford over a season?
- How valuable should strong engineers be without becoming mandatory?
- How strong should assistance recommendations be relative to experienced humans?
- How strong should default AI be relative to average human play?

These are tuning questions, not unresolved concept decisions.

---

# 38. Locked Decisions Summary

- One full real-world F1 calendar per campaign.
- Eleven constructor identities.
- Constructors' Championship is primary; Drivers' Championship also matters.
- Constructor identity does not force real-world drivers or car performance.
- Personnel acquisition is competitive, limited-choice, fixed-price and partially randomised.
- Car construction is equal-access and compromise-driven.
- One detailed simulation supports Simple and Full modes.
- Simple car groups: Speed, Cornering, Mechanical Grip, Tyres, Reliability.
- Simple driver groups: Pace, Racecraft, Tyres, Consistency, Technical.
- Aggression is not a separate driver stat.
- Driver preferences are compatibility traits.
- Manager: Leadership, Operations, Development, Efficiency.
- Race Engineer: Strategy, Setup, Tyre Reading, Communication.
- Technical Engineer: Aero, Mechanical/Chassis, Power, Reliability, Development Efficiency.
- Pit Crew is a team attribute, not a draft card.
- Starting budget is important and only modestly replenished.
- One development decision between each GP.
- Permanent development and Race Packages are distinct.
- Permanent development is always active.
- Exactly one Race Package may be selected per GP.
- Weekend Setup is separate from the Race Package.
- Simple Mode uses one team-wide weekend setup.
- Setup axes: Aero, Mechanical, Tyres.
- Practice gives feedback before setup lock.
- Q1/Q2/Q3 qualifying.
- Qualifying choice: Safe / Push / All-Out.
- No qualifying cards.
- No explicit dice mechanics.
- Race start is automatically simulated and animated.
- Race phases are roughly 5–8 laps, with roughly 8–10 phases per race.
- Exact positions and gaps are maintained.
- Core race decisions: Pace, Pit, context-sensitive Attack/Defend.
- Decisions are simultaneous.
- Phase simulation shows animated key-event feedback.
- Phases may end early for Safety Cars or significant weather changes.
- Safety Cars create a special cheap-pit decision window.
- Race cards are deferred until the core race model is proven fun.
- Weather is intentionally simplified.
- Assistance is per player: Standard / Assisted / Guided.
- Guided assistance may reduce choices to two sensible options.
- Assistance never receives illegitimate hidden information.
- AI uses the same rules and targets roughly midfield competitiveness.
- Tyres, overtaking, pits, incidents, reliability and exact numeric values are designed during modelling.

---

# 39. Design North Star

The game should naturally produce stories such as:

> We deliberately spent less on our second driver so we could afford a better technical team.

> Our base car is not the fastest, but our low-drag package gives us a real chance at Monza.

> We sacrificed qualifying tyre warm-up for better race preservation.

> We qualified P6 but our tyre strategy is stronger than the cars ahead.

> The leader stayed out. We pitted into clear air and the undercut might work.

> A Safety Car has changed everything. Do we give up track position for fresh tyres?

> We rushed this package to make the race, and now we must decide whether its reliability compromise is worth running.

> Our engineer thinks the tyres have another phase in them — but how confident are we?

> A younger player can follow a clear recommendation while an experienced player sees the same decision and reasons about deeper trade-offs.

The player's satisfaction should come from seeing **earlier strategic decisions collide with the changing conditions of a race**.

That is the core game.
