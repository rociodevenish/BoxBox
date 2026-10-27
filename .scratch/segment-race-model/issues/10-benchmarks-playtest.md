# 10: Benchmark simulation and recommend the next playtest

**What to build:** Developers inspect simulation costs and designers receive an evidence-based scope for the next browser playtest.

**Blocked by:** 07: Measure track fit and passing frequency; 08: Measure tyre and pit-window sensitivity; 09: Measure persistent form and lap-noise effects

**Status:** resolved

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [x] Benchmark warm simulation-only phase resolution separately from report generation over increasing segment counts and full-distance 22-car races.
- [x] Record mean, p95 and maximum latency, allocated bytes, retained history/report sizes, runtime/hardware, seeds, workload and repetitions.
- [x] Compare against the previous laboratory without inferring multiplayer capacity or asserting tight wall-clock test thresholds.
- [x] Publish supported conclusions, tuning-sensitive conclusions, remaining limits and a browser-playtest recommendation covering clarity, feedback and intended duration.

## Answer

Published [benchmark](../evidence/benchmark.md), [machine data](../evidence/benchmark.json) and [browser-playtest recommendation](../evidence/playtest-readiness.md), synthesizing tickets 07–09 without adopting final tuning or implementing a browser. `tools/verify-benchmark.ps1` reproduces the pinned actual historical executable through an external harness referencing its simulation assembly and ReportWriter. Historical checkout `d22ceedc9548d3cfce6672812a31faf0f6a1bcf6` stays tracked-clean; current aggregate is explicitly a separate control.

Five serial Release workloads: actual previous aggregate, current aggregate and equivalent 4/8/16 subdivisions with shared passing identities. Each has two full-race warm-ups, 20 timed seeds 7–26 (22 mixed-car-fit cars, 40 laps, eight phases), plus three isolated memory/report runs. This is 100 timed races/800 measured phases, ten warm-ups and fifteen memory/report samples. Explicit fields/plans/configuration, per-phase observations, initialization costs, mean/sample SD/p95/max, phase allocations, live-heap estimates, history JSON size and actual warm ReportWriter timing/allocation/disk size are recorded. ARM64 Snapdragon X Elite X1E78100, 12 logical processors, .NET 10.0.12, workstation Interactive GC; both executables' complete environment details are retained.

Final phase mean/p95/max milliseconds: previous 7.556/21.334/26.818; current aggregate 7.224/24.744/46.813; 4 segments 26.901/39.076/48.016; 8 segments 60.386/79.530/89.774; 16 segments 162.576/196.861/219.274. Four/16-segment mean phase allocations are about 35.1/129.0 MB; approximate retained engine/run/phase-snapshot heap 5.85/19.37 MB; final history JSON 7.29/25.70 MB; actual full diagnostic reports 56.68/197.19 MB. Heap deltas use isolated NoInlining reference lifetimes, full GC and GC.KeepAlive and remain process-level estimates, distinct from serialized bytes. Report work and forced GC are outside phase timings. These local serial measurements imply no multiplayer capacity or tight wall-clock test threshold.

Recommendation: exploratory coarse-layout strategy playtesting focused on estimated tyre feedback, queued next-lap-end stops, warm-up/rejoin queues and distinguishing actual passes from pit-cycle ranks. Measure intended 20–25 minute real player sessions rather than equating the virtual clock with play time. Sparse equal-all overtaking, tuning-sensitive pit/form results, inactive default exhaustion controls and large cumulative designer reports remain explicit limitations. Compact player transport/retention and concurrent responsiveness require separate work.

Validation: benchmark CLI observed unsupported red before implementation. Actual reproduction script passed, verifying all five sample sets, full legal field/distance, configured shared passing identities and unchanged historical source. Full tests 98 passed. Runner smoke passed, including exact replay and benchmark sample-size rejection before reports. No latency threshold assertions or simulation formula changes. Parent spec resolution remains for integration review.

