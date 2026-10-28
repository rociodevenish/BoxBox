# 05: Recover opening and mid-race disruptions

> GitHub issue: [#30](https://github.com/rociodevenish/BoxBox/issues/30) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Handle opening incidents and server interruption through the same durable Safety Car flow.

**Blocked by:** 04: Run the Safety Car period and restart coherently; qualifying-race-start 03.

**Status:** resolved

## Acceptance criteria

- [x] Route major automatic-start incidents into the existing interruption/window path before the first normal decisions; retain grid/start replay.
- [x] Commit incidents, locks and neutralised results before broadcast; restart or duplicate callbacks cannot repeat damage, stops or gap compression.
- [x] Complete browser opening/mid-race flows with retirement, both-car choices, refresh, reduced motion and exact Classification after presentation.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Opening incidents now enter the same durable simultaneous Safety Car window before normal choices while retaining frozen grid/start replay. Versioned operation dispatch, historical staging migration and conditional-save reconciliation preserve one logical incident/stop/neutralised result. Public events retain observed incidents through automatic continuation, with factual Damaged/Retired status and readable reduced-motion controls. Production activation remains unregistered pending ticket 06 evidence and per-circuit authored routes. See [checks and captures](../evidence/05-opening-recovery.md).
