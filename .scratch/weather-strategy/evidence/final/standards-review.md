# Standards review

Reviewed `51c18a5e8db556372ca4021ad9dfc75dac5d8fd6...a2d7097d7ff74fee8aaaae0322eb6a3246bcb608` read-only against AGENTS.md, its referenced agent documentation, GLOSSARY.md, and both locked design documents. No ADR directory exists. No tests or services launched.

## Documented breach

- **P2 — Make weather reaction identities culture invariant.** `src/backend/Game.Simulation/RaceEngine.WeatherReactions.cs:40` interpolates `time:R` through the process current culture. A fractional reaction time produces, for example, `123.5` under en-US and `123,5` under de-DE. This identity is part of the authoritative `PhaseResult` (`RaceEngine.cs:937`), which is serialized into outcome hashes (`BrowserReplay.cs:40–41`, `AutomaticRaceStart.cs:18`). Replaying unchanged saved inputs on a host with another culture therefore reports a different outcome. Rule: technical architecture §§10–11 requires a deterministic engine and the same outcome for identical initial state, decisions and seed, supporting exact replay and recovery. Format the complete identity with invariant culture, as CircuitWeather already does for its numeric identity components.

## Judgement calls

No additional material Fowler-smell findings. Similar compound-band checks are small and currently serve distinct recommendation/presentation purposes; splitting existing coordinator partials or compact formatting alone is not a documented violation.
