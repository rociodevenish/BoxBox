# 04: Run the Safety Car period and restart coherently

**What to build:** Resolve actual neutralised travel, reduced Pit Loss and a return to normal racing.

**Blocked by:** 03: Open a simultaneous Safety Car pit window.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Compress gaps through bounded shared-clock running toward following spacing, without teleportation or racing overtakes; document pit/retirement/blue-flag exceptions.
- [ ] Calculate Pit Loss from actual neutralised travel and service; preserve re-entry order, tyre/component use and continuing failure consequences.
- [ ] Keep lapped relative order without unlapping, show deficits, support finish under Safety Car and reopen normal decisions from current state without a restart tactic.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
