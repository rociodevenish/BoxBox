# 04: Play in Family and Mixed privacy modes

> GitHub issue: [#88](https://github.com/rociodevenish/BoxBox/issues/88) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** A household chooses intentionally open TV decisions for designated teams while private teams keep their own information boundaries.

**Blocked by:** 02 — Lock multiple teams and resolve together.

**Status:** resolved

- [x] Competitive mode uses private controllers; Family supports designated open teams; Mixed permits private, shared-tablet and open control per team.
- [x] Lobby and active screens clearly identify intentional openness and distinguish spectator display access from an authorised TV controller assignment.
- [x] Open views expose only the designated team's legitimate information and legal choices, never hidden diagnostics or another team's private estimates/selections.
- [x] Authorised TV control submits the same atomic team command as private controllers; spectator displays cannot issue commands.
- [x] Extend the Broadcast system with readable phone/tablet/TV privacy and ownership states and accessible labels.
- [x] Verify open and Mixed browser flows and all public/private/open HTTP/SignalR projections, including competitive-team isolation.


## Comments

Claimed by ticket04 on codex/shared-04 from integration ec108eb. Approved seams: real HTTP/SignalR and browser household flows. Actual human feedback remains pending under ticket06.

## Answer

Added an explicit read-only Open team projection and SignalR subscription. Only approved designated Open assignments in Family/Mixed mode expose their own existing team observations and accepted two-car plan; Competitive, Private and SharedTablet teams remain inaccessible through this projection. The wire contract excludes player/device authority and hidden diagnostics, and publication filters Open assignments under the coordinator gate. The unchanged public race projection contains Classification/readiness without private estimates or choices.

The lobby labels intentional openness and distinguishes an open spectator display from an authorised TV controller. Controller links are based on current authenticated device metadata rather than ownership guesses. The TV controller reuses the existing two-car controller, command/revision checks, atomic team command and playback/progression flow. Its privacy label comes from authoritative assignment metadata. Spectators cannot submit even when they know a device ID. The additive open spectator page reuses the Broadcast timing tower, driver-card component and existing stable portrait manifest mappings; no simulation inputs or originals were changed.

## Evidence

- Vertical TDD at approved public boundaries: the initial API test failed with 404 before the Open endpoint existed, then passed through real HTTP/SignalR and authentication-emulator requests. The Mixed browser tracer failed on the missing intentional-open lobby state, then passed after integration. A corrected combobox locator preceded that genuine browser red.
- Five focused Open API cases pass: strict top-level/car payload allowlists, designated-team observations, denied private/Competitive/SharedTablet access, read-only unauthenticated subscription, private-lock isolation, designated Open accepted choices, and spectator submission rejection. Record: `.tools/reports/open-api-command.json`.
- Verified implementation SHA `9a92811d24d522787b2b12eb7630ade2090ec678` includes integration ticket03 (`f9c982f4931c146522c80a3ba21a7f36caff59a3`). Affected verification run `.tools/reports/verification/runs/2026-10-10T11-51-40-941Z-2aa5d9ec-097d-4488-ad3e-a020e60b4c90/result.json` passed frontend typecheck/build, 39 API tests, 110 simulation tests, both existing lobby browser flows and both Family/Mixed browser flows. The shared-tablet browser assertion still expected its former privacy-label text; updating it to the intended explicit private shared-tablet label passed the focused rerun in `.tools/reports/handoff-label-command.json` (PID 10856, exit 0). Five unique household browser flows are therefore green across the affected run and focused correction. No full regression suite was unnecessarily repeated.
- Browser flows verify a designated Open team with a separate Private team, explicit lobby/active openness, a fresh spectator's failed controller access, actual assigned-device submission, waiting for the remaining team, refresh and accepted Open instructions. Family submits on a 1920x1080 TV viewport; Mixed completes the 360px phone controller flow. Production build retains the existing initial-bundle and qrcode CommonJS warnings.
- Inspected [360px Mixed controller](../evidence/mixed-tv-controller-360.png): both cars, privacy/ownership label and confirmation are contained, with no horizontal overflow. The complete phone flow submits both cars atomically and waits for the other team.
- Inspected [1920x1080 Mixed open spectator](../evidence/mixed-open-spectator-1920.png): all 22 public timing rows and both designated-team observation cards fit. Estimates, legal choices, participant/constructor identity and the spectator-authority explanation remain visible. An initial over-tall capture was corrected before the affected regression pass; the browser now checks the last timing row fits within 1080px.
- Inspected [1920x1080 Family TV controller](../evidence/family-tv-controller-1920.png): the authoritative Open label and two car columns are clear. The reused controller scrolls to strategic controls; the Family browser flow confirms a plan on this TV viewport.
- Node 24.19 and repository .NET SDK were used. Tests owned UI/API ports 4474/4574 and emulator ports 19099/19400/19450. Bounded command records retain PIDs, elapsed times and exits; all launched services stopped and the test lease was returned to root. No preview service is intentionally retained. Worktree/dependency-junction cleanup is delegated to the integration owner after merge.
- Actual human household playtest feedback remains pending under ticket06 by user direction; automated browser evidence does not replace that feedback.
