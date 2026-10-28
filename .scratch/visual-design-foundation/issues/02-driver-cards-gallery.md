# 02: Build reusable driver cards and the executable gallery

> GitHub issue: [#92](https://github.com/rociodevenish/BoxBox/issues/92) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Players and designers can run an Angular gallery and interact with compact, expanded and TV driver cards using the established visual system.

**Blocked by:** 01 — Establish and validate the visual design system.

**Status:** resolved

- [x] Establish the game frontend workspace and independently runnable fixture gallery using the approved tokens and card anatomy.
- [x] Configure Tailwind CSS for layout, spacing and responsive utilities, mapping approved semantic tokens into its theme; use component CSS for distinctive Broadcast graphics and complex treatments.
- [x] Cards use an explicit view model, separate portraits and live names, attributes, preferences and ownership; prices and ratings are illustrative gallery content.
- [x] Compact, expanded and TV variants share the same identity and styling; details can be opened and closed with keyboard and touch.
- [x] Use locally available assets with recorded provenance or neutral placeholders; failed portraits retain useful identity and controls.
- [x] Recheck the supplied design-asset collection before implementation and use suitable driver imagery rather than assuming the recovered prototype's portraits are still missing. Keep asset references in data and document frontend packaging so new driver, track and other assets can be adopted without component redesign.
- [x] Cover long names, missing portraits, unknown attributes and detailed fixture attributes without implying unsupported engine capabilities.
- [x] Verify visible card interactions, focus, responsive layouts and reduced motion through rendered gallery behaviour; inspect representative screenshots and record their dimensions and findings.
- [x] Build and type-check the workspace, document gallery execution and keep it independent of a 3D renderer and race orchestration.

## Answer

Built the Angular/Tailwind workspace, explicit reusable DriverCard view model and compact/expanded/TV gallery. Shared tokens and supplied portrait manifest are consumed directly; assets were rechecked and originals preserved. Native details dialogs, keyboard focus/escape/return, touch, failed-image fallback, long names and unknown ratings are covered through the agreed rendered seam. Eight browser tests, production build and strict template/type checking pass. [Review evidence](../evidence/driver-gallery-review.md) records inspected screenshots with actual dimensions, preserved rules and deliberate adaptations. [Run instructions](../../../src/frontend/README.md) describe Node24, pinned dependencies, port4400 and browser reuse. No race orchestration or3D renderer is imported. Real TV distance and responsive image-delivery optimization remain documented limits.
