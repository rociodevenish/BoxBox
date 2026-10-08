# 02: Build reusable driver cards and the executable gallery

**What to build:** Players and designers can run an Angular gallery and interact with compact, expanded and TV driver cards using the established visual system.

**Blocked by:** 01 — Establish and validate the visual design system.

**Status:** ready-for-agent

- [ ] Establish the game frontend workspace and independently runnable fixture gallery using the approved tokens and card anatomy.
- [ ] Configure Tailwind CSS for layout, spacing and responsive utilities, mapping approved semantic tokens into its theme; use component CSS for distinctive Broadcast graphics and complex treatments.
- [ ] Cards use an explicit view model, separate portraits and live names, attributes, preferences and ownership; prices and ratings are illustrative gallery content.
- [ ] Compact, expanded and TV variants share the same identity and styling; details can be opened and closed with keyboard and touch.
- [ ] Use locally available assets with recorded provenance or neutral placeholders; failed portraits retain useful identity and controls.
- [ ] Recheck the supplied design-asset collection before implementation and use suitable driver imagery rather than assuming the recovered prototype's portraits are still missing. Keep asset references in data and document frontend packaging so new driver, track and other assets can be adopted without component redesign.
- [ ] Cover long names, missing portraits, unknown attributes and detailed fixture attributes without implying unsupported engine capabilities.
- [ ] Verify visible card interactions, focus, responsive layouts and reduced motion through rendered gallery behaviour; inspect representative screenshots and record their dimensions and findings.
- [ ] Build and type-check the workspace, document gallery execution and keep it independent of a 3D renderer and race orchestration.
