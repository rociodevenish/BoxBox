# 01: Join a household lobby and claim a team

**What to build:** A host creates a family lobby and players join as accounts or guests, claim teams and prepare a complete grid.

**Blocked by:** Browser dry-race vertical slice, including its verified session contracts.

**Status:** resolved

- [x] Extend the existing session coordinator and frontend; keep host/player identities, teams, device sessions and assignments separate.
- [x] Firebase Authentication supports permanent hosts and anonymous/guest players with a documented local emulator path and server-side identity validation.
- [x] Join links, codes and QR codes locate the lobby and request membership without granting host/team authority or exposing private credentials.
- [x] Up to eleven teams can be claimed atomically; concurrent claims have one winner and clear feedback. Claims preserve preset personnel/performance and constructor identity.
- [x] Hosts can approve/correct assignments and assign shared-device participants before start; unclaimed teams use scripted opponents and the human/scripted set freezes at race start.
- [x] Lobby settings identify Competitive, Family and Mixed modes and intended control/privacy assignments.
- [x] Verify capacity, concurrent claims, wrong-session requests and unauthorised host commands through public API/SignalR behaviour and rendered lobby flows.

## Comments

2026-10-09: Claimed by implementer in codex/shared-01, based on codex/shared-sessions. Applying TDD at the approved HTTP/SignalR and real-browser boundaries; ticket 02 owns active multi-team race decisions.


## Answer

Implemented authenticated lobby creation/joining, public code/link/QR, separate player/device/constructor identities, atomic claims, host approvals/corrections and shared participants. Host start freezes human/scripted teams and initializes the existing engine. Real emulator, HTTP/SignalR and browser checks pass; all solo race regressions remain green. See [evidence and limitations](../evidence/01-lobby.md) and [local run procedure](../../../docs/shared-sessions.md). Active multi-team decisions remain ticket 02.
