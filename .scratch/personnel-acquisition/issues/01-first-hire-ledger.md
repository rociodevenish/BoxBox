# 01: Hire personnel with unique ownership and a shared budget

**What to build:** Enter preseason and make a fixed-price hire from a small offer with an atomic budget and roster update.

**Blocked by:** campaign-championships 01; weekend-preparation 01.

**Status:** done

## Acceptance criteria

- [x] Establish the sole integer-credit ledger with fixed starting credit and immutable debits; purchase commits unique ownership, slot assignment and debit atomically.
- [x] Validate a versioned provisional personnel pool/prices and mandatory one Manager, two Drivers, two Race Engineers and two Technical Engineers; Pit Crew stays a team attribute.
- [x] Show two-or-three reserved offers, printed prices and remaining funds; reject stale, unauthorised and unaffordable commands with no partial effects and recover accepted hires.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.


## Answer

Implemented opt-in acquisition campaigns with the frozen95-person pool, three reserved Manager offers, current-device authority, one atomic immutable integer-credit ledger/ownership/slot transaction and durable command retries. Existing preset campaigns remain the default. Domain3, real API3 and focused phone/browser1 checks pass. [Evidence and boundaries](../evidence/01-first-hire.md). Rotating progression, Scouts and completion reserves remain tickets02–03; active personnel consumers remain04.
