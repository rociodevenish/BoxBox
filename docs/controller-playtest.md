# Controller playtest presentation

The controller now confirms **Approve Plan** atomically for both cars. Its local result presentation hides decision and driver panels until recorded feedback completes (three seconds by default), is skipped, or reduced motion reconciles immediately. The resolved Classification is visible throughout controller feedback, so positions and gaps precede the next choices. The server automatically opens the next authoritative window independently of every display. Refresh retrieves the current outcome and choices. Replaying feedback does not relock choices already revealed. Shared required-team waiting will use **Waiting for other teams…**; solo resolution advances directly to **Resolving play…**.

The live heading is **Decisions for the next X laps**, using the server's existing five-leader-lap window capped by remaining race distance. Finished races show final instructions. Nearby Classification includes the controlled position and at most two positions each side; at a field edge fewer rows exist. Interval is the recorded public interval to the immediately preceding car, including explicit unavailable/lapped values. Driver buttons open the existing detailed player card.

Recent timing is the last five completed **full** lap intervals, chronological. A duration requires two recorded `LapCompleted` finish-line crossings. The initial rolling partial lap is excluded. Finish-line elapsed time includes intervening pit loss. Before two crossings the controller says no full lap times are recorded. Values round the whole duration to milliseconds before formatting minutes and seconds.

The fastest-lap marker uses the smallest unrounded recorded full-lap duration across the entire race, even if it is older than recent history. Exact ties choose the earliest completion timestamp, then ordinal driver ID. Completed pit stops count only recorded `PitExit`, once per recorded event; entry and tyre-change events do not increment the count. Projection is recomputed from authoritative engine history, so retry, refresh and playback do not increment it. None of these fields changes physics or replay hashes.

## Player cards during testing

`race-driver-card.ts` pins an explicit fixture-to-source-card ID list. Synthetic `driver-01` through `driver-22` remain the preset simulation participants and are displayed as Driver 1 through Driver 22. Their cards use the published provisional collection `.docs/data/f1_strategy_driver_cards_2026.json`, supplied portrait manifest, headline 1–5 values, detailed 1–100 values, preferences and cost. The mapping was initially authored from the collection's race-driver sort order and is now an explicit list. Changing source sort order cannot silently remap identities.

This is a presentation mapping for playtesting. The provisional source ratings do not describe or replace the equal/mixed synthetic engine attributes. No stars or detailed values are converted into physics. Missing portraits retain the existing readable fallback. Original assets and source provenance remain intact.

Tyre selection uses keyboard-operable compound badge buttons, with explicit **New compound Hard/Medium/Soft** accessible labels and pressed state. The confirmation modal still summarizes both complete instructions and preserves focus behavior.

Live portrait variants total 715,346 bytes for 22 supplied race drivers; the initial two controlled portraits total 60,776 bytes. Existing legacy gallery PNGs remain separate from live variants and bundle measurements.
