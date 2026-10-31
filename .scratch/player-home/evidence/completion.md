# S20 completion

Implemented on `codex/player-home`. Final verified code revision: `2b4cad00b65d287f011892041347c8e5ef0a97a5`. Later commits record evidence and canonical tracker status only. This is local implementation completion; no merge into main or production deployment is claimed.

Signed-out visitors have Quick join by code and permanent-account entry. Signed-in players can create a campaign, reopen hosted campaigns, reenter joined campaigns and join another game. Guest sign-in/registration preserves home or campaign-creation destinations. Discovery uses indexed server membership, safe summaries and existing authenticated return authority. Development fixtures remain available separately.

## Verification

| Check | Unique passing cases |
| --- | ---: |
| Tooling | 3 |
| Simulation | 287 |
| API, final complete run | 247 |
| Gallery | 36 |
| Solo browser | 22 |
| Shared browser | 22 |
| Campaign-resume browser | 2 |
| **Total** | **619** |

Final typecheck and production build passed. Existing initial-bundle and QR CommonJS warnings remain. Counts are deduplicated across the initial integrated pass and affected corrections; focused API and browser reruns are not added again.

The full final API suite passed 247/247 with zero failures or skips. Its retained TRX and complete console log establish recovery of every failure in the earlier 242/247 run, rather than inferring success from a partial rerun. Independent Standards and Spec reviews each have zero outstanding findings; see [review](review.md), [review fixes](review-fixes.md) and [final integration fixes](final-integration-fixes.md).

Ignored execution records retained at repository root:

- Initial tooling/typecheck/build and simulation output: verification run `2026-10-30T20-03-15-110Z-9c38c793-f176-42c2-8728-89f3ca652ae2`. API compilation in that run was blocked by preview assembly locks; subsequent checks used isolated artifacts.
- Gallery 36/36, solo browser 22/22, resume 2/2 and initial shared 17/18: verification run `2026-10-30T20-05-32-285Z-e4f6c9f5-918b-4912-bbd9-71a35cce795f`. The one shared case was interrupted by frontend reload during integration; its stable rerun passed.
- Final shared affected run: `.tools/reports/s20-shared-final-focused.json`, 8/8 including that preparation case and four added guest authentication matrix cases. Together with unaffected shared cases this is 22 unique passes.
- Final typecheck/build: verification run `2026-10-30T20-15-51-632Z-7f8ff62b-5e7d-4e07-903e-21a9c8d4c83b`.
- Final API: `.tools/reports/s20-api-verified-emulators.json`, `s20-api-verified-command.json`, `s20-api-verified.log`, and `s20-api-results/s20-final.trx`. The configured test recovery host matches owned Firestore19880; Auth19889/hub19890/logging19920/websocket9195 were owned and stopped.
- Worker JSON/TRX records preserved under `.tools/reports/s20-workers/`; focused fix records also remain under `.tools/reports/player-home-fixes/` and `player-home-finalfix/`. Completed records were retained when affected checks were rerun.

## Visual checks and preview

Reviewed complete 360×800 home, guest joining, lobby and resume flows, plus the 1920×1080 public Classification and desktop resume view. Published captures are linked from [join evidence](02-join-game.md) and [resume evidence](03-resume-campaigns.md). No clipping or horizontal overflow was observed.

Fresh preview: UI `http://127.0.0.1:4412/` (PID29516), API4511 (PID31928), existing Auth19099 (PID27476). Home and proxied auth configuration returned HTTP200. Original UI4411/API4510 and their in-memory games were preserved. This fresh preview uses volatile storage; it does not import the original service's test games or add durability across backend restarts.

All five implementer worktrees were removed after confirming clean status and integrated ancestry. Dependency junctions were detached nonrecursively, root dependency targets retained, and worker branches preserved. Test-owned services stopped; the two preview service sets and existing authentication remain running.
