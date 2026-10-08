# Segment laboratory benchmark

Serial Release workload: 22 mixed-car-fit cars, 40 laps, paired seeds 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26; two complete warm-up races, then 20 timed races per model. Eight five-leader-lap phases per race. Warm-up excluded. Each model also has three separately sampled memory/report runs using the first three seeds. No benchmark overlaps an experiment sweep.

Actual previous executable is compiled against pinned commit `d22ceedc9548d3cfce6672812a31faf0f6a1bcf6`, using its RaceEngine and ReportWriter; current aggregate is a separate control. Current workloads use 4/8/16 equivalent subdivisions of the same four-zone layout. Shared PassingZoneId preserves one opportunity per pair/eligible visit; subdivision does not add passing zones. These models have different semantics/seed contracts, so equal workload is not a claim of identical historical outcomes.

| Model | Segments | Phase mean/p95/max ms | Phase allocated bytes mean/max | Mean race phase-resolution ms | Engine + run + phase snapshots retained heap delta mean bytes | Final history JSON mean bytes | Warm report mean ms / allocated bytes / output bytes |
| --- | ---: | --- | --- | --- | --- | --- | --- |
| previous-aggregate | 1 | 7.556/21.334/26.818 | 3047836/4054760 | 60.446 | 1553424 | 1398921 | 86.772/39694264/10741662 |
| current-aggregate | 1 | 7.224/24.744/46.813 | 3154813/4206064 | 57.794 | 2107008 | 1949183 | 109.484/49669800/14998858 |
| current-segments-4 | 4 | 26.901/39.076/48.016 | 35087769/45215712 | 215.208 | 5852064 | 7285196 | 232.944/204693240/56681070 |
| current-segments-8 | 8 | 60.386/79.530/89.774 | 66114318/84812288 | 483.089 | 10383195 | 13495288 | 443.596/499238213/103290694 |
| current-segments-16 | 16 | 162.576/196.861/219.274 | 129003452/163574944 | 1300.604 | 19365552 | 25701894 | 869.768/977459859/197186757 |

## Measurement boundary

Phase timings surround only ResolvePhase(plan), including its state/history snapshots; initialization is separately sampled. Phase allocation uses current-thread allocated bytes. Scalar sample recording and all serialization/file I/O lie outside that interval. Calls execute serially. Results include per-phase observations, mean/sample SD/p95/max and initialization costs.

Retained heap is the incremental managed live-heap estimate after full GC for isolated roots holding the engine, RaceRun, all eight cumulative phase snapshots and designer diagnostics where supported. NoInlining boundaries and GC.KeepAlive prevent a previous sample's locals or premature collection from defining that lifetime. It remains a process-level GC estimate, not exact graph-size accounting. Final history JSON bytes and on-disk report bytes are separate sizes. Warm report timing/allocation uses each revision's actual ReportWriter after one untimed writer warm-up per sample, includes JSON/text/replay where supported, and includes file I/O. Reports contain cumulative phase histories, so output cost grows with richer contracts and retained events. No forced GC or report work enters phase timings.

Runtime, CPU identifier/name, architecture, logical processors, GC configuration and available-memory limit are published for both executables. Host scheduling, JIT and GC affect tails; three report samples do not establish stable disk-performance percentiles. These local measurements do not establish multiplayer capacity or tight wall-clock test thresholds.

Reproduce with `tools/verify-benchmark.ps1` (Release, pinned baseline checkout and external harness), then `--suite benchmark --runs 20 --baseline-benchmark <previous-benchmark.json> --output <directory>`. Generated full reports stay ignored; this summary and the machine data are concise published artifacts.

