# F1 Strategy Game

A team strategy game in which players make simultaneous decisions at race phase boundaries.

## Language

**Race Phase**:
A span of race time governed by one set of simultaneous player decisions, with boundaries determined by the leader's completed laps.
_Avoid_: Turn, round

**Pace Instruction**:
A driver's Conserve, Standard or Push choice, applying throughout a Race Phase, including after a pit stop.

**Pit Instruction**:
A driver's choice to stay out or pit for a selected tyre compound. A pit choice takes effect at that driver's next lap-end after the Race Phase begins.

**Tyre Compound**:
A tyre category with distinct pace, warm-up and degradation characteristics. Dry compounds are Soft, Medium and Hard; Intermediate and Wet suit wetter conditions.

**Battle Instruction**:
A driver's Neutral, Attack or Defend choice for nearby battles during a Race Phase.

**Pit Loss**:
The time lost by making a pit stop compared with continuing on track, including pit-lane travel and stationary service.

**Classification**:
The race order ranked by laps completed, then progress within the current lap.

**Weekend Setup**:
A team's Aero, Mechanical and Tyres choices for both cars at one event. Setup redistributes performance through compromises.
_Avoid_: Upgrade, Race Package

**Practice Assessment**:
The one lightweight assessment of a confirmed provisional Weekend Setup, giving feedback for both drivers before final revision.
_Avoid_: Practice Session, Reroll

**Final-Ready**:
A team's confirmed preparation awaiting the start of qualifying. Its readiness is distinct from the qualifying lock.

**Qualifying Lock**:
The moment qualifying begins and every team's Weekend Setup and selected Race Package become fixed for the weekend.

**Driver Preference**:
A compatibility trait describing how a driver likes the car to behave. Adaptability moderates the effect of a mismatch.
_Avoid_: Rating, Aggression

**Effective Attributes**:
The supported performance inputs resulting from a car, driver and selected weekend context. Player-facing Simple groups summarize attributes separately.

**Campaign**:
A season of ordered Grand Prix weekends with continuing team ownership, driver identities and championship results.

**Campaign Calendar**:
The fixed ordered set of circuit events chosen for a campaign.

**Campaign Event**:
One scheduled Grand Prix weekend in a campaign, including its preparation and completed race result.

**Event Contribution**:
One completed campaign event's awarded points and eligible finishing results used in the championship standings.

**Constructors' Championship**:
The primary campaign competition, combining both drivers' awarded points for each constructor with the defined finishing-position tie-breaks.

**Drivers' Championship**:
The secondary campaign competition, accumulating awarded points and finishing-position tie-breaks for each driver across events.

**Personnel Acquisition**:
The preseason carousel in which teams hire unique people at fixed prices to fill their mandatory roster places.
_Avoid_: Auction

**Personnel Offer**:
A small reserved choice of people available to the current team for one roster place.

**Scout**:
A limited token spent to replace a Personnel Offer when alternatives are available, returning the replaced people to the pool.

**Credit Ledger**:
The record of a team's starting credits, income and spending, from which its remaining budget is determined.

**Completion Reserve**:
Credits kept available for a team's remaining mandatory personnel and minimum initial car construction.

**Base Car**:
A team's confirmed initial car, shared by both drivers, before permanent development, a Race Package and Weekend Setup are applied.
_Avoid_: Weekend Setup, Race Package

**Construction Draft**:
An editable choice of initial car options whose credits remain unspent until confirmation.

**Development Opportunity**:
A team's one between-event choice to save its credits or commit to permanent development, package creation or package refinement.

**Development Project**:
Committed paid work with a quoted cost, an uncertain outcome and an intended completion event. Delayed work remains the same project.

**Permanent Development**:
Lasting changes to a team's car capabilities, separate from its original Base Car, selected Race Package and Weekend Setup.

**Race Package**:
A coordinated specification of specialised strengths and compromises for both cars. Exactly one completed package version is selected for a Grand Prix.
_Avoid_: Weekend Setup, Component

**Package Library**:
A team's collection of Race Packages and their versions, retained as alternatives for later weekends.

**Package Refinement**:
Development work that creates a new version of an owned Race Package by strengthening it, reducing a compromise or broadening its usefulness.

**Rush**:
A higher-cost project choice that trades increased outcome risk for a better chance of reaching the intended event. Completion is not guaranteed.
