# S13 integration record

Integration branch: `codex/initial-car-construction`. Baseline: completed S12 `05ae54c3ca4e67e57e99be7865e1348488260425`. Integrated implementation and remediation: `5bf1b3926e27539a2d970e7ebd3a717b12298f86`.

All four tickets' engineering work is integrated. Ticket 04 and the parent remain ready for human review because actual player choice comprehension is **UNMEASURED**. See the [map](../../map.md), [experiment findings](../04-construction-evidence.md) and [browser assessment](../04-browser.md). Engineering readability and automated interactions do not establish human understanding.

## Delivered behavior

New acquisition campaigns enter construction after their rosters complete. Every team gets the same frozen four-category catalogue and its 625 raw combinations. Costs map one provisional budget unit to one shared integer credit; the source's separate 20-unit envelope is explicitly superseded by actual available credits. Complete builds cost 18–24, with a legal 18-credit baseline protected by acquisition. A draft is free, saved, revisioned and private; unaffordable combinations cannot replace it or spend credits.

Previews show five source-weighted Simple groups, optional detailed values, both drivers' supported indicative compatibility and qualitative Monza/Monaco comparisons. A versioned bounded-delta mapping records diminishing returns. The construction baseline's Reliability 84 does not retrofit the legacy neutral value 82. No future weather, incidents, exact lap-time promise or headline-star physics is introduced.

Confirmation commits the immutable identity, frozen catalogue/choices/materialized inputs and one exact debit in the existing ledger under one campaign revision. Stale, conflicting or unauthorised commands reject; identical retries return the accepted car. A paid 18-credit car can legitimately leave zero. The independently durable first-lobby guard stays closed until all cars are confirmed and their exact inputs are installed. Restart repairs interrupted handoffs and initialization without replacing cars or charging twice.

Constructed ratings and compatibility enter supported preparation once; Weekend Setup remains a separate once-applied layer. Personnel, source history, Pit Crew and event conditions are preserved across every frozen event. Reliability feeds the existing incident profile only when its opt-in policy is enabled; no default incident behavior changed. Permanent development and Race Package creation are deferred consumers.

Versioned guidance uses the same catalogue, own roster and actual available credits. It preserves affordable manual alternatives and explains future-credit cost. Explicit host automatic construction acts only on untouched unclaimed teams, records its observations/choices and uses normal confirmation/debit rules. Manual drafts and accepted cars remain intact. Preparation cannot bypass readiness, and new purchases close after qualifying; accepted confirmations remain replayable.

## Independent reviews

### Standards

[Original review](standards-review.md): zero documented-standard breaches and one P3 duplicate handling-axis calculation. One implementer replaced the preview copy with the shared domain operation. [Narrow recheck](standards-recheck.md): resolved, no new material findings.

### Spec

[Original review](spec-review.md): one P3 personnel-panel spending-history/reserve disclosure error after a car purchase. It now names the construction debit and distinguishes paid-car completion from the unspent reserve. A public browser regression proves seven personnel debits, one construction debit and a legal zero remainder. [Narrow recheck](spec-recheck.md): resolved, no new spec defects. Human comprehension remains an acknowledged acceptance gap.

Final actionable findings: Standards zero unresolved; Spec zero unresolved.

## Verification provenance

The complete 360×800 phone flow and actual 1920×1080 public top were inspected before the single full verifier invocation. Reviewed viewport captures distinguish comparison, driver fit, affordability, confirmation, private clearing, missing-image fallbacks and first-weekend handoff; tall historical captures are not substituted for viewport evidence. Final paid-car remediation captures were also reviewed.

[Initial manifest](initial-verification.json), source `818ad34`, records tooling, typecheck and build passes plus 239 passing simulation tests. Its API result was 223 passed/one failed: automatic delivery consumed an injected recovery failure before an explicit manual sweep. This failed run is retained. A timing probe reproduced two failures/four cases with automatic delivery and passed all four with fixture-local manual mode. The final fixture retains its assertion, scoped ownership and parallelism, with no temporary delay or production scheduler change. See [remediation evidence](../remediation/README.md).

Only affected and previously unrun named stages were executed after remediation. [Their successful manifest](verification-completion.json) pins `5bf1b39`; the initial tooling pass remains valid. All twelve stages have passing records: tooling 3; typecheck; production build; backend 239 simulation and 224 API cases; gallery 34; browser 22; boundary 2; shared 14; assistance 1; control 1; deadlines 1; hosted 1. The production initial bundle is 429.38 kB within the unchanged 500 kB error budget. Existing warning-budget/CommonJS warnings remain. No second whole verifier invocation was used.

Focused remediation also passes 17 public domain checks, eight construction API cases, both browser flows, typecheck and exact 1,300-race replay. These subsets are not added again to integrated suite counts. The corrected paid-car ledger viewport was independently inspected by the integration coordinator after merging.

Runtime: Node 24.22.1, .NET SDK 10.0.401 from global.json, Java 21.0.12.1, locked dependencies and cached Chromium. Owned isolated services use the retained [emulator configuration](verification-emulators.json) and [rules](firestore.rules). Command-scoped `NuGetAudit=false` retains dependency pins while accommodating existing advisory/feed conditions; it does not establish a clean security audit. Local hosted regression checks do not constitute a deployment.

## Evidence limits and cleanup

The frozen archive SHA256 is `BE0A5D69FB5468923AE785EE5F0A4E2169C7DCB907635B8845EFDEF4C1609782`: 1,300 races with 22 entries each, split into 300 normal and 1,000 explicitly enabled incident diagnostics. Forty pairwise comparisons preserve the absent cornering crossover and low-drag local dominance under one fixed roster/grid/20-lap strategy. The diagnostic sample shows no observed retirement reduction; it does not prove the consumer inactive or reliability ineffective. Two representative circuits do not validate full-calendar balance, final coefficients, optimal strategy or real player comprehension.

Fresh old tools replayed the published S12 acquisition archive and S11 short campaign archive exactly, retaining original hashes and outputs. The final constructed archive also replays exactly after remediation; corruption and no-overwrite contracts pass. Supplied artwork remains intact, with stable manifests/provenance, runtime variants and live fallbacks; no car artwork was supplied or invented.

Worker evidence, commands, PIDs, configuration and captures were preserved with verified SHA256 manifests under `.tools/reports/s13/`. Merged worker checkouts were removed after nonrecursive unlinking of dependency junctions; shared caches remain intact. Only the root integration worktree remains. The final [listener inspection](listeners.json) found no task-owned listeners on the checked ports. [Completion manifest](completion.json) collects source revisions, results, capture hashes and pending human acceptance. No push, PR, deployment or S14 work is part of this milestone.
