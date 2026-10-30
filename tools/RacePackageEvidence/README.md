# Race Package comparative evidence

Run from the repository root with the pinned .NET SDK and Node24. This standalone CLI uses only public acquisition, construction, income, development, package-selection, effective-input, race and scoring operations. No emulator or production configuration is changed.

```powershell
node tools/run-bounded.mjs --timeout-ms 60000 --report .tools/reports/packages-build.json -- dotnet build tools/RacePackageEvidence -p:NuGetAudit=false
node tools/run-bounded.mjs --timeout-ms 90000 --report .tools/reports/packages-pilot.json -- dotnet tools/RacePackageEvidence/bin/Debug/net10.0/RacePackageEvidence.dll --pilot .tools/reports/packages-pilot
$env:PACKAGE_EVIDENCE_OUTPUT=(Resolve-Path .tools/reports/packages-pilot).Path
node tools/run-bounded.mjs --timeout-ms 180000 --report .tools/reports/packages-cli-tests.json -- node --test tools/RacePackageEvidence/evidence.test.mjs
node tools/run-bounded.mjs --timeout-ms 240000 --report .tools/reports/packages-full.json -- dotnet tools/RacePackageEvidence/bin/Debug/net10.0/RacePackageEvidence.dll .tools/reports/packages-full
node tools/run-bounded.mjs --timeout-ms 240000 --report .tools/reports/packages-replay.json -- dotnet tools/RacePackageEvidence/bin/Debug/net10.0/RacePackageEvidence.dll --replay .tools/reports/packages-full/race-packages.json.gz
```

Existing output directories must be empty; archives are never overwritten. Generation records SHA256 for the three tool source files (UTF-8 text with CRLF normalised to LF, so Git checkout line endings do not change provenance). Exact replay requires those same tool sources (the pinned tool commit), understood rule versions and frozen archive data. It does not reopen personnel, construction or weekend content catalogues. It reconstructs each complete row through public operations and compares acquisition receipts, confirmed car, exact ledger, all development quote/outcome/version contracts, package observations/choices, effective snapshots, race replay/classification/awards and standings inputs. Unsupported versions, missing rows and any divergent row fail. The archive SHA256 is retained in the manifest. CLI tests use a pilot and prove exact replay, production aliases, overwrite refusal and damaged rule/debit/quote/outcome/selection/race rejection.

The predeclared full matrix has seeds101–108 × two resource bundles × seven arms in each of two scopes:224 logical runs,896 six-lap event-races. Shared event-one prefixes are calculated once per scope/seed/bundle (736 actual race calculations). Counts of logical car-event starts are correlated counterfactuals, not independent samples. The pilot uses only seed101 (28 runs,112 event-races/92 calculations). No seed search, extra seeds, forced outcome category or injected credit is used.

Resource bundles are economical or highest-priced LEGAL offers for the target; opponents use cheapest actual offers. All preserve completion reserve and unique ownership. Six development arms confirm minimum cars. The seventh arm instead attempts the declared actual speed construction philosophy before saving every development opportunity. Price is not roster strength. Target hiring can alter later offers and opponents, so comparisons across bundles are not isolated staff effects. Paired treatment comparisons stay within the same bundle, scope and seed.

`production-short` follows the supported Monza→Monaco shape with default inactive incidents and one development opportunity. It is a domain/simulation campaign-shaped experiment, not an application-coordinator/qualifying rehearsal. `small-library`, `large-library` and `refinement` have exactly the same first creation here; the latter two are explicitly labelled aliases. This scope cannot compare mature libraries or refinement.

`synthetic-alternating-six` repeats frozen Monza/Monaco three times with unique event/opportunity identities, normal flat income AFTER each completed event and one decision before the next. This is an explicitly synthetic design experiment, not an enabled production or full calendar. It uses the existing `disruption-live-opt-in-v1` error/contact/failure coefficients and reviewed Monza/Monaco pit routes, frozen equally across siblings. No hazards are increased to obtain retirements. Paired differences never cross the two scopes. Reliability selection weight is zero unless that scope's mechanical-failure consumer is active.

