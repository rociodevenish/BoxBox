# 10: Benchmark simulation and recommend the next playtest

**What to build:** Developers inspect simulation costs and designers receive an evidence-based scope for the next browser playtest.

**Blocked by:** 07: Measure track fit and passing frequency; 08: Measure tyre and pit-window sensitivity; 09: Measure persistent form and lap-noise effects

**Status:** ready-for-agent

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [ ] Benchmark warm simulation-only phase resolution separately from report generation over increasing segment counts and full-distance 22-car races.
- [ ] Record mean, p95 and maximum latency, allocated bytes, retained history/report sizes, runtime/hardware, seeds, workload and repetitions.
- [ ] Compare against the previous laboratory without inferring multiplayer capacity or asserting tight wall-clock test thresholds.
- [ ] Publish supported conclusions, tuning-sensitive conclusions, remaining limits and a browser-playtest recommendation covering clarity, feedback and intended duration.

