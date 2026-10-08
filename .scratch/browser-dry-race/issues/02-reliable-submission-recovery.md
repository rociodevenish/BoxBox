# 02: Make submissions and recovery reliable

**What to build:** A player can retry, correct rejected input or refresh the controller without partially committing choices or advancing the race twice.

**Blocked by:** 01 — Play one authoritative Race Phase.

**Status:** ready-for-agent

- [ ] Validate capability, session, driver ownership, phase/revision, complete two-car choices and engine legality before committing; reject the entire invalid submission without mutating race or lock state.
- [ ] Serialize session mutations. Stable command identities return recorded acknowledgements for duplicates; stale or conflicting commands fail clearly.
- [ ] Server-confirmed locks cannot be replaced by another submission, and duplicate/concurrent submissions resolve at most once.
- [ ] Rejected choices remain editable; disconnected clients show unavailable actions rather than claiming successful submission.
- [ ] Initial loading, refresh and reconnect retrieve current authorised state and recover locked/result states while the server remains running.
- [ ] Clients ignore duplicate or older revisions; server restart presents a clear expired/unavailable session rather than claiming durability.
- [ ] Exercise wrong-team, malformed, stale, duplicate and concurrent commands, notification ordering and refresh during decision/result states through API/SignalR and focused browser checks.
