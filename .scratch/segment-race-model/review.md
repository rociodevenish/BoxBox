# Segment model final review

Review range: `d22ceedc9548d3cfce6672812a31faf0f6a1bcf6` through the ten-ticket implementation at `6123d881fc3cb199e5d5c17a2a3d692b9f03404e`, followed by these corrective changes. Unrelated visual foundation work is outside this review.

## Standards

No actionable findings. Optional consolidation of duplicated statistical summaries was not required for this milestone.

## Spec

Two P2 findings were resolved:

- Complete effective input validation: a finite but extreme Speed could overflow only in a later segment, after time and history had already changed. Constructor validation now projects every segment using the shared public physics seam at the supported tyre and variability bounds, and checks derived wear for every compound/pace combination and the greatest segment demand. Aggregate validation uses the same helper as aggregate execution. Validation runs before mutable race state is created. Public regressions first failed for the later-segment reproduction, overflowing tyre demand, and future compound/pace/variability extremes, then passed with constructor rejection. No separate diagnostic physics or speculative trajectory Cartesian product was added.
- Randomness scope metadata: aggregate passing draws use attacker/opponent/lap, while segmented draws use attacker/defender/zone visit. Replay metadata and the documented channel table now distinguish those contracts. The public aggregate metadata regression failed before the correction and both model cases pass afterward. This changes the aggregate diagnostic description, not its draws or randomness version; historical aggregate evidence retains the annotation produced by its recorded source revision.

Validation: all 108 simulation tests pass; `tools/verify-runner.ps1` passes actual race/comparison replay, strict input/version rejection and experiment/benchmark runner checks. Default formulas and operation order are preserved, so published experiment sweeps were not repeated. Benchmark evidence remains tied to its recorded measurement revision; its initialization measurements predate the additional constructor checks.

The parent spec remains pending the coordinating agent's final acceptance and cleanup.
