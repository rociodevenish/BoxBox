# 01: Persist locks and recover an interrupted race

**What to build:** A household can resume a saved dry race after server restart with the same accepted choices and authoritative results.

**Blocked by:** Shared sessions and controller ownership, including its household verification; browser dry-race replay contracts.

**Status:** ready-for-agent

- [ ] Use a replaceable Firestore adapter and Firebase identities to save session metadata, assignments, settings, accepted locks, immutable complete decision sets, outcomes and presentation/recovery records. Keep cloud SDKs outside domain and simulation.
- [ ] Acknowledge locks only after durable recording and announce results only after committing outcomes; conditional revision writes prevent duplicate transition ownership.
- [ ] Retain command/control identities and schema/model/randomness versions; bound document/history size rather than assuming one document holds a race.
- [ ] Recover through engine initialization and replay of committed decisions, verifying recorded outcomes and measuring replay latency without serializing private engine fields.
- [ ] Recover crashes before outcome commit deterministically from the saved decision set; rebroadcast committed outcomes without a second logical resolution.
- [ ] Storage failures expose recoverable paused/error state, preserve choices and retry the same revision without false durable acknowledgements or uncommitted broadcasts.
- [ ] Reject incompatible recovery versions clearly while preserving saved inputs/results. Recovery enforces current device authority and public/private filtering.
- [ ] Through API/SignalR and real Auth/Firestore emulators, restart after locks, committed decision set, resolution and commit-before-broadcast; verify uninterrupted-equivalent outcomes, retry identities and failed-write handling.
