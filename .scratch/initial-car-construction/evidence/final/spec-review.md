# Spec review — S13

Compared `05ae54c3ca4e67e57e99be7865e1348488260425...818ad34e362314c1cd015e6bd1af507bc6f39ff6` read-only against the parent spec, four tickets, source menu, implementation and published evidence. No tests or services were run.

- **[P3] Update the personnel ledger and reserve wording for purchased cars.** `src/frontend/src/app/campaign/campaign-page.html:60–62` still says “Your minimum car reserve remains protected” whenever `own.rosterReady` is true, and labels every non-starting-credit debit “Personnel hire.” New confirmation now adds a `base-car-construction` debit (`src/backend/Game.Domain/CarConstruction.cs:73`), and `ProjectAcquisition` returns that same ledger (`src/backend/Game.Application/CampaignCoordinator.Acquisition.cs:85`). Thus reopening personnel after an accepted purchase shows its construction cost as another personnel hire and claims an unspent reserve even when a legitimate baseline leaves zero credits. This contradicts the newly corrected paid-car wording higher in the same panel and misrepresents the spending history. Spec: “As a player, I want costs and remaining budget visible, so that future flexibility is understandable.” Render a construction reason and make the final-roster reserve sentence conditional on whether the car is paid.

No additional actionable missing mechanics or scope creep found in the reviewed code. Confirmation uses one conditional campaign write for ledger/car/all-calendar presets; readiness guards precede preparation, and the source menu/credit reconciliation and once-only composition are explicit.

Known acceptance gap, accurately disclosed: ticket 04 remains ready-for-human with actual choice comprehension **UNMEASURED**. Spec requires “Record real readability and choice-comprehension findings”; engineering captures do not complete that human acceptance criterion. Full-calendar evidence is expressly deferred. Published 1,300-race evidence reports the absent cornering crossover, low-drag local dominance and zero observed retirement reduction rather than asserting balance. These disclosed limitations are not additional implementation defects.

Total actionable findings: 1 (worst P3).
