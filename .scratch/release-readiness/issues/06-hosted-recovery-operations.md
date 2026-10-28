# 06: Rehearse hosted recovery, costs and safe updates

> GitHub issue: [#68](https://github.com/rociodevenish/BoxBox/issues/68) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Recover retained campaigns through hosted failures and rehearse deployment/rollback without losing committed play.

**Blocked by:** 01: Freeze a replayable integrated release candidate.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Exercise authenticated joins/subscriptions, revoked assignments, private payloads, storage failures, duplicate deadlines, expired windows, missed presentation and compatible save restores; no repeated points/spending/resolution.
- [ ] Document and rehearse backup/restore, migration, failed deployment, rollback and active-campaign handling; unsupported versions fail actionably and logs expose no credentials or player-hidden data.
- [ ] Measure Firestore requests/sizes, scheduling, Cloud Run startup/idle and family active costs/latency; configure observed limits, retention and actionable alerts without promising zero monthly cost.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
