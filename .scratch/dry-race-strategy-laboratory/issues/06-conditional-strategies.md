# 06: Run conditional strategies using team information

**What to build:** A designer can run reproducible strategies that react to public race state and imperfect team tyre estimates instead of following only a fixed schedule.

**Blocked by:** 03 — Execute pit strategies and enforce compound eligibility.

**Status:** resolved

- [x] Provide seeded imperfect tyre-condition estimates with equal estimation quality for all teams initially.
- [x] Conditional rules can use public race information and the driver's team estimates, including a demonstrated estimated-condition pit rule.
- [x] The strategy input contract excludes actual hidden tyre condition, future outcomes and opponents' locked decisions. Diagnostics may retain exact condition separately.
- [x] Conditional and scheduled decisions have documented, deterministic precedence and rule evaluation at phase boundaries.
- [x] Rule evaluation produces all phase choices before simulation resolution.
- [x] Invalid rules or produced decisions fail clearly; deliberate pace/pit omissions retain Standard/Stay Out defaults.
- [x] Reports retain estimates, resolved decisions and replay inputs to explain why rules fired.
- [x] Tests cover public/team information boundaries, deterministic estimates and decisions, rule precedence and errors. Reuse the established simulation test boundary and small runner checks.
- [x] The strategy interface accepts public-state evolution from later traffic/battle slices without coupling rules to their implementation internals.
