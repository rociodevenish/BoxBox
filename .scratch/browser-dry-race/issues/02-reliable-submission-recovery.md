# 02: Make submissions and recovery reliable

> GitHub issue: [#4](https://github.com/rociodevenish/BoxBox/issues/4) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** A player can retry, correct rejected input or refresh the controller without partially committing choices or advancing the race twice.

**Blocked by:** 01 — Play one authoritative Race Phase.

**Status:** resolved

- [x] Validate capability, session, driver ownership, phase/revision, complete two-car choices and engine legality before committing; reject the entire invalid submission without mutating race or lock state.
- [x] Serialize session mutations. Stable command identities return recorded acknowledgements for duplicates; stale or conflicting commands fail clearly.
- [x] Server-confirmed locks cannot be replaced by another submission, and duplicate/concurrent submissions resolve at most once.
- [x] Rejected choices remain editable; disconnected clients show unavailable actions rather than claiming successful submission.
- [x] Initial loading, refresh and reconnect retrieve current authorised state and recover locked/result states while the server remains running.
- [x] Clients ignore duplicate or older revisions; server restart presents a clear expired/unavailable session rather than claiming durability.
- [x] Exercise wrong-team, malformed, stale, duplicate and concurrent commands, notification ordering and refresh during decision/result states through API/SignalR and focused browser checks.

## Answer

Serialized session mutations and recorded normalized command fingerprints/acknowledgements make accepted retries idempotent and conflicting commands explicit. Own confirmed plans recover through authorized snapshots without leaking opponent choices. Connection status, stable pending UUIDs, rejected-draft recovery, snapshot rejoin and revision filtering keep browser actions truthful.

Verification: 108 simulation + 11 API/SignalR tests, 34 gallery tests and 6 real-server browser tests pass; Angular typecheck/build pass (265.58 kB raw / 69.57 kB transfer). [Evidence](../evidence/02-recovery.md) records actual interrupted transport/reordered wire tests; [run and recovery contracts](../../../docs/browser-dry-race.md) explain in-memory lifetime limits. Full-race continuation and polished component integration remain subsequent tickets.
