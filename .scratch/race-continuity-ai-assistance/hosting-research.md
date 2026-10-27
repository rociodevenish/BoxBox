# S06 hosting and durable wakeups research

Researched 2026-10-11 against official documentation. Scope: issues 06/07 and the locked architecture, not deployment approval. No cloud resources were changed; no hosted latency or cost was measured. Recommendations below are design inferences, not claims of implemented behaviour.

## Deployment and live transport

Use separate approved development/production Firebase/GCP projects, Angular static Firebase Hosting, and direct HTTPS/WSS Cloud Run API/SignalR URLs. Hosting's Cloud Run rewrites have a **60-second request timeout**; do not place the persistent hub behind them. This establishes the documented timeout constraint, rather than claiming all Hosting WebSocket behaviour has been tested. [Firebase Cloud Run integration](https://firebase.google.com/docs/hosting/cloud-run)

Start with min instances 0/max instances 1, request-based billing, modest explicitly recorded CPU/RAM, and a 3600-second Cloud Run request timeout. WebSockets default to a five-minute timeout and have a 60-minute maximum; reconnect remains mandatory. Do not enable end-to-end HTTP/2. An open WebSocket makes its instance active and billable, even while nobody submits decisions. Session affinity is best effort. [Cloud Run WebSockets](https://docs.cloud.google.com/run/docs/triggering/websockets)

Max instances is cost mitigation, not a lock: brief overshoot is possible, and deployment revisions can overlap. Maintain durable revision checks/transactional transition ownership even for family configuration. Avoid traffic splitting an active race; reconnection can reach a replacement process. [Autoscaling](https://docs.cloud.google.com/run/docs/about-instance-autoscaling), [maximum instances](https://docs.cloud.google.com/run/docs/configuring/max-instances)

Enable SignalR automatic reconnect and a separate bounded retry for initial `start()` failures: the default automatic reconnect policy attempts after 0/2/10/30 seconds and does not retry initial startup. After every reconnect fetch authoritative revision, membership and current phase; do not equate a new connection with resumed authority or rely on missed notifications being replayed. [Microsoft SignalR JavaScript client](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-10.0)

Allow exact Hosting/preview origins in API CORS and explicitly validate hub WebSocket origins: CORS protections do not apply to WebSockets. Exclude token query strings from logs because browser hub transports may send access tokens in URLs. [SignalR security](https://learn.microsoft.com/en-us/aspnet/core/signalr/security?view=aspnetcore-10.0)

## Two authentication schemes

Browser account/guest calls use Firebase ID tokens, validated server-side for signature, expiry, project audience and issuer, then application session/device/team authorization. Revocation handling is a separate decision; basic token verification does not check revocation. Firebase tokens are not Google service-account OIDC tokens. [Firebase token verification](https://firebase.google.com/docs/auth/admin/verify-id-tokens)

Use a dedicated callback principal and explicit OIDC audience for `POST /internal/wakeups`. Cloud Tasks supports scheduled HTTP requests and OIDC service-account tokens; the signing account must belong to the queue's project. Grant the runtime enqueue permission and only required impersonation permission, preserve the Cloud Tasks service-agent role, and audit the current setup instructions when provisioning. [Create authenticated HTTP tasks](https://docs.cloud.google.com/tasks/docs/creating-http-target-tasks)

The browser-facing service needs application Firebase authentication because Cloud Run IAM invocation expects Google identity credentials. If callbacks share that publicly invokable service, `roles/run.invoker` alone cannot protect the callback route: validate Google JWT signature/issuer/audience/expiry **and the exact authorized service-account subject/email** in a distinct application authentication policy. Never accept task headers, a Firebase user token, or merely a valid Google user token as callback authority. Alternatively deploy a separate IAM-private callback service and grant its caller `roles/run.invoker`; both services still coordinate through Firestore. Prefer one service initially to keep operational scope small. [Cloud Run service identity authentication](https://docs.cloud.google.com/run/docs/authenticating/service-to-service), [Google ID-token validation](https://developers.google.com/identity/gsi/web/guides/verify-google-id-token)

## Recoverable scheduling recommendation

Persist deadline/presentation instant, wakeup identity, generation, expected phase/control revision and pending enqueue intent in the same authoritative transaction. After commit enqueue a compact task referring to that identity; record enqueue completion separately. A Firestore commit and Cloud Tasks enqueue are separate operations, so crashes between them require a durable outbox/reconciler. Startup/request reconciliation is useful but **cannot wake an otherwise idle process when no task was ever created**. Add an independent periodic authenticated sweep (for example Cloud Scheduler) or explicitly retain this as a hosted reliability blocker; do not claim recoverable autonomous progression from an in-process timer or startup scan alone.

Callbacks load the saved intent and current server time, compare revisions and commit at most one transition. Old/duplicate/paused generations return success as durable no-ops; storage failures return retryable failure. Do not return success before durable completion. Task deletion is cleanup, not correctness. Pause/resume changes the persisted generation; stale deliveries remain harmless. Cloud Tasks can duplicate executions and ordering is not guaranteed. Future task scheduling is limited to 30 days and task retention to 31 days, so saved races must outlive task records and long pauses must reschedule on resume. [Delivery limitations](https://docs.cloud.google.com/tasks/docs/common-pitfalls), [task limits](https://docs.cloud.google.com/tasks/docs/quotas)

Distinguish intended scheduled execution from restart recovery. Per S06 recovery may close one already-expired decision window, then persists host-resume-required before opening another. Replay must use saved immutable decisions and policy/model versions, not reselect fallback choices. Exercise enqueue-crash gaps, duplicate/late callbacks, a paused old generation, cold starts, and overlapping revisions through public API/SignalR with controlled delivery/time.

## Firestore boundaries

Split session metadata, accepted locks, immutable decision sets, outcomes, presentation and replay history into bounded records; never serialize a whole growing race into one document. Limits include 1 MiB/document, 10 MiB/API request, 270-second transaction duration/60-second idle expiry, and 500 field transformations on one document per commit/transaction. The last value is not a general claim that every transaction may write exactly 500 documents. [Firestore limits](https://firebase.google.com/docs/firestore/quotas)

Transaction functions may rerun on contention and reads precede writes. Keep simulation, random selection, enqueue and broadcast outside retryable transaction bodies: compute immutable versioned inputs, then compare-and-commit; announce only persisted results. Server SDKs bypass Firestore Security Rules and use IAM, so browser rules do not replace server authorization. Deny direct player access to designer replay/private authoritative records. [Transactions](https://firebase.google.com/docs/firestore/manage-data/transactions), [server access and rules](https://firebase.google.com/docs/firestore/security/rules-conditions)

## Costs and controls: estimates only

| Item | Current documented allowance/caveat |
|---|---|
| Cloud Run | Request-based free allowance uses us-central1 pricing: 180,000 vCPU-seconds, 360,000 GiB-seconds and 2 million requests/month. Aggregated by billing account; regional pricing/currency and egress matter. |
| Cloud Tasks | First 1 million operations/account/month free; later $0.40/million USD. API calls and every delivery attempt count, chunked per 32 KB. |
| Firestore | One free database/project; 1 GiB stored, 50,000 reads/20,000 writes/20,000 deletes per day and 10 GiB outbound/month. TTL, PITR, backup/restore have no free usage. |
| Firebase Hosting | 10 GB storage and 360 MB/day transfer free allowance; keep asset sizes and retained releases bounded. |

Sources: [Cloud Run pricing](https://cloud.google.com/run/pricing), [Cloud Tasks pricing](https://cloud.google.com/tasks/pricing), [Firestore quotas](https://firebase.google.com/docs/firestore/quotas), [Firebase pricing](https://firebase.google.com/pricing).

Use billing-enabled approved projects; include build/container registry, logging, egress, chosen Auth providers and any periodic sweeper in estimates. Do not conflate Firebase Hosting with Firebase App Hosting. Record configured billing mode and actual billed socket-hours. A proposed 1-vCPU/0.5-GiB, four-hour active session uses approximately 14,400 vCPU-seconds/7,200 GiB-seconds before startup/overlap: arithmetic only, not a measured bill or guarantee of zero monthly cost. Region and other projects' allowance consumption can change the result.

Configure budget alerts and bounded task dispatch/retry rates, logs and diagnostic retention. **Alerts-only budgets are not a spending cap.** Current docs also describe a separate Spend cap budget option that pauses specified services; inspect availability/supported services and recovery effects in the approved account before selecting it. Max=1 and alerts cannot guarantee a zero bill. [Cloud Billing budgets](https://docs.cloud.google.com/billing/docs/how-to/budgets)

## Release, rollback and evidence

Pin image digest, Git SHA, Hosting release, environment/region, runtime/auth configuration and recovery/schema/model versions. Deploy a candidate revision without live traffic, validate it, then move traffic coherently. Cloud Run supports shifting traffic to previous revisions; Firebase Hosting supports rollback to previous live releases. Rollback restores serving code/assets, **not Firestore state**, so confirm the old revision can read saved active histories; otherwise pause explicitly and restore compatible code. Preserve replay records rather than discard or silently reinterpret them. [Cloud Run rollback](https://docs.cloud.google.com/run/docs/rollouts-rollbacks-traffic-migration), [Hosting releases](https://firebase.google.com/docs/hosting/manage-hosting-resources)

Before hosting, run the repository verification workflow and focused 360px/1920x1080 inspection. Hosted acceptance still requires actual authenticated join, forced hub reconnect, wakeup after scale-to-zero, enqueue-gap repair, process replacement, accepted-lock recovery, duplicate delivery and saved final result. Record decision/resolution/presentation/recovery latencies separately, runtime/socket duration, reads/writes/storage, task attempts, logging/egress and billing observations. Emulator success and free-tier arithmetic cannot check issue 07's hosted smoke or family-playtest boxes. Deployment/project authorization, measured latency and measured costs remain pending.
