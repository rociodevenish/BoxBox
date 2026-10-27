# Driver card sample design specification

Reference prototype: [Grid / Driver Lab](https://grid-driver-lab.seandevenish.chatgpt.site/).
Recovered Angular, HTML/CSS and image inventory: [Driver card mockups](driver-card-mockups/README.md). PNG binaries remain to be imported; see the recovery status in that document.
Captured: 8 October 2026.

This sample preserves the original design handoff recovered from the chat that created the reference prototype. It records design intent and illustrative game data rather than establishing a production implementation ticket.

## Selected design and subsequent refinements

- Direction A — Broadcast was selected.
- Compact, expanded and TV states share charcoal surfaces, bold italic typography and coral driver accents, with quieter outlined preference tags.
- Both sample drivers (Max Verstappen and Lewis Hamilton) share the same coral role accent. Team ownership adds a separate badge and contextual border.
- Portraits remain separate assets; names, costs, ratings and controls are live, data-driven Angular UI.
- The prototype supports opening details, comparing two drivers, selecting a driver and replaying the TV selection announcement, with desktop, tablet and phone preview modes.
- The diagonal coral slash belongs behind the driver portrait. Use transparent portrait cutouts so it remains visible around the subject without covering the face.
- Latest reference update (published version 4, checked 8 October 2026): coral and secondary gray (`#596065`) diagonal stripes span the full portrait background. The gray stripe sits above the coral stripe with a gap; both have equal 32px gradient-stop widths at a 138-degree angle and share 0.8 opacity. Both remain behind the transparent driver cutout. The recovered sample stylesheet includes this change.
- Portraits, prices and ratings are illustrative. The originating implementation reported passing interaction and build checks; phone and TV layouts still required visual review.

## Original design handoff

The source handoff ends at the unfinished heading “16. Phase 7 —”. It is preserved below as supplied; no missing requirements have been invented.

---

# F1 Strategy Game — Digital Card Design Handoff

## Purpose

Continue designing the **digital card system and visual language** for a web-native F1 strategy game.

The immediate goal is to establish one excellent **Driver Card** design, turn it into a reusable digital component, and then extend that design language across the rest of the game.

This is not a physical-card project.

The final cards should be **data-driven Angular UI components**, with separate photographic/art assets, rather than flattened card images.

Generated images may be used extensively during the concept/design phase.

---

# 1. Game Context

The game is an F1 strategy game with a strong board-game influence.

Players each control one constructor/team across an entire season.

The game combines:

- pre-season personnel selection;
- car construction;
- between-race development;
- weekend setup;
- qualifying;
- Grand Prix strategy.

The visual identity should sit between:

**premium Formula 1 broadcast graphics**

and

**high-end modern board/trading-game presentation**.

It should not feel:

- fantasy/RPG;
- cartoonish;
- like FIFA Ultimate Team;
- like a spreadsheet-heavy management sim.

The game is intended to be enjoyable on a large TV in a living room, while phones/tablets can act as private controllers.

Cards therefore need to work at:

- large TV scale;
- tablet scale;
- phone scale.

---

# 2. Technical Context

Frontend:

**Angular**

Race-board 3D:

**Babylon.js**

Cards and UI:

**Angular + HTML + CSS + SVG**

Do not build the cards inside Babylon.js.

The card UI should remain normal responsive web UI.

Photographs/illustrations should be separate assets referenced by the card data.

Example concept:

```ts
interface DriverCard {
  id: string;
  name: string;
  cost: number;

  pace: number;
  racecraft: number;
  tyres: number;
  consistency: number;
  technical: number;

  detailedStats: {
    qualifyingPace: number;
    racePace: number;

    overtaking: number;
    defending: number;
    starts: number;

    tyrePreservation: number;
    tyreWarmup: number;

    errorControl: number;
    wetWeather: number;

    setupFeedback: number;
    adaptability: number;
  };

  preferences: DriverPreference[];
  portraitAsset: string;
}
```

The card visual should be driven entirely from this data.

---

# 3. Locked Driver Attributes

## Simplified Driver Stats

These are the primary ratings visible on the normal Driver Card:

**PACE**

**RACECRAFT**

**TYRES**

**CONSISTENCY**

**TECHNICAL**

Use a clear simple rating system, likely 1–5 visually.

---

## Detailed Driver Stats

These appear in an expanded/details state.

### Pace

- Qualifying Pace
- Race Pace

### Racecraft

- Overtaking
- Defending
- Starts

### Tyres

- Tyre Preservation
- Tyre Warm-up

### Consistency

- Error Control
- Wet-Weather Skill

### Technical

- Setup Feedback
- Adaptability

---

# 4. Driver Preferences

Preferences are **not core ratings**.

They describe compatibility with car/setup characteristics.

Examples:

- Stable Rear
- Strong Front End
- Strong Braking Platform
- High-Speed Confidence
- Traction Sensitive

Preferences should appear as compact traits/tags rather than additional stat bars.

Aggression is **not** a separate driver attribute.

---

# 5. Other Card Types

Once the Driver Card design is established, derive the following card types from the same visual system.

## Manager

Primary attributes:

- Leadership
- Operations
- Development
- Efficiency

---

## Race Engineer

Primary attributes:

- Strategy
- Setup
- Tyre Reading
- Communication

---

## Technical Engineer

Attributes:

- Aerodynamics
- Mechanical / Chassis
- Power Systems
- Reliability
- Development Efficiency

---

## Race Package

A Race Package is a selectable car specification.

Each team may select **one Race Package per Grand Prix**.

The card should clearly communicate:

- package name;
- visual/technical illustration;
- primary strengths;
- compromises;
- circuits/situations where it is useful.

Example:

### Low-Drag Aero Package

+3 Top Speed  
+1 Acceleration  
−1 Low-Speed Cornering

---

## Track Card

The Track Card should communicate both performance requirements and race characteristics.

### Simplified Performance

- Speed
- Cornering
- Mechanical Grip
- Tyres

### Race Characteristics

- Overtaking Difficulty
- Pit-Lane Loss
- Safety-Car Risk
- Mechanical Stress
- Weather Volatility

The Track Card should be highly visual, with circuit shape strongly featured.

---

# 6. Core Card Design Principles

The card system should be:

### Readable

Headline stats must be understandable at a glance.

### Premium

Use strong typography, spacing, subtle texture and photographic presentation.

### Restrained

Avoid excessive gradients, glowing borders, rarity effects or fantasy-game conventions.

### Hierarchical

The player should immediately understand:

1. who/what the card is;
2. cost/value;
3. primary ratings;
4. preferences/special characteristics.

### Expandable

The compact card should show simplified information.

Click/tap/expand reveals detailed attributes.

### Responsive

The same underlying component should adapt cleanly to:

- TV;
- desktop;
- tablet;
- phone.

---

# 7. Suggested Visual Direction

Start with a **Driver Card**.

Recommended concept:

Large portrait occupying approximately the upper 45–55% of the card.

Driver surname/name prominent.

Cost placed consistently in the upper-right or another highly visible location.

Lower area contains five headline stats.

Potential structure:

```text
┌───────────────────────────────┐
│                         COST  │
│                               │
│       DRIVER PORTRAIT         │
│                               │
│                               │
├───────────────────────────────┤
│ MAX VERSTAPPEN                │
│ Driver                        │
│                               │
│ PACE             ★★★★★        │
│ RACECRAFT        ★★★★★        │
│ TYRES            ★★★★         │
│ CONSISTENCY      ★★★★★        │
│ TECHNICAL        ★★★★         │
│                               │
│ [Stable Rear] [Late Braking]  │
└───────────────────────────────┘
```

This is only a layout starting point.

Explore alternatives.

---

# 8. Colour Strategy

Do not bind a driver/personnel card permanently to their real-world constructor.

Personnel can move to different teams during pre-season.

Therefore card identity should not rely heavily on current real-world team colours.

Possible approach:

Neutral premium base:

- charcoal;
- off-white;
- brushed-metal;
- graphite;
- restrained accent colours.

Then use:

**role/category accents**

rather than team accents.

For example:

Driver — one accent family  
Manager — another  
Race Engineer — another  
Technical Engineer — another  
Race Package — technical/engineering palette  
Track — location/event palette

The team owning the card can add a subtle contextual border/accent elsewhere in the UI rather than becoming baked into the card artwork.

---

# 9. Phase 1 — Visual Exploration

Produce approximately **4 substantially different Driver Card concepts**.

They should differ meaningfully in:

- portrait treatment;
- stat layout;
- typography;
- border/frame style;
- cost placement;
- preferences treatment.

Do not merely create minor colour variants.

Suggested design directions:

### Direction A — Broadcast

Clean F1 television graphics.

Very strong typography.

Minimal border.

Large photography.

---

### Direction B — Premium Trading Card

Slight physical-card influence.

Subtle materials and depth.

Still modern and restrained.

---

### Direction C — Technical / Motorsport

More engineering/data influenced.

Grid structures.

Technical textures.

Still visually rich.

---

### Direction D — Editorial

Magazine/editorial photography.

Very elegant typography.

Minimal visual clutter.

---

# 10. Phase 1 Deliverable

Present the four concepts side by side and evaluate them against:

- TV readability;
- phone readability;
- premium feel;
- clarity of five stats;
- suitability for all personnel card types;
- ease of implementing in HTML/CSS;
- ability to animate.

Choose one direction or combine selected elements into a fifth refined concept.

Do not move into full component implementation until the visual hierarchy is agreed.

---

# 11. Phase 2 — Driver Card Refinement

Take the chosen direction and design:

### Compact Driver Card

Used for:

- pre-season selection;
- team roster;
- comparison;
- timing/event overlays where appropriate.

### Expanded Driver Card

Used when the player opens details.

Shows:

- all detailed stats;
- preferences;
- explanation/tooltips;
- potentially historical/season information later.

### TV Card

A larger presentation state suitable for major events:

- driver selected;
- qualifying result;
- overtake highlight;
- race winner;
- championship leader.

The three states should clearly belong to the same design system.

---

# 12. Phase 3 — Interaction Design

Define card interactions.

Examples:

### Hover

Subtle elevation / highlight.

### Tap / Click

Open detailed state.

### Selection

Card visibly becomes selected.

### Acquisition

Card animates into team ownership.

### Locked

Subtle lock state after pre-season selection.

### Comparison

Two or three cards shown side-by-side with aligned stats.

### Recommended

Guided/Assisted play may place a subtle recommendation indicator on a card.

Avoid excessive animation.

Animations should feel premium and fast.

---

# 13. Phase 4 — Data-Driven Angular Prototype

Convert the selected visual design into a reusable Angular component.

Suggested concept:

```html
<game-card>
    <driver-card-content />
</game-card>
```

Potential component hierarchy:

```text
GameCardComponent
    CardPortraitComponent
    CardHeaderComponent
    CardCostComponent
    CardStatListComponent
    CardTraitListComponent
```

Do not over-componentise initially.

Prefer CSS custom properties/tokens for design variation.

Example:

```css
--card-accent
--card-background
--card-border
--card-title-size
--card-radius
--card-shadow
```

---

# 14. Phase 5 — Design Tokens

Create a shared card design system.

Define:

### Typography

- display font;
- body font;
- stat font;
- numeric font.

### Spacing

Consistent spacing scale.

### Radius

Card corners and chips.

### Shadows

TV/desktop/mobile levels.

### Colours

Neutral surfaces plus card-role accents.

### Stat Treatments

Stars, bars, numbers or hybrid treatment.

### Trait Chips

Preference/specialty presentation.

This should make future card types cheap to build.

---

# 15. Phase 6 — Extend to Personnel Cards

Using the Driver Card system, implement:

1. Manager
2. Race Engineer
3. Technical Engineer

These should reuse the same:

- portrait area;
- cost location;
- title hierarchy;
- stats framework;
- trait treatment.

Avoid making every card category visually unrelated.

The player should immediately recognise all of them as belonging to one game.

---

# 16. Phase 7 —
