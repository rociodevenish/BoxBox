# 05: Influence nearby battles with Attack and Defend

**What to build:** A strategist can select contextual Battle Instructions and observe passing benefits and tyre costs during relevant battles.

**Blocked by:** 04 — Make traffic and track difficulty constrain overtaking.

**Status:** ready-for-agent

- [ ] Offer Neutral, Attack and Defend where phase-start proximity qualifies, using tunable thresholds.
- [ ] Battle Instructions last the phase and affect only relevant battles; running alone receives no unrelated battle effect.
- [ ] Attack improves passing chances and Defend reduces the following car's chances, with tunable tyre costs.
- [ ] Scripts and simulation requests validate instruction availability consistently and reject invalid choices clearly.
- [ ] Simultaneous battle choices resolve reproducibly without one strategy reading another's locked decision.
- [ ] Readable and structured reports identify relevant battle outcomes and tyre costs.
- [ ] Public-boundary checks and controlled repeated runs demonstrate instruction effects, eligibility and irrelevant-battle behaviour without tests mirroring internal formulas.
