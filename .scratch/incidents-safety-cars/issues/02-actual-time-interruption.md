# 02: Interrupt a Race Phase at the actual incident time

**What to build:** Stop at a qualifying major incident with every car in coherent shared-clock state.

**Blocked by:** 01: Resolve contextual incidents and reliability consequences.

**Status:** resolved

## Acceptance criteria

- [x] Resolve earliest events and simultaneous-event ties directly without resolving then rolling back; return normal, Safety Car or finish completion reasons.
- [x] Preserve elapsed progress, committed queued/ongoing stops and authoritative Classification across mid-segment, lap-boundary, pit and finish scenarios.
- [x] Minor events remain local; finish precedence prevents unnecessary windows; resumed normal windows target the configured lap span from current leader progress.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Claim

Claimed by isolated implementer `codex/s09-02`, based on integration `a80009b0a33409169a7bd381c69b656f77308dd0`, 2026-10-18. Approved seam: public simulation phase/observation operations.

## Answer

Added opt-in actual-time major interruption to the existing earliest-event loop. All cars accrue elapsed exposure and trajectory history before consequences; ordinary timestamp events precede incident-only events, then existing contact order/stable driver identity breaks ties. The timestamp drains completely before returning. Finish and all-retired completion suppress the special-window identity. Queued and ongoing stops remain committed; continuation cannot repeat service/tyres or the consumed incident.

Incident-enabled results expose nullable completion reason, immutable interruption identity/time/driver/kind and frozen normal target. New normal windows anchor the active leader's fractional distance plus `NormalPhaseLaps` (default five), bounded by finish; fractional targets are event candidates. Integer crossings retain their original exact timestamps and legacy null-incident fields remain omitted. Major policy is explicit opt-in: spin, contact or mechanical failure qualify; lock-up remains local. Live risk remains disabled pending special-window and neutralised-running integration.

Ten focused interruption cases and 58 distinct existing incident/segment/traffic/pit/replay/start tests passed (68 unique tests). Eight representative saved v1/v2 full-field runs replayed byte-for-byte through the existing saved-file verifier. [Evidence and limits](../evidence/02-actual-time-interruption.md). No session/UI/persistence transition was introduced; those acceptance checks remain with tickets 03–05.
