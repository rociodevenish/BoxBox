# Compact controller verification — 10 October 2026

Controller cards retain the supplied portrait/name design while hiding the headline ratings, preferences, ownership and cost. View driver details opens the existing accessible stats/preferences dialog; Escape restores focus. Full gallery cards remain unchanged.

Nearby Classification is the existing public TimingTower/TimingRow, filtering the current authoritative classification to the driver and up to two ranks either side. It uses Interval, compound/age, completed stops, fastest marker and the existing stop-status rendering. Optional driver-name buttons open the existing analysis dialog. Its live source remains current while old outcome playback is replayed.

The shared tower now responds to its own container width, so the same presentation fits both a phone controller card and the public display. Early phone inspection caught cramped names; a selected final table capture caught a clipped Stops heading. Both were corrected before integration review. Final evidence shows complete names, interval, full Medium badge, age and Stops inside the card. The public display retains all 22 rows within a genuine 1920×1080 viewport.

Approved browser seam regression began red against the previous full card. The initial test queried a hidden detailed-attribute label too; its final assertion instead checks headline/ preference absence in the visible article, preserving a meaningful identity-only requirement.

Validation:
- Five affected live browser scenarios passed: compact identity and shared classification, accessible details/focus return, outcome-first presentation, bounded vicinity/recorded lap pace, legal battle choices and public TV layout. A final focused compact scenario passed again after correcting Stops column width.
- Thirteen affected gallery scenarios passed for full cards, failed portraits, detail keyboard/touch access, timing gaps/lap deficits/stop status, phone compound labels, two-car confirmation and rejected drafts.
- Angular typecheck and production build passed before the final CSS column-width correction: 346.74 KB raw / 82.78 KB transfer estimate. Existing 300 KB budget and qrcode/dijkstrajs CommonJS warnings remain. The parent performs one complete integrated verification including the final build.
- Ordinary tests changed no historical tracked evidence. Captures were written to ignored output and explicitly published with tools/publish-evidence.mjs.

Evidence: compact-phone.png (360px full scrollable phone), nearby-phone.png (actual phone classification crop), public-tv.png (1920×1080).

Review correction: removed duplicate nearby-window selection. The exact selected public rows now supply both the timing facts and the driver-detail cards, using fresh authoritative state during replay. Three focused browser scenarios passed after this correction (compact card/edge window/drill-down, outcome-first/replay, recorded laps and completed stops).
