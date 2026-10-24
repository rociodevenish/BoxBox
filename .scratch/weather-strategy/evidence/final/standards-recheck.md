# Standards recheck

Scope: prior Standards P2 and remediation diff `a2d7097d7ff74fee8aaaae0322eb6a3246bcb608...e1b25da26ad35d6c080f46a8750ba0f9aec9cf2e` only. Read-only inspection; no tests or services launched.

**Resolved:** The prior culture-dependent weather identity finding is fixed at `src/backend/Game.Simulation/RaceEngine.WeatherReactions.cs:49`. `FormattableString.Invariant` now wraps the complete identity, retaining round-trip time formatting. The regression compares literal fractional identities and full outcome hashes under en-US and de-DE and restores the original culture. This addresses the architecture §§10–11 deterministic replay requirement.

**Remaining:** No actionable Standards findings in the narrow remediation diff. The deferred-crossing change checks both remembered-band edges at the separation-eligible time before predicting another crossing, with public-boundary regression cases for flat/reversing rain and coincident boundaries. Emulator overrides preserve existing defaults and are documented consistently with owned verification configurations.

The remediation evidence records 24 unique focused checks and exact replay of all 560 archived weekends. These are reviewed evidence claims, not independently rerun checks. This recheck does not extend acceptance to pending household measurements or constitute a new whole-S10 review.
