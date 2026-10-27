# S13 Standards review

Scope: `git diff 05ae54c3ca4e67e57e99be7865e1348488260425...818ad34e362314c1cd015e6bd1af507bc6f39ff6` (integrated target pinned).

Documented-standard breaches: none found in the reviewed source, contracts and new evidence tooling. Sources: AGENTS.md; docs/agents/issue-tracker.md, triage-labels.md, domain.md and verification-workflow.md; GLOSSARY.md; the locked concept and technical architecture documents; construction source mapping. No ADR directory exists. No code was changed and no checks were executed by this reviewer.

Judgement calls:

- **[P3] Possible Duplicated Code — reuse the supported mechanical-axis mapping.** `src/backend/Game.Simulation/ConstructionPreview.cs:25–27` computes `Math.Clamp((Handling("rearStability")+Handling("tractionConfidence")-Handling("frontResponse")-Handling("rotation"))/2,-2,2)` and rounds away from zero. The new `src/backend/Game.Domain/CarConstruction.cs:55–59` exposes exactly this calculation as `MechanicalAxis`, already consumed by confirmed construction and policy evaluation. Having the preview retain its own copy creates two places to update a versioned mapping and risks showing compatibility different from the purchased car after tuning. Call `CarConstruction.MechanicalAxis(catalogue,choices)` from the preview and remove its redundant selection/handling calculation. This is a baseline Duplicated Code heuristic, not a documented-standard violation; the current expressions agree.

Summary: 0 documented breaches; 1 heuristic finding, worst P3.
