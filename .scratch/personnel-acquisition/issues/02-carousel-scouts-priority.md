# 02: Complete rotating carousel picks and limited scouting

**What to build:** Acquire successive roster slots with fair scheduled priority and Scout replacements.

**Blocked by:** 01: Hire personnel with unique ownership and a shared budget.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Store seed-derived initial order, category rotation and alternating multi-slot traversal independent of join/submission speed; release unchosen reservations.
- [ ] Scout spends a token only when alternatives exist; return replaced offers to pool, prevent free reconnect rerolls and disclose insufficient alternatives without state change.
- [ ] Persist offers, draws, tokens and cursor before acknowledgement; retries/restart cannot sell a person twice or consume another token.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
