# S16 #23 — personnel and paid content candidate

Canonical issue: https://github.com/rociodevenish/BoxBox/issues/23 (implementation complete, left open for integrated review).

Implementation commit: `60f61cf`; verified integrated code: `b5f138ff58e475bb709cd6177c8459f30f665a22`, including storage prerequisite `f93166b`. Verification date: 2026-10-31. New immutable content version: `release-2026-personnel-rc1`. This candidate deliberately retains the representative Monza/Monaco calendar; #22 owns full-calendar geometry and #24 composes the later combined immutable candidate.

## Delivered behavior

The candidate retains all eleven stable constructor/team mappings, 28 real drivers, 21 fictional Managers, 39 fictional Race Engineers and 39 fictional Technical Engineers. No extra people were needed. The six real reserve identities stay real reserve references, with the dated starting-roster/temporary-substitution qualifications in [provenance](../research/provenance.md). Fictional staff retain distinct IDs, explicitly fictional identity provenance and neutral readable image fallbacks. Pit Crew remains a team attribute and is not a personnel slot/card.

Prices and economic rules retain the compatible `provisional-prices-v1` model: 100 starting credits, 18 minimum car credits, the global unique-demand Completion Reserve and existing fixed prices. Driver prices are 12/18/24 game credits from detailed race-pace tiers; supplied JSON economy values 7–12 and headline stars are not silently imported as actual prices or physics. Managers cost 8–10, Race Engineers 6–8, Technical Engineers 5–7. Completion supply remains at the existing base prices. The independent initial completion envelope is 96: 8 Manager + 48 two Drivers + 12 two Race Engineers + 10 two Technical Engineers + 18 minimum car. Highest-priced legal actual offers were exercised through all 77 hires, protecting each later reserve.

Construction retains its existing four-category, 20-option catalogue, one selection per category, 18–24 cost range, shared credit ledger, detailed source mapping and bounded modifier composition. The source's recommended 20-unit construction envelope is not introduced as a second wallet or an extra cap. All eleven teams actually purchase the reserved minimum car through the campaign API. Old prices, constructed cars, quote models and pilot/version-less compatibility remain supported.

Five paid Race Package templates each have normal and Rush neutral-staff reference contracts, frozen separately in `PaidPackageOptions`. Standard reference costs are Low-drag18, Downforce18, Tyre-management14, Cooling/reliability14 and Mechanical-grip16; Rush adds the existing 6 credits. These reference contracts are not acquired-personnel quotes: actual own-team PackageOptions/RefinementOptions remain authoritative and include frozen staff discount/quality. Initial ownership is strictly the neutral baseline only. Both candidate validation and saved initial FrozenPresets reject free purchased package ownership. Ten public runtime cases commit the printed cost once, resolve normal/partial/delayed work, retain the free neutral fallback, and prepare a selected completed alternative.

## Truthful meanings and frozen metadata

`ContentPersonnelDetail` records PersonId, Role, IdentityKind, ReferenceRole, designer Provenance, ImplementedAttributes, InactiveAttributes, RatingScale, PreferenceMeaning and EffectScope. It is saved in optional `CampaignContentSnapshot.PersonnelDetails`, alongside optional `PaidPackageOptions`. Defaults are omitted for older snapshots. These are immutable authoring/recovery metadata, not another set of simulation inputs or a wholesale public DTO; #24 owns safe display projections.

Implemented means a supported conditional consumer, not an unconditional effect at every event. Drivers retain detailed 1–100 game inputs; Starts applies at the automatic standing start, wet skill depends on wet conditions, setup feedback affects practice, and Adaptability moderates preference mismatch. Preferences describe compatibility separately from quality. Manager Efficiency/Development affect bounded paid-development discount/quality; Leadership/Operations are inactive. Race Engineer Setup affects assigned-driver Practice Assessment uncertainty; Tyre Reading, Strategy and Communication are inactive. Technical Engineer Development Efficiency and matched specialties affect bounded paid-development contracts, without raw pace, extra income or development-speed bonuses. Reliability remains conditional on the actual incident FailureRate consumer; the cooling template already discloses this condition.

Acquisition offers now show their actual effect label for all roles. Incorrect hardcoded “Future economy effects” and “starts has no current consumer” statements were removed. Explicit select accessible names keep Personnel distinct from Content pack after adding a personnel-named candidate. The new candidate selector explicitly says it is a two-event personnel candidate. No new simulation mechanics, geometry, ratings, budget grants or artwork were introduced.

