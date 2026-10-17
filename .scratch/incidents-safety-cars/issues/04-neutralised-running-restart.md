# 04: Run the Safety Car period and restart coherently

**What to build:** Resolve actual neutralised travel, reduced Pit Loss and a return to normal racing.

**Blocked by:** 03: Open a simultaneous Safety Car pit window.

**Status:** resolved

## Acceptance criteria

- [x] Compress gaps through bounded shared-clock running toward following spacing, without teleportation or racing overtakes; document pit/retirement/blue-flag exceptions.
- [x] Calculate Pit Loss from actual neutralised travel and service; preserve re-entry order, tyre/component use and continuing failure consequences.
- [x] Keep lapped relative order without unlapping, show deficits, support finish under Safety Car and reopen normal decisions from current state without a restart tactic.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Implemented bounded neutralised travel on the shared event engine, explicit optional lane route/service timing, lapped physical-order preservation, failure/finish handling and normal fractional restart with active command/barrier membership. New typed travel replay v2 retains v1 designer staging verification. Rules and focused simulation/API/browser evidence: [04 evidence](../evidence/04-neutralised-running.md), [runtime policy](../../../docs/neutralised-running.md). Live risks stay disabled pending 05/evidence gates.