Fixed arm schedules are versioned in the tool:

- Save every opportunity with minimum construction.
- Base-car: attempt the supported speed build (low-drag aero, balanced chassis, top-end power, low-drag thermal) with an actual construction debit, then Save. Unaffordable construction explicitly retains the minimum build and records its error. This arm has its own constructed presets and independently simulated event-one result; no minimum-car prefix is reused. Comparisons begin with equal acquired credits, not equal post-build balances.
- Permanent: handling for the next Monaco, speed for the next Monza.
- Small library: attempt Downforce then Low-drag, then Save.
- Large library: attempt Downforce, Low-drag, Tyre-management, Mechanical-grip, Cooling/reliability in order.
- Refinement: first purchase Downforce; then strengthen/reduce-compromise/broaden/strengthen its latest COMPLETED owned version, respecting pending-version reservations. No completed parent means explicit Save.
- Rush: the same intended creation sequence as large library, with Rush on each attempt. Unaffordable Rush is a recorded rejection followed by Save, never silently downgraded to standard work.

Unaffordable/capped work retains its exact failure and chooses Save; nothing is refunded or injected. A failed early purchase is not retried under a different schedule. Delay at the final opportunity stays pending. Free selection uses the public own-library/circuit policy for all arms, recorded separately from the fixed project schedule. Only one available immutable version applies. Small/large are intended philosophies: actual completed library size and unused versions are evidence, not prepopulated assumptions.

Both scopes use the same transparent constructor-order grid, six-lap race policy (Hard pit instruction at lap1), race seeds and hidden project/opportunity/class identities across sibling arms. Source content, controls and personnel are frozen; class keys remain diagnostic archive data and cannot enter either policy observation. This fixed race policy isolates development alternatives under a small design scenario; it is not a complete campaign AI, qualifying, household or optimal racing study.

The summary keeps every row, each outcome category including zero, pending work, failed quotes, separate Base Car and development spending/balances, library/version counts, selections and retirements. Paired mean two-car finish seconds are null if either sibling car fails to finish; excluded comparisons and reasons remain visible through retirement counts, complete classifications and per-event nulls. Cumulative totals are null if any event is non-comparable. Standings use the real event-one leader by the existing tie-break, retaining all points-tied leaders and margins through every event versus the matching Save sibling. Empty leading/trailing/tied groups are explicitly counted. Points changes, slower/unused purchases and retained credit remain separate.

Eight paired seeds and repeated circuits cannot establish an optimal philosophy, real-world calibration, full-calendar balance or runaway safety. Existing #53–56 public correctness and browser evidence separately proves outcome categories, action counts, retry/restart, lock and UI behavior. Human understanding/workload (#74) remains unmeasured; validated full-calendar release comparisons remain deferred to S16.

The original six-arm declaration was generated and exactly replayed under schema/policyv1 at tool commit `b77ee478ba9eae4ec315e8bc856b31d7bc63ffd2`. Before running the appended Base Car arm, parent-spec review identified the missing construction comparison. Policy/schema v2 appends that seventh fixed arm; it does not change the original six schedules, resources, draws or race controls. The original archive/manifest/summary remain preserved separately.


Review-fixed regeneration uses archive/summary schema v3 with product source `6ee1dbffbc2244ef21847ced851b685e2b8c9de7`. The seven-arm schedule policy remains v2: seeds, resources, controls, intended projects and races are unchanged. Evidence records the new non-overlapping broadening draft eligibility; saved paid v2/v3 quote/outcome contracts keep their existing versions and legacy semantics. Shared demand-fit arithmetic preserves the previous normalization and evaluation order. Earlier six-arm v1 and seven-arm v2 archives remain historical and require their pinned tool/source checkout; v3 never overwrites them. The current reviewed artifacts are linked from the ticket05 review addendum.
