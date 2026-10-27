# Narrow Spec recheck

Reviewed remediation from `a2d7097d7ff74fee8aaaae0322eb6a3246bcb608` to `e1b25da26ad35d6c080f46a8750ba0f9aec9cf2e`, limited to the previous deferred-crossing finding and its remediation. No tests, services or repository edits performed.

**Prior P2 resolved.** `RaceEngine.WeatherReactions.cs` now computes wetness at the first separation-eligible time within the current rain interval and checks both hysteresis edges of the remembered band before considering the derivative. Therefore a still-applicable crossing survives a subsequent flat or reversing rain interval; a crossing that has ceased to apply does not force an obsolete transition. Future crossing prediction starts from that eligible observation, preserving interval-knot handling.

The added public-boundary regressions directly cover both reported failure modes at 84 seconds, reversal below the upper edge followed by the legitimate Dry transition at 99 seconds, and interactions with the ordinary boundary, Safety Car expiry and finish at the deferred timestamp. The remediation report records valid failures before the fix, 24 unique focused passing checks and exact replay of all 560 unchanged published archives. These are recorded evidence, not checks independently rerun during this review.

**Remaining:** The previously disclosed human acceptance gap remains unchanged: ticket 06 still requires actual household measurements of forecast understanding, decision/presentation time and simultaneous-trigger clarity. The remediation correctly does not claim those measurements.

No remaining actionable defect in the prior P2's remediation. This is not a new whole-S10 review.
