# S16 progress — 2 November 2026

This is a work-in-progress checkpoint, not release acceptance. Canonical scope and status remain in [issues #21–25](https://github.com/rociodevenish/BoxBox/issues/25). The integration branch is `codex/s16-full-calendar-content`; draft PR [#117](https://github.com/rociodevenish/BoxBox/pull/117) is stacked on the S14–S15 branch in #112.

## Committed implementation

- Validated, immutable content packs; selected 2026 calendar with 23 explicit circuit profiles and eleven constructors. Earlier pack versions remain available.
- Affordable, unique personnel pools, supported car/package metadata and frozen display data. Shared cards use saved identities, prices, rating groups and retained hashed assets rather than live sample JSON or positional driver mappings.
- Reproducible packaging and visual audit of 51 supplied assets, preserving originals and 55 legacy files.
- Bounded, integrity-checked compression for campaign and weekend saves, retaining legacy raw records and existing storage limits. A real 5,415,499-byte weekend fixture stores in 56,180 bytes and recovers its frozen preparation and replay.
- **WIP:** additional card fallback corrections and browser coverage (`b77944b`), plus the new capture/resume/verify/benchmark evidence tool (`90d309d`). These are committed for continuation; their latest changes are not fully accepted.

## Verification recorded so far

- Tooling, frontend typecheck and production build passed on the integrated card candidate. Existing bundle-budget and QR CommonJS warnings remain.
- 369 simulation cases passed. The corrected backend run passed 290 of 293 API cases; all three failures were traced to two test transport adapters assuming raw JSON. Five focused cases passed after correcting those adapters, with assertions unchanged.
- Development, delayed development, guidance, package creation/refinement, compromised packages and Rush browser steps passed. The delay configuration intentionally skips the separate ordinary-budget scenario.
- Full-calendar and pilot content browser cases passed; the personnel case still needs its focused rerun after fallback-caption changes.
- Gallery: 34 passed. Solo browser: 20 passed and two obsolete synthetic-portrait expectations failed; fallback identity corrections and updated expectations are committed but await their focused rerun.
- Boundary: two passed. Shared play stopped on the preparation image-fallback wording assertion. Assistance, control, deadlines and hosted steps were not reached in that invocation.
- New-card API checks and the real phone/tablet/TV-to-private-race flow passed before the latest WIP changes. Expanded all-role browser coverage is unfinished and its last run failed.
- The evidence tool completed a two-event Firestore-emulator smoke with fresh-process resume/replay and compatibility checks. It is explicitly partial. Later asset/input integrity additions are still being validated.

The failed verification manifests remain preserved alongside successful focused reruns. No broad-suite success, hosted CI result, full-season completion or independent final review is claimed.

At this progress checkpoint, frontend typecheck passed again and the committed FullReleaseEvidence tool built with normal NuGet auditing, zero warnings and zero errors. This compile check does not replace the pending behavioural checks.

## Remaining work

1. **[#24 — cards and assets](https://github.com/rociodevenish/BoxBox/issues/24):** finish and rerun all-role acquisition, lost-hire acknowledgement, construction/package comparisons, long-name/unknown/unsuitable-image coverage and keyboard/touch checks. Recheck the latest caption and synthetic-identity fixes; publish the final reviewed captures and evidence.
2. **Regression completion:** resolve the shared preparation fallback assertion; rerun affected personnel, synthetic-controller and shared cases. Run the still-unreached assistance, control, deadlines and hosted steps, and the final named cards step. Rerun typecheck/build for the final changes and reconcile results without counting duplicate cases.
3. **[#25 — campaign and historical recovery](https://github.com/rociodevenish/BoxBox/issues/25):** finish the evidence tool and its asset/input integrity smoke; pin a clean final source revision. Complete all 23 actual emulator-backed weekends, 220 automatic development choices and 22 explicit claimed-team Save commitments. Retain per-event private checkpoints and separate campaign/weekend size, operation and timing measurements.
4. Verify that full archive in a fresh process, including frozen qualifying/race inputs and results, retained asset bytes, catalogue update/removal, unsupported-version rejection without save loss, durable resume and retry checks. Publish a reviewed report without private recovery payloads. The earlier ten-minute diagnostic stopped at 21/23 and is not acceptance.
5. Run fresh, independent Standards and Spec reviews; resolve findings in one fix branch, rerun affected checks and record a final completion manifest. Only then resolve the canonical issues and mark the draft PR ready. Retarget the stacked PR after #112 lands and clean up merged implementation worktrees while retaining integration.

## Existing evidence

- [Provenance](../research/provenance.md)
- [Circuit verification](s16-02-verification.md)
- [Personnel coverage](03-personnel-content.md)
- [Campaign storage prerequisite](storage-prerequisite.md)
- [Weekend storage prerequisite](session-storage-prerequisite.md)
- Asset audit and retained contact sheets: `04-assets/`

Private checkpoints, generated build output and ignored local test reports are not included in this commit.
