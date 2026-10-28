# 02: Play, skip and replay race highlights

> GitHub issue: [#59](https://github.com/rociodevenish/BoxBox/issues/59) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Follow starts, passing, stops and supported disruption highlights with equivalent text and exact final reconciliation.

**Blocked by:** 01: Display a representative authoritative race board.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Animate recorded anchors where available and identify compressed highlights as illustrative; never invent continuous positions or causal undercut claims.
- [ ] Support start, pit/service/re-entry, retirement, Safety Car and weather cues with bounded dense/quiet phase treatment; simulation and playback clocks remain separate.
- [ ] Skip/reduced motion/replay yields the same committed results and information, without applying effects or opening windows; server progression never waits for display acknowledgement.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
