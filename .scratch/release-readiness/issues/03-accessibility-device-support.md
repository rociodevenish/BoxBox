# 03: Complete accessible flows and document supported devices

> GitHub issue: [#65](https://github.com/rociodevenish/BoxBox/issues/65) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Complete core game flows with keyboard, touch, text alternatives and usable household display layouts.

**Blocked by:** 02: Teach a household to join and make its first decisions.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Review toward WCAG 2.2 AA for semantics, contrast, focus order/visibility, targets, errors and reduced motion; fix material defects and report actual coverage/limits.
- [ ] Test real phone/tablet/TV hardware, long names, dense standings, cards, supplied imagery/fallbacks and viewing distance; publish supported browser/device matrix.
- [ ] Measure controller/display bundle, load, frame and memory workloads, preserve renderer isolation and choose graceful quality/fallback behaviour; add regression checks only for observable defects.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
