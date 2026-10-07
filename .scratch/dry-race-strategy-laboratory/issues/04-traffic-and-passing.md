# 04: Make traffic and track difficulty constrain overtaking

**What to build:** A designer can observe cars catching without automatically passing, losing time in traffic and rejoining battles after stops.

**Blocked by:** 03 — Execute pit strategies and enforce compound eligibility.

**Status:** resolved

- [x] Catching and overtaking are distinct. Following applies configurable time loss; overtaking depends on pace advantage, driver Racecraft and track passing difficulty.
- [x] Pit re-entry uses actual shared-clock traffic, rather than merely sorting unrestricted elapsed times.
- [x] Passing updates progress and Classification coherently, with no duplicate drivers or impossible ordering.
- [x] Lapping uses blue flags to permit leaders through at a small configurable time loss. Detailed backmarker battles remain deferred.
- [x] Both track fixtures demonstrate contrasting passing difficulty and track-position effects.
- [x] Reports include passes and traffic losses separately from Pit Loss; hidden diagnostic data remains distinct from public race information.
- [x] Deterministic scenarios demonstrate a faster car held behind another, overtaking, re-entry traffic and blue-flag handling.
- [x] Public-boundary tests verify these behaviours and preserve replay, phase and finish invariants.
