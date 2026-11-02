# S16 full-calendar durable evidence — 2 November 2026

The complete selected 23-event preset campaign finished through actual Firestore-backed application operations. A separate process reconstructed every qualifying result, grid, opening, race operation, raw classification, points award and cumulative standings from the frozen archive. No current deployment catalogue was constructed by replay verification.

Source pin: `d5410a737cee7085ef9236f45720ed2cf638dfef`, `release-2026-cards-v1`. Later commits change signed-modifier wrapping, legacy-image presentation compatibility, test expectations, emulator lifecycle and documentation; they do not change frozen simulation inputs, content or model versions. The capture began from an exact clean source checkout. This is a six-lap-per-event preset content/recovery workload, not full-distance balance or release-performance acceptance.

## Results

- All 23 actual 22-car weekends finished, with three qualifying stages and frozen effective preparation for every event.
- 220 automatic development choices and 22 explicit claimed-team host-absent Save commitments were checked. Duplicate result and development retries retained the same grants and decisions.
- Two deliberate development-storage interruptions covered failure before commit and lost acknowledgement after commit. Both recovered without duplicate spending or contributions.
- Frozen-input and all 51 retained-asset hashes passed. Fresh-process durable recovery succeeded with absent and different deployment catalogues. Unsupported content and corrupt envelope copies failed actionably while preserving those copies and the original last-good campaign.
- The separate two-event CLI smoke rejected damaged asset bytes, changed frozen-input hashes, incomplete full acceptance, nonempty capture destinations and historical import over live identities. A real interrupted six-event checkpoint imported into another owned emulator, resumed through event seven and passed fresh-process replay.

## Measured workload

| Measurement | Actual result |
| --- | --- |
| Complete capture timer | 712.082 seconds |
| Independent 23-weekend verification | 60.104 seconds |
| Lifecycle actions / race operations | 211 / 27 |
| Campaign reads / attempted writes / acknowledged writes | 418 / 160 / 159 |
| Weekend reads / attempted writes / acknowledged writes | 1,274 / 352 / 352 |
| Maximum campaign raw / encoded payload | 34,351,538 / 261,773 bytes |
| Maximum weekend raw / encoded payload | 5,025,676 / 169,316 bytes |
| Final raw development history | 27,710,957 bytes |
| Cold typed campaign recovery | 22.219 seconds |
| Three warm typed campaign reads | 2.181, 2.038, 2.073 seconds |
| Cold final-weekend recovery | 1.519 seconds |
| Recovery benchmark peak working set | 1,491,144,704 bytes |

Counts are adapter operations and payload bytes, not cloud billing units. Maximum raw and encoded sizes need not occur on the same weekend. Capture/replay/recovery ran in separate processes; the benchmark reports its own memory and operations. Per-event cumulative checkpoint times and actual raw/encoded storage sizes are retained in [the sanitized event table](final/per-event-storage.json).

Late-season cold recovery is slow and the benchmark memory footprint is substantial. S18 must assess household latency, full-distance seasons, browser/device performance, balance and cloud cost. No production deployment, physical-device household playtest, real-world physics calibration or rights clearance is claimed here.

## Reproduction and artifacts

Use [the evidence CLI](../../../tools/FullReleaseEvidence/README.md) with an isolated loopback Firestore emulator and a new ignored output directory. Execute `capture`, then `verify` and `bench` in fresh processes. Preserve the complete private checkpoint directory for `resume`; it contains host/device identifiers, command journals and recovery payloads and must not be published.

Public reviewed measurements: [capture](final/full-season-capture.json), [independent verification](final/full-season-verification.json), [cold/compatibility benchmark](final/full-season-bench.json). Private archives and checkpoints remain ignored under `.tools/` in the retained worktree.

The first full attempt stopped after six events when a Windows reader briefly prevented atomic replacement of `progress.json`; its saved checkpoint recovered. The writer now retries brief sharing/access errors for at most 500 ms and propagates persistent failures. The successful fresh 23-event run above supersedes that partial attempt and the earlier 21/23 diagnostic. Original failed records remain in ignored local reports.
