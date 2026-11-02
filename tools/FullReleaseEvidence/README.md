# Full-release campaign evidence

Run from the repository root against an owned loopback Firestore emulator (`demo-f1game`). Set `F1_RECOVERY_HOST=127.0.0.1:<port>`. The tool rejects cloud endpoints. Build with `dotnet build tools/FullReleaseEvidence`; invoke the resulting `bin/Debug/net10.0/FullReleaseEvidence.dll` in a separate process for each operation.

```text
capture OUTPUT --version release-2026-cards-v1 --source FULL_COMMIT_SHA
resume OUTPUT [--stop-after N]
resume NEW_OUTPUT --checkpoint PRIVATE_CHECKPOINT --restore-store [--stop-after N]
verify OUTPUT [--partial]
bench OUTPUT
```

`capture` requires a new/empty output directory. A complete capture requires an exact clean source commit. It drives 23 actual six-lap, 22-car weekends through public application operations, including qualifying, automatic preparation, standings, 220 unclaimed-team development decisions and 22 explicit host-absent Save commitments. It retries duplicate results and injects pre-commit/lost-acknowledgement failures at development boundaries. The 45-minute guard and 1,024 lifecycle actions per weekend bound this diagnostic workload; six-lap evidence does not establish full-distance season balance.

`resume` uses the durable store plus saved command identities. Importing a historical checkpoint requires `--restore-store` and a new/empty output directory. All target identities must be absent before the first import write; existing saved data is preserved on rejection. Assets and the matching frozen archive are copied from the checkpoint's private directory. Keep these files together. Use an isolated empty emulator for historical imports.

`verify` reconstructs qualifying, grids, openings, every race operation, raw results, scoring and cumulative standings from the frozen archive without constructing the current content catalogue. It validates frozen-input and retained-asset hashes. A partial archive cannot receive full acceptance. `bench` performs fresh-process durable recovery, tests absent/different deployment catalogues, and checks unsupported/corrupt copies without changing the original save. It writes diagnostic probe identities only within the owned emulator.

Output contains private campaign records, host/device identifiers and command journals. Keep the entire output ignored; publish only reviewed measurements and captures, never the archive or `private/`. `progress.json`, `capture.json`, `verification.json` and `bench.json` distinguish incomplete progress, capture, independent replay and compatibility checks. Per-event private checkpoints allow recovery after interruption. Atomic progress-file replacement tolerates brief Windows reader locks and still fails on persistent access errors.

`smoke.mjs` runs two actual weekends plus corruption, incomplete-acceptance, nonempty-output, fresh-process resume and compatibility checks. Set `F1_EVIDENCE_OUTPUT` to a fresh ignored/private path and `F1_EVIDENCE_SOURCE` to a full candidate SHA. It is tool validation, not full-season acceptance.