Five paid-template asset identities use neutral readable fallbacks. Existing user-supplied driver/track assets remain in place, with no licence or photographic identity claim added by this ticket. Metadata does not confer rights. #24 owns the complete visual/card audit and shared anatomy.

## Verification at approved public seams

TDD used campaign creation/consuming API and public domain/simulation operations; no private parser tests or internal collaborator mocks. Persistence uses actual isolated Auth/Firestore emulators; only database/time boundaries are replaceable in tests.

| Check | Result | Bounded record |
| --- | --- | --- |
| New candidate before implementation | Expected red: unsupported content version | [red-api-real.json](03-personnel/red-api-real.json) |
| Missing paid catalogue before validation | Expected red: creation incorrectly accepted | [red-paid-options-owned.json](03-personnel/red-paid-options-owned.json) |
| Candidate UI before selector | Expected red: missing selectable candidate | [red-browser-behavior.json](03-personnel/red-browser-behavior.json) |
| Final integrated API/recovery | 10 passed: 77 unique hires, all eleven seven-person rosters, every minimum car confirmation/retry, preparation with acquired drivers and neutral-only initial library, catalogue-removal recovery, legacy pilot recovery; malformed pool/budget/provenance/effect/paid-contract/free-ownership rejection before creation reservation | [final-api.json](03-personnel/final-api.json) |
| All five normal/Rush paid options | 10 passed, actual public Edit/Commit/Close/selection/preparation | [paid-runtime.json](03-personnel/paid-runtime.json) |
| Complete phone hire/reload, driver offers and public display | 1 passed, including real portrait loads, compatibility versus quality, standing-start conditional text, no horizontal overflow, hidden private panel on public capture | [green-browser-response.json](03-personnel/green-browser-response.json) |
| Final Angular typecheck | Passed | [final-typecheck.json](03-personnel/final-typecheck.json) |

Earlier setup failures (wrong Java path, sandbox-local test-host/bind restrictions, missing shared Chromium cache configuration and incorrect runner CWD) remain in ignored bounded reports. Browser iterations exposed an accessible-name ambiguity and an assertion that checked before the creation response completed. The product now has explicit select names, and the flow waits for the real API acknowledgement and saved hire state; no arbitrary sleeps or enlarged success claim were used. The final API check was repeated after merging the changed compressed-persistence boundary; unaffected suites were not repeated.

Owned ports: Auth22109, Firestore22100, hub22410, logging22460, websocket9451, UI4691, API4791. No pre-existing listeners were reused; bounded runners and Playwright owned cleanup. Node24, audited .NET10.0.402, Java21 and the installed Chromium1234 cache were used. Reproduce API with `PersonnelReleaseTests` plus `ReleaseContentTests.Reviewed_candidate_creates`, the tracked `firebase.personnel-content.json`, manual wakeups and matching Auth/Firestore environment variables. Reproduce browser with `playwright.personnel-content.config.ts`, F1_TEST_PORT4691/F1_API_PORT4791 and the shared browser cache.

## Visual inspection

Inspected actual generated captures once at 360px phone and 1920×1080 public display. The phone Manager offer shows a readable neutral initial fallback, fixed price, open detailed 1–100 attributes, explicit fictional provenance, implemented development explanation, inactive attributes and the post-hire reserve. The driver offer shows recognisable loaded portraits, separate preference text and conditional standing-start/wet labels; long content wraps without horizontal overflow. The public display has no private offer/roster panel. Existing long acquisition pages remain scrollable; shared card presentation and comprehensive worst-case layouts are #24's responsibility.

- [Manager phone](03-personnel/s16-23-manager-360.png)
- [Driver phone](03-personnel/s16-23-drivers-360.png)
- [Public display](03-personnel/s16-23-acquisition-public-1920.png)

This is complete #23 slice evidence, not a full 23-event runtime/whole-season balance acceptance claim. #25 still owns the integrated full-calendar campaign proof and release-wide balance limits.

## Canonical status publication blocked

The attempted action was a GitHub PATCH of issue23 (review-status label/body) followed by POST to issue23/comments (implementation/evidence summary). Automatic approval review rejected the entire shell invocation before execution: it considered publishing implementation details, commit identifiers, test evidence and repository paths to that destination unauthorized because it did not recognize specific user authorization for that payload. No final mutation was sent, and no retry or workaround was attempted.

The canonical issue remains open with its prior claim/status. Code and local verification are complete. The coordinating root owns subsequent canonical updates after independent review and checking the existing repository/workflow authorization; this implementer will not retry the rejected mutation. No separate human approval flow is added by this ticket.
