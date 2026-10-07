# 03: Resolve eligible passing opportunities

**What to build:** A following car attempts passes only at eligible zones, with reproducible encounter outcomes and contextual Battle Instructions.

**Blocked by:** 02: Resolve spatial catches and restricted following

**Status:** resolved

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [x] Define pair/eligible segment visit opportunities at entry in range or first catch; permit at most one draw per opportunity and a new opportunity on a subsequent visit.
- [x] Preserve passing opportunity identities across equivalent segment subdivision; recalculation cannot reroll a failed attempt.
- [x] Apply Attack/Defend and battle wear only to actual eligible encounters; report explicit blue flags separately.
- [x] Verify restricted following then eligible passing, repeated failed opportunities, nearby opponents and deterministic event history.


## Answer

Implemented eligible pair/zone/visit opportunities with cached results and identity-only PassingAttempt events. Entry in encounter range can establish an opportunity, while Overtake waits for physical catch. Shared PassingZoneId preserves equivalent subdivision, rejects non-equivalent/disjoint groups and retains visits across start/finish wrapping. Queues renew opportunities on later eligible visits; repeated recalculation does not reroll. Contact order follows resolved overtakes. Attack/Defend influence actual eligible encounters and charge battle wear once; lapping records separate blue flags without passing draws or battle wear.

Evidence: full suite passes 68 tests. Ten passing tests verify restricted-to-eligible passing, failed reuse and new visits, entry versus physical catch, subdivision invariance, explicit blue flags, contextual wear/Defend, wrapped visits, multiple opponents/replay, and metadata validation. Contract documentation is in docs/segment-simulation.md.
