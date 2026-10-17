# Ticket 03 evidence

Claim: `codex/s09-03`, based on ticket 02 integration `f87bdb117d66263301c04a5b52a132bcead63f11`. Approved seams: public simulation, session HTTP/SignalR and rendered browser. See [runtime rules](../../../docs/safety-car-window.md).

Red/green: the first public simulation test failed because the narrow choice/option seam did not exist. Implementation then passed legal queue, atomic invalid replacement and retired/ongoing commitment cases. The public API test caught retry validation incorrectly checking newly changed legality before the saved acknowledgement; the fix preserves the original acknowledgement before revalidating a new command.

Unique focused checks (no broad regression run):

- Simulation: 13 passing, comprising 3 new `SafetyCarChoiceTests` and 10 existing `InterruptionTests`. Bounded record `.tools/reports/s09-03/simulation-command.json`.
- API: new special-window/retry/recovery and shared-device/deadline cases passed against isolated Auth19369/Firestore19360 (the new-host store in these tests is the conditional in-process store; external Firestore disruption recovery remains ticket 05). `.tools/reports/s09-03/api-command.json`, custom `.tools/reports/s09-03/firebase.json`.
- Additional API check: 4 passing across special typed designer replay/no-live-team and existing committed-stop checks, `.tools/reports/s09-03/api-replay-command.json`. Final strict unknown-field/typed replay check: 2 passing, `.tools/reports/s09-03/api-final-command.json` (same two new cases, not additional unique tests). Total unique API coverage is 3 new cases plus 2 existing committed-stop cases.
- Frontend typecheck passed: `.tools/reports/verification/runs/2026-10-17T20-05-26-920Z-e80d6a1c-42e8-4d98-a888-144016b3efe2/result.json`.
- Two focused rendered browser tests cover free-car choices, queued commitment, all-owned-retired barrier, refresh, real private/public subscriptions and reduced motion. UI4523/API4623; final bounded record `.tools/reports/s09-03/browser-complete-command.json`.

Reviewed captures: [360px free-car flow](safety-car-360.png), [360px committed stop](queued-360.png), [360px retired cars](retired-360.png), [1920×1080 public display](safety-car-1920.png). Phone screenshots include the complete scrollable two-car flow. Controls/text fit without horizontal overflow; both identities, compound buttons and committed/retired notices are readable. Public display keeps incident status and authoritative Classification visible with reduced motion. Existing wide-screen timing CSS hid section headings; the Safety Car heading now remains visible. The special phone layout omits the crowded nearby timing table. Full-page desktop capture extends below 1080px for the existing development context card; no live playtest claim is made.

Owned process/cleanup records: emulator root PID10820 (55.355s, passed, emulator shutdown logged); simulation PID7572, final strict API PID392, final browser PID4996. All completed successfully with owned process-tree cleanup. Ports19369/19360/19570/19620/9160 were refused if already occupied; UI/API launchers also refuse existing listeners. Final listener check confirmed all seven listed preview/emulator ports closed; commit identity is recorded in the completion message. No global process termination, cloud credentials, push or PR.

Limitations: `safety-car-ready` is a staging boundary for ticket 04, not a finished usable neutralised race. Risks remain disabled in default live presets. Existing 90-second decision plus shared-device allowance remains provisional; duration/comprehension evidence belongs to ticket 06.
