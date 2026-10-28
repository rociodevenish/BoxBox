# 06: Verify household play across devices

> GitHub issue: [#90](https://github.com/rociodevenish/BoxBox/issues/90) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** A household can follow documented local setup and complete shared dry-race play with measured findings about joining, privacy and handoffs.

**Blocked by:** 05 — Manage host controls and controller access; visual foundation 06 — Verify and document the integrated foundation.

**Status:** ready-for-human

- [x] Document local multi-device networking and authentication-emulator setup without requiring production hosting.
- [x] Verify two private controllers, several participants on one tablet, authorised open TV control and Mixed mode across the integrated race flow.
- [ ] Record actual household findings about joining friction, privacy understanding, handoff time and both-car management.
- [x] Inspect representative phone/tablet/TV layouts, handoff clearing and readiness/host controls with accessible labels.
- [x] Run relevant API/SignalR and browser checks, frontend build/type checks and existing simulation regressions using the existing engine.
- [x] Record limitations clearly: absent-player waiting, in-process recovery only, and no deadlines, AI takeover or assistance yet.

## Automated answer

The LAN/emulator guide and automated verification are complete. See [automated findings and limitations](../evidence/06-household.md) and [completion records](../evidence/06/household-verification.json). All 219 unique automated cases pass across the single broad run and affected corrections, plus frontend typecheck/build. The real household playtest remains pending by explicit user direction; joining friction, privacy understanding, handoff time and both-car usability have not been measured. Independent review's related shared-tablet late-ack correction is assigned to the final integration review worker and recorded separately in the evidence.
