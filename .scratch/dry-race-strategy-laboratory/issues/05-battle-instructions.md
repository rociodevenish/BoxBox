# 05: Influence nearby battles with Attack and Defend

**What to build:** A strategist can select contextual Battle Instructions and observe passing benefits and tyre costs during relevant battles.

**Blocked by:** 04 — Make traffic and track difficulty constrain overtaking.

**Status:** resolved

- [x] Offer Neutral, Attack and Defend where phase-start proximity qualifies, using tunable thresholds.
- [x] Battle Instructions last the phase and affect only relevant battles; running alone receives no unrelated battle effect.
- [x] Attack improves passing chances and Defend reduces the following car's chances, with tunable tyre costs.
- [x] Scripts and simulation requests validate instruction availability consistently and reject invalid choices clearly.
- [x] Simultaneous battle choices resolve reproducibly without one strategy reading another's locked decision.
- [x] Readable and structured reports identify relevant battle outcomes and tyre costs.
- [x] Public-boundary checks and controlled repeated runs demonstrate instruction effects, eligibility and irrelevant-battle behaviour without tests mirroring internal formulas.

Battle costs are charged per actual encounter, avoiding retrospective whole-lap proximity assumptions.
