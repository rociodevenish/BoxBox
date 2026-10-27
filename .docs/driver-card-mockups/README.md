# Driver card mockups

Reference: [Grid / Driver Lab](https://grid-driver-lab.seandevenish.chatgpt.site/).

Latest reference sync: published version 4, checked 8 October 2026. The stylesheet now includes full-width coral and gray (`#596065`) background stripes of equal width, with the gray stripe above the coral stripe and a gap between them. This was verified against the publication record and originating source-change history; direct browser inspection and a local build were unavailable because the Windows execution helper failed during setup.

Recovered from the prototype's original design chat on 8 October 2026. This folder contains the Angular prototype source and HTML/CSS, reconstructed from recorded file-creation commands and subsequent fixes. It is a documentation sample, separate from the game's application.

## Source

- `src/main.ts`: typed sample driver data, reusable driver-card and rating-pips components, inline Angular HTML templates, expanded details, comparison, selection and TV announcement.
- `public/index.html`: HTML entry point.
- `public/styles.css`: shared tokens, responsive styles and portrait layering that places the slash behind the driver.
- `public/favicon.svg`: prototype icon.
- `build.mjs` and `package.json`: original standalone Angular/esbuild setup.

The HTML card mockups are inline templates in `src/main.ts`; the original prototype did not create a separate static HTML card mockup.

## Images and recovery status

**PNG files have not been copied into this folder.** Their original cloud-workspace locations and generation prompts are preserved in [image-artifacts.md](image-artifacts.md). That inventory includes the four-direction concept board, the refined compact/expanded/TV board, original portraits and transparent portrait cutouts.

The live prototype uses these portrait assets:

- [Max Verstappen cutout](https://grid-driver-lab.seandevenish.chatgpt.site/assets/max-cutout.png)
- [Lewis Hamilton cutout](https://grid-driver-lab.seandevenish.chatgpt.site/assets/lewis-cutout.png)

The source expects those PNGs at `public/assets/max-cutout.png` and `public/assets/lewis-cutout.png`. Those files are currently missing. The source paths in the image inventory belong to a separate cloud workspace and are not local repository paths.

## Build and validation

After restoring the portrait assets, run `npm install`, then `npm run build`, and serve `dist/` with a static web server. No lockfile was recovered.

The originating chat reported successful build, type and interaction checks. This recovered copy has not been built or visually checked because local command execution fails during process setup. Phone and TV visual review remained outstanding in the original handoff.

All portraits, prices, preferences and ratings are illustrative. Both drivers use coral role accents; ownership adds a contextual badge and border. Selection is page-session state. Production integration should use the game's Angular build and authoritative game state.
