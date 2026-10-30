# S16 factual provenance and candidate audit

Research completed/accessed **2026-10-30**, using the client date. Scope: factual identities, calendar/event order and starting roster; this is not real-world performance calibration or a rights clearance. Stable IDs below are repository identifiers, not identifiers issued by FIA or Formula 1.

## Snapshot decision

Select **2026 revised championship calendar, official F1/FIA snapshot accessed 2026-10-30**, with **23 active events**, paired with the **2026 season-opening contracted driver roster**. Freeze both descriptions with the content version. This is explicitly not the originally announced 24-event schedule and not an assertion that the same drivers started every actual race. The current [F1 schedule](https://www.formula1.com/en/racing/2026) and [FIA season calendar](https://www.fia.com/events/fia-formula-one-world-championship/season-2026/2026-fia-formula-one-world-championship) agree on the active event order. Neither live page exposes a reliable publication/update timestamp; access date is the snapshot date.

Material amendment chain:

- F1's [14 March 2026, 22:00 UTC announcement](https://www.formula1.com/en/latest/article/bahrain-and-saudi-arabian-grands-prix-will-not-take-place-in-april.1hnqllVG85RSt8pbFc5Ivx) removed the April Bahrain/Saudi Arabian weekends without April replacements. Its wording concerns April, so it does not by itself prove permanent season cancellation.
- F1's [26 July 2026, 10:30 UTC announcement](https://www.formula1.com/en/latest/article/formula-1-and-fia-confirm-that-malaysia-will-join-the-2026-calendar-as-host-venue-for-the-bahrain-grand-prix.6lL7vjFEM2VVynRHvg1TCf) places the Bahrain-branded event at Sepang, Malaysia, on 2–4 October between Azerbaijan and Singapore. The [FIA's matching announcement, dated 26 July 2026](https://www.fia.com/news/fia-and-fom-confirm-malaysia-will-join-2026-calendar-host-venue-bahrain-grand-prix), independently gives the same circuit, dates and position. Both July notices were conditional on final agreements/sign-off, including WMSC approval; do not erase that historical qualification.
- Later operational evidence resolves that conditional announcement for this snapshot: the current [F1 Bahrain event page](https://www.formula1.com/en/racing/2026/bahrain) presents a completed Malaysia event at Sepang; the FIA season calendar explicitly labels Bahrain/Sepang and explains that the venue is in Malaysia. The FIA's [30 September 2026 conference schedule](https://www.fia.com/news/f1-2026-bahrain-gp-malaysia-schedule-press-conferences) covers the Malaysia event through Sunday 4 October. This report does not claim to have located the formal WMSC approval instrument.
- Saudi Arabia is absent from F1's active rounds; FIA retains the 19 April Jeddah row visibly marked Called Off. A retained cancelled row must not become a playable Campaign Event. Remaining Qatar and Abu Dhabi events remain listed in this snapshot; no cancellation is inferred from external speculation.

**Identity exception:** `event-bahrain-gp-malaysia-2026` maps to `track-sepang`, not Sakhir/Bahrain International Circuit. Bahrain is the event brand; Malaysia is the host country. An event-brand flag and circuit-location flag are different concepts. `event-spanish-gp-2026` maps to Madrid/Madring; Barcelona-Catalunya is a separate event and circuit. FIA calls the Madrid venue Madrid IFEMA; [F1's Spain page](https://www.formula1.com/en/racing/2026/spain) calls it Madring. Those names are aliases, not separate circuits.

## Verified ordered mapping

Order and weekend dates match the current official F1 schedule. Circuit identities are cross-checked against the FIA season calendar and each linked official F1 event page (all accessed 2026-10-30; live event pages have no explicit publication date). Dates are calendar dates, not session UTC conversions. The candidate's 23 rows agree in order, dates and circuit mapping. These factual fields do not verify the candidate's lap lengths, counts, demand ratings or race tuning.

| Round | Event stable ID | Weekend | Circuit stable ID / identity | Primary event source |
| --- | --- | --- | --- | --- |
| 1 | `event-australian-gp-2026` | 2026-03-06–2026-03-08 | `track-albert-park` / Albert Park Grand Prix Circuit | [F1 australia](https://www.formula1.com/en/racing/2026/australia) |
| 2 | `event-chinese-gp-2026` | 2026-03-13–2026-03-15 | `track-shanghai` / Shanghai International Circuit | [F1 china](https://www.formula1.com/en/racing/2026/china) |
| 3 | `event-japanese-gp-2026` | 2026-03-27–2026-03-29 | `track-suzuka` / Suzuka Circuit | [F1 japan](https://www.formula1.com/en/racing/2026/japan) |
| 4 | `event-miami-gp-2026` | 2026-05-01–2026-05-03 | `track-miami` / Miami International Autodrome | [F1 miami](https://www.formula1.com/en/racing/2026/miami) |
| 5 | `event-canadian-gp-2026` | 2026-05-22–2026-05-24 | `track-circuit-gilles-villeneuve` / Circuit Gilles-Villeneuve | [F1 canada](https://www.formula1.com/en/racing/2026/canada) |
| 6 | `event-monaco-gp-2026` | 2026-06-05–2026-06-07 | `track-monaco` / Circuit de Monaco | [F1 monaco](https://www.formula1.com/en/racing/2026/monaco) |
| 7 | `event-barcelona-catalunya-gp-2026` | 2026-06-12–2026-06-14 | `track-barcelona-catalunya` / Circuit de Barcelona-Catalunya | [F1 barcelona-catalunya](https://www.formula1.com/en/racing/2026/barcelona-catalunya) |
| 8 | `event-austrian-gp-2026` | 2026-06-26–2026-06-28 | `track-red-bull-ring` / Red Bull Ring | [F1 austria](https://www.formula1.com/en/racing/2026/austria) |
| 9 | `event-british-gp-2026` | 2026-07-03–2026-07-05 | `track-silverstone` / Silverstone Circuit | [F1 great-britain](https://www.formula1.com/en/racing/2026/great-britain) |
| 10 | `event-belgian-gp-2026` | 2026-07-17–2026-07-19 | `track-spa-francorchamps` / Circuit de Spa-Francorchamps | [F1 belgium](https://www.formula1.com/en/racing/2026/belgium) |
| 11 | `event-hungarian-gp-2026` | 2026-07-24–2026-07-26 | `track-hungaroring` / Hungaroring | [F1 hungary](https://www.formula1.com/en/racing/2026/hungary) |
| 12 | `event-dutch-gp-2026` | 2026-08-21–2026-08-23 | `track-zandvoort` / Circuit Zandvoort | [F1 netherlands](https://www.formula1.com/en/racing/2026/netherlands) |
| 13 | `event-italian-gp-2026` | 2026-09-04–2026-09-06 | `track-monza` / Autodromo Nazionale Monza | [F1 italy](https://www.formula1.com/en/racing/2026/italy) |
| 14 | `event-spanish-gp-2026` | 2026-09-11–2026-09-13 | `track-madring` / Madring | [F1 spain](https://www.formula1.com/en/racing/2026/spain) |
| 15 | `event-azerbaijan-gp-2026` | 2026-09-24–2026-09-26 | `track-baku` / Baku City Circuit | [F1 azerbaijan](https://www.formula1.com/en/racing/2026/azerbaijan) |
| 16 | `event-bahrain-gp-malaysia-2026` | 2026-10-02–2026-10-04 | `track-sepang` / Sepang International Circuit | [F1 bahrain](https://www.formula1.com/en/racing/2026/bahrain) |
| 17 | `event-singapore-gp-2026` | 2026-10-09–2026-10-11 | `track-marina-bay` / Marina Bay Street Circuit | [F1 singapore](https://www.formula1.com/en/racing/2026/singapore) |
| 18 | `event-united-states-gp-2026` | 2026-10-23–2026-10-25 | `track-cota` / Circuit of The Americas | [F1 united-states](https://www.formula1.com/en/racing/2026/united-states) |
| 19 | `event-mexico-city-gp-2026` | 2026-10-30–2026-11-01 | `track-mexico-city` / Autodromo Hermanos Rodriguez | [F1 mexico](https://www.formula1.com/en/racing/2026/mexico) |
| 20 | `event-sao-paulo-gp-2026` | 2026-11-06–2026-11-08 | `track-interlagos` / Autodromo Jose Carlos Pace | [F1 brazil](https://www.formula1.com/en/racing/2026/brazil) |
| 21 | `event-las-vegas-gp-2026` | 2026-11-19–2026-11-21 | `track-las-vegas` / Las Vegas Strip Circuit | [F1 las-vegas](https://www.formula1.com/en/racing/2026/las-vegas) |
| 22 | `event-qatar-gp-2026` | 2026-11-27–2026-11-29 | `track-lusail` / Lusail International Circuit | [F1 qatar](https://www.formula1.com/en/racing/2026/qatar) |
| 23 | `event-abu-dhabi-gp-2026` | 2026-12-04–2026-12-06 | `track-yas-marina` / Yas Marina Circuit | [F1 united-arab-emirates](https://www.formula1.com/en/racing/2026/united-arab-emirates) |

## Eleven constructor identities and starting drivers

The [FIA Chinese GP entry list, document 14, 13 March 2026 at 10:35](https://www.fia.com/system/files/decision-document/2026_chinese_grand_prix_-_entry_list.pdf) supports all eleven entrant identities and the 22-driver mapping below. The dated [F1 season-opening roster article, 3 January 2026 at 10:28 UTC](https://www.formula1.com/en/latest/article/who-are-the-2026-formula-1-drivers.3mVj9UTWK7Puz2QuScnzuz) and current [F1 team directory](https://www.formula1.com/en/teams) corroborate the short identities. All accessed 2026-10-30. Table uses enduring short constructor identities; sponsor-laden entrant and engine/constructor strings in the FIA document are separate metadata, not extra teams.

| Candidate constructor ID | Short identity | Candidate starting-driver IDs (prefix `driver-`) |
| --- | --- | --- |
| `mclaren` | McLaren | `lando-norris`, `oscar-piastri` |
| `mercedes` | Mercedes | `george-russell`, `andrea-kimi-antonelli` |
| `red-bull-racing` | Red Bull Racing | `max-verstappen`, `isack-hadjar` |
| `ferrari` | Ferrari | `lewis-hamilton`, `charles-leclerc` |
| `williams` | Williams | `carlos-sainz`, `alexander-albon` |
| `racing-bulls` | Racing Bulls | `liam-lawson`, `arvid-lindblad` |
| `aston-martin` | Aston Martin | `fernando-alonso`, `lance-stroll` |
| `haas` | Haas | `esteban-ocon`, `oliver-bearman` |
| `audi` | Audi | `nico-hulkenberg`, `gabriel-bortoleto` |
| `alpine` | Alpine | `pierre-gasly`, `franco-colapinto` |
| `cadillac` | Cadillac | `sergio-perez`, `valtteri-bottas` |

All 22 candidate race-driver identities and team associations agree with this starting snapshot. Preserve existing IDs through display-name aliases: Andrea Kimi Antonelli / Kimi Antonelli, Oliver / Ollie Bearman, Alexander / Alex Albon, Nico Hülkenberg / Hulkenberg, Sergio Pérez / Perez. F1's January article has an apparent local typo in Piastri's paragraph referring to 2025 while introducing the 2026 grid; the dated FIA entry list supplies independent confirmation. No name spelling difference justifies a duplicate person.

The starting roster is not a historical race-by-race entry ledger. F1's [7 September 2026, 17:57 UTC report](https://www.formula1.com/en/latest/article/lawson-to-stay-at-red-bull-for-third-race-weekend-in-madrid-as-hadjars-recovery-continues.Bxrk9FOVDChcWCo5vxY8v) confirms Hadjar's wrist injury led to Lawson driving Red Bull and Tsunoda Racing Bulls at Zandvoort, Monza and Madrid. A game can retain the contracted starting roster for Personnel Acquisition, but must not describe Tsunoda as having made no 2026 race starts or claim the regular pairing participated unchanged throughout. The current directory again lists Hadjar/Verstappen and Lawson/Lindblad; this research makes no exhaustive assertion about every temporary substitute.

## Six extra candidate drivers

These are real people with source-backed reserve associations, not fictional fillers. The six-row selection is a game pool, not the complete real reserve list. The primary [F1 reserve overview, 25 February 2026 at 18:07 UTC](https://www.formula1.com/en/latest/article/who-are-the-reserve-drivers-for-each-f1-team-in-2026.Ce8YvwuVJGLkVNgERX21U.Ce8YvwuVJGLkVNgERX21U) corroborates all six associations. All sources below accessed 2026-10-30; assertions are dated role provenance, not guarantees of unlimited current availability or exclusive team service.

| Candidate stable ID | Factual association and qualification | Primary provenance |
| --- | --- | --- |
| `driver-yuki-tsunoda` | Red Bull and Racing Bulls test/reserve; also temporary 2026 race substitute as noted above | F1 February reserve overview; September substitution report |
| `driver-patricio-oward` | McLaren reserve; official team uses Pato O’Ward, candidate uses Patricio O’Ward | [McLaren announcement, 15 January 2026 at 14:00 UTC](https://www.mclaren.com/racing/formula-1/2026/reserve-drivers-and-driver-development-programme-line-up/). Shares reserve role with Leonardo Fornaroli; IndyCar duties mean availability is conditional. |
| `driver-zhou-guanyu` | Cadillac reserve, not Ferrari reserve in this selected season | F1 February overview; [Cadillac 5 January 2026 release](https://cadillacf1team.com/wp-content/uploads/2026/01/Cadillac-Formula-1_Reserve-Driver-Announcement-Press-Release1.pdf) indexed primary excerpt corroborates; direct fetch returned 403, so substantive verification relies on F1. |
| `driver-stoffel-vandoorne` | Aston Martin reserve; does not imply sole third driver | F1 February overview separately includes Jak Crawford. |
| `driver-antonio-giovinazzi` | Ferrari reserve alongside WEC racing commitments | F1 February overview. |
| `driver-jack-doohan` | Haas official reserve; not Alpine reserve for this snapshot | [Haas announcement, 3 February 2026](https://www.haasf1team.com/news/tgr-haas-f1-team-signs-jack-doohan-reserve-role), explicitly sharing duties with Ryo Hirakawa. |

No candidate extra-driver team association requires correction against these dated sources. Retain Pato/Patricio as one identity. The overview also names reserves omitted by the candidate (Fornaroli, Vesti, Browning, Iwasa, Crawford, Hirakawa, Aron, Maini); this demonstrates that six selected reserves must not be marketed as complete real-world coverage. No extra driver is automatically a mandatory starter or simultaneously duplicated into another unique acquisition slot.

## Audit of the four supplied design inputs

All four JSON documents were parsed in full. They are explicitly provisional design inputs; their internal research/confidence labels are not citations. Repository facts below are observations of the supplied files, not assertions made by FIA/F1.

| File under `.docs/data/` | Schema / data version | Audit conclusion |
| --- | --- | --- |
| `f1_strategy_track_cards_2026.json` | `1.0.0` / `2026.10.08-initial-balance` | 23 unique track IDs, 23 unique event IDs; every event resolves to a track. Revised event order/date/mapping agrees with the verified snapshot. `calendarBasis` names 8 October and should remain historical input metadata; release provenance names this verification date separately. |
| `f1_strategy_driver_cards_2026.json` | `1.0.0` / `2026.10.08-initial-balance` | 28 unique driver IDs: 22 race and six reserve; all eleven constructor-reference IDs are represented. Source-backed identity/role claims as above; costs 7–12 budget units are authored game values. Ratings and preferences remain provisional. |
| `f1_strategy_car_attribute_spec_2026.json` | `1.0.0` / `2026.10.08-initial-model` | Ten detailed attributes, five headline groups; nine demand mappings plus reliability resolved separately. Candidate vocabulary is not evidence of implemented consumers. |
| `f1_strategy_preseason_car_construction_2026.json` | `1.0.0` / `2026.10.09-preseason-car-build-v1` | References the car-attribute specification and same neutral baseline. Four mandatory choices, minimum category costs 5+5+4+4=18, maximum 6+6+6+6=24, recommended envelope20. A 24-cost combination conflicts with envelope20 and must be rejected or repriced under actual game budget rules. |

Cross-file demand mapping is explicit: `highSpeedAero`→`highSpeedCornering`, `lowSpeedAero`→`lowSpeedCornering`, `tyreWear`→`tyreDegradation`, `tyreTemperature`→`tyreThermalDemand`; top speed, acceleration/deployment, braking, traction and mechanical grip map by matching keys. `reliability` has no demand-key mapping and uses a separate mechanical-stress hypothesis. These mappings describe candidate design intent, not a licence to add unsupported simulation mechanics. Headline scales (1–5), detailed values (1–100), suggested effective clamp (60–100), money tokens (`budget-unit`, `budgetUnits`) and the engine's actual units need deliberate translation and validation. No star-to-physics conversion is verified here.

Two drivers at the candidate minimum already cost14, plus minimum construction18 totals32 before manager, race engineer and technical engineer. This is an arithmetic lower bound, not proof that 11 teams can complete acquisition: uniqueness, available offers, Completion Reserve, staff supply and configured total credits still need executable checks. Drivers alone supply no missing real staff facts. Any authored missing staff should be clearly fictional/illustrative and uniquely identified.

## Evidence boundaries and release handoff

Verified here: season snapshot, active event count/order/weekend dates, track identity mapping, eleven constructor short identities, contracted starting driver/team associations, and dated reserve provenance. Not verified here: numerical track geometry, lap/segment timings, passing zones, weather probabilities, Safety Car risk, Pit Loss, tyre/stress coefficients, ratings, compatibility traits, prices, image identities, or asset reuse rights. Circuit-specific qualitative prose is not a measured coefficient. Candidate preferences marked researched have no supplied citations and must remain authored compatibility hypotheses unless separately researched.

Source access establishes neither copyright/trademark permission nor portrait/photo rights. This report grants no licence, and filename/source availability cannot establish permission. Asset inspection and runtime validation remain separate S16 work. Active Campaigns must keep the selected content snapshot rather than re-query these live pages.

No production code or tests were changed by this research. No source-backed contradiction with the candidate's selected calendar or regular starting roster was found. The significant limitations are July approval conditionality resolved through later operational pages, FIA's retained Called Off Saudi row, Madrid venue aliases, and real in-season substitute drivers. Release-enabling validation, balance and every-circuit public simulation checks remain implementation responsibilities.
