# Ticket02 rendered driver gallery review

Reviewed 8 October 2026 in Chromium 151.0.7922.34 using Playwright 1.62.1; Angular 22.2.1, TypeScript 6.0.3, Tailwind 4.3.3, Node 24.19.0. Reinspected shared assets: same 28 driver images plus duplicate George, no newly supplied track assets. Production fixtures consume the existing data-driven portrait manifest and packaged unmodified Max/Lewis copies. Original user files remain untouched.

## Evidence and judgment

All12 `angular-*` PNG crops were opened and visually inspected. The suffix identifies viewport width, not the crop size. Machine manifests `angular-captures-360.json`, `angular-captures-768.json`, `angular-captures-1920.json` record viewport and actual PNG dimensions separately.

| Viewport | Compact section | Expanded card | TV card | Long-name dialog |
| --- | --- | --- | --- | --- |
|360×800|328×2050|328×895|328×727|328×358|
|768×1024|704×1375|384×895|704×503|700×240|
|1920×1080|1216×701|384×895|1216×502|700×240|

Phone: all essential names and live controls fit; Montgomery-Sutherland wraps at its hyphen. Unknown values are written, not zero. Cards and details scroll vertically without horizontal overflow. Missing portrait caption initially met the first-name line; moved it into the body so initials/name and the explanation remain separate. Ownership and illustrative prices occupy opposite corners and do not cover faces. Dialog close target remains44px or larger and full long identity wraps.

Tablet: two compact cards share a row, edge case follows below. Expanded card stays384px wide to avoid the overly tight face crop seen in its first576px version. TV switches to a portrait/stat two-column arrangement and fits at503px high. All five Simple groups, preferences and details access remain visible.

Desktop/display viewport: three compact cards form a scan row; TV card is1216×502 with a contained real portrait, large live surname and centered five-group column. This deliberately adapts01's full-width stat TV composition into a reusable landscape card. Phone TV stacks the same content. It remains a card treatment, not proof of living-room text readability; real TV viewing-distance review remains pending. The gallery page scrolls between sections; this does not replace01's separate genuine1920×1080 public composition.

## Baseline preserved and adaptations

Charcoal/coral tokens are imported unchanged. Gray and coral stripes keep138degrees, equal32px gradient-stop widths,30px gap and0.8 opacity behind cutouts. Solid charcoal supports the coral Driver role label; Apex ownership stays separately turquoise. Illustrative12/11-credit price labels are view-model strings, announced as sample content by the gallery header. Display ratings remain1–5 or Unknown; detailed fixtures are labelled outside the reusable live card and never converted into engine inputs.

Text/outline colours remain the contrast-checked01 pairs. Written preferences, ratings, role/ownership and Unknown survive without colour. Native labelled buttons, dialogs and article names give keyboard/assistive access. The focus outline is3px with4px offset. Reduced-motion CSS is inherited from tokens; all three layout captures were made with reduced motion. No required motion or animated announcement was added.

## Checks and limits

Eight rendered tests pass. The details/focus-return,404 fallback and unknown/long-name behaviors were observed failing before their implementations. The keyboard check exposed native Tab moving into browser chrome, including Shift+Tab; the text-only modal now retains both directions on its sole Close control while native Escape restores origin focus. Touch opening/closing passed at 360px. Reflow and all primary button dimensions≥44px passed at 360/768/1920. Production build and strict Angular template/type checking pass; package audit reports zero findings. Bundle148.18KB raw / 43.78 KB estimated transfer, excluding images.

Supplied PNGs are preserved at1024×1536, about 4.5 MB for two. Development Angular reports oversized image delivery; this is documented rather than hidden. Smaller responsive renditions remain a later delivery optimization. No race orchestration or3D dependency appears in source or package imports. No unit tests were added for docs, CSS nesting or private methods.
