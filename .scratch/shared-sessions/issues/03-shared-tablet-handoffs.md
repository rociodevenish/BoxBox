# 03: Share a tablet with private handoffs

**What to build:** Several household players use one authorised tablet, revealing only the active team's private screen after an explicit handoff.

**Blocked by:** 02 — Lock multiple teams and resolve together.

**Status:** ready-for-agent

- [ ] Shared-device authority covers an explicit team list while participant identity remains separate from device identity.
- [ ] Show a neutral pass-to-team screen and require Ready before fetching/subscribing to the next team's private view; identify the active team clearly while editing.
- [ ] On lock or neutral exit, clear rendered private data, drafts and notifications and unsubscribe the previous stream; explain discarded unsubmitted drafts.
- [ ] Previous choices never remain visible behind the handoff screen and private views are excluded from offline/service-worker caches.
- [ ] Team identity and assignment revision prevent late messages from populating another team's view; refresh restores an appropriate authorised state without exposing the previous team during handoff.
- [ ] Verify complete shared-tablet interactions, keyboard/touch access, refresh and late updates through browser tests and API/SignalR checks.
- [ ] Explain the trusted-household-device privacy boundary without advertising handoff as independent authentication.
