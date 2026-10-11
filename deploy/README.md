# Hosted household deployment preparation

No project has been supplied and no cloud deployment has been performed. Review these commands, select a billing-enabled Firebase/Google Cloud project and region, then explicitly authorize deployment. Run from the repository root in PowerShell. Production uses Firebase Hosting for static Angular files and the **direct Cloud Run origin** for REST and SignalR; Hosting rewrites have a 60-second request limit. Runtime configuration contains only the public API origin. Keep ADC/service-account keys, designer keys and tokens out of frontend files and logs.

## Provisioning commands (review before execution)

```powershell
$project = 'REPLACE_PROJECT'
$region = 'REPLACE_REGION'
$service = 'race-api'
$runtime = "race-runtime@$project.iam.gserviceaccount.com"
$task = "race-task@$project.iam.gserviceaccount.com"
$sweep = "race-sweep@$project.iam.gserviceaccount.com"
gcloud config set project $project
gcloud services enable run.googleapis.com cloudbuild.googleapis.com artifactregistry.googleapis.com firestore.googleapis.com cloudtasks.googleapis.com cloudscheduler.googleapis.com identitytoolkit.googleapis.com
gcloud iam service-accounts create race-runtime
gcloud iam service-accounts create race-task
gcloud iam service-accounts create race-sweep
gcloud projects add-iam-policy-binding $project --member="serviceAccount:$runtime" --role=roles/datastore.user
gcloud iam service-accounts add-iam-policy-binding $task --member="serviceAccount:$runtime" --role=roles/iam.serviceAccountUser
gcloud tasks queues create race-wakeups --location=$region --max-dispatches-per-second=2 --max-concurrent-dispatches=1 --max-attempts=10 --max-retry-duration=86400s --min-backoff=5s --max-backoff=300s --max-doublings=5
gcloud tasks queues add-iam-policy-binding race-wakeups --location=$region --member="serviceAccount:$runtime" --role=roles/cloudtasks.enqueuer
gcloud iam service-accounts describe $task --format='value(uniqueId)'
gcloud iam service-accounts describe $sweep --format='value(uniqueId)'
```

Create the default Firestore Native database in the chosen region if absent (choose location before creating it; do not blindly create another database). Add Firebase to the same project and create a web app. In Firebase Authentication enable anonymous and email/password providers and add actual Hosting/custom frontend domains to Authorized domains. Anonymous and password flows do not require an OAuth redirect callback. If adding redirect providers later, configure their documented `https://<authDomain>/__/auth/handler` callback and provider allowlists separately. Deploy deny-all browser Firestore rules (`tools/auth-emulator/firestore.rules`); server ADC IAM grants bypass rules. Do not deploy emulator configuration.

Cloud Tasks and Scheduler service agents must retain their default service-agent roles. Deployment operator needs permission to act as the three selected service accounts; do not grant token minting to browser users. The public Cloud Run API uses app-level Firebase user authentication/membership; task and sweep endpoints additionally verify signed Google OIDC **exact** email, numeric uniqueId/JWT sub, issuer, audience and expiry. Use distinct task and sweep accounts, both with explicit audience equal to the API origin, without a path or query.

```powershell
gcloud artifacts repositories create race --repository-format=docker --location=$region
$image = "$region-docker.pkg.dev/$project/race/api:REPLACE_COMMIT_SHA"
gcloud builds submit --tag=$image .
# Obtain the deterministic service origin without deploying incomplete production config:
$projectNumber = gcloud projects describe $project --format='value(projectNumber)'
$apiOrigin = "https://$service-$projectNumber.$region.run.app"
# Copy deploy/cloud-run.env.example.yaml to a private local file; replace all placeholders,
# including both uniqueIds above, Firebase public API key, exact frontend origins and $apiOrigin.
gcloud run deploy $service --image=$image --region=$region --service-account=$runtime --allow-unauthenticated --min=0 --max=1 --concurrency=80 --cpu=1 --memory=512Mi --timeout=3600 --no-use-http2 --env-vars-file=REPLACE_PRIVATE_ENV_FILE
# Confirm the actual origin matches the configured audience/callback before accepting traffic.
gcloud run services describe $service --region=$region --format='value(status.url)'
gcloud scheduler jobs create http race-repair --location=$region --schedule='* * * * *' --time-zone=UTC --uri="$apiOrigin/internal/wakeups/sweep" --http-method=POST --oidc-service-account-email=$sweep --oidc-token-audience=$apiOrigin --attempt-deadline=30s --max-retry-attempts=3 --min-backoff=5s --max-backoff=60s
Push-Location src/frontend
node ../../tools/run-bounded.mjs --timeout-ms 180000 --report ../../.tools/reports/deploy-frontend.json -- node node_modules/@angular/cli/bin/ng.js build
Pop-Location
node deploy/write-runtime-config.mjs $apiOrigin
firebase deploy --only hosting --config firebase.hosting.json --project $project
firebase deploy --only firestore:rules --config firebase.firestore.json --project $project
```

The bootstrap origin above uses Google's [documented deterministic URL format](https://docs.cloud.google.com/run/docs/triggering/https-request). Approve the exact service name, project number and region before constructing production config; for an existing service use its verified origin instead. The `status.url` may show the older stable hashed alias: verify the configured deterministic URL itself responds successfully and use that one consistently for frontend, callbacks and OIDC audience. Do not substitute a placeholder callback or weaken production validation to bootstrap. Check every command's exit status before proceeding. Firebase CLI must be installed/pinned separately or invoked through the repository's installed CLI path. Build and record the immutable image digest; tags alone are mutable. No cloud credentials are bundled in the image. Queue-scoped enqueuer permissions use the [documented queue IAM command](https://docs.cloud.google.com/sdk/gcloud/reference/tasks/queues/add-iam-policy-binding).

## Recovery and rollback

Record image digest, Git SHA, Cloud Run revision, Hosting release, recovery/state/auto-planner versions and env configuration fingerprint before rollout. Max=1 is a cost bound, not a transaction lock: deploy/revision overlap can happen; Firestore manifest compare-and-swap remains required. Retain the immediately previous image/revision and Hosting release only after confirming that it understands `progression-v1`, current session envelope and frozen planner versions. Never roll back to code unable to read current persisted sessions. Prefer forward repair, pause affected sessions and leave state intact if incompatible.

```powershell
gcloud run revisions list --service=$service --region=$region
gcloud run services update-traffic $service --region=$region --to-revisions=REPLACE_COMPATIBLE_PREVIOUS_REVISION=100
# Firebase console > Hosting > release history > Roll back a verified compatible release.
gcloud scheduler jobs run race-repair --location=$region
```

Neither traffic rollback nor Hosting rollback restores Firestore. Snapshot/backup recovery is a separate deliberate operation with additional cost and possible loss of later accepted commands. Keep the Scheduler repair job enabled: a persisted enqueue intent survives queue failure and process death; Scheduler repairs the commit/enqueue gap independently at min=0. Duplicate/stale tasks remain no-ops. Ordinary cold recovery closes at most one overdue decision then pauses; host reviews and resumes. A trusted current presentation task can open the next window; a cold decision callback closes once and pauses. Confirm these distinctions in hosted tests.

## Cost and observability limits

Target near-zero for a small household, never promise zero. Billing is required. Each minute of repair causes a Cloud Run request and bounded scheduled-manifest query even when no players connect; measure this baseline. Open SignalR connections keep an instance active/billable and reconnect after the 60-minute Cloud Run request timeout. UI refresh and reconnect recover from authoritative snapshots; no display acknowledgments gate progression. Tasks delivery/retries, Firestore writes/chunks, queries, image builds/storage, egress and logs can bill independently. Free tiers aggregate according to each product's project/account rules; consult [the cited research](../.scratch/race-continuity-ai-assistance/hosting-research.md). Use explicit budget alerts (alerts alone do not stop usage), evaluate eligible spend-cap options separately, and inspect regional/currency pricing before rollout.

Keep hosting diagnostics at Warning to avoid access-token query logging. Never log request bodies, Authorization, private team plans, Firebase tokens or OIDC tokens. Existing durable designer record contains concrete applied auto choices/planner versions; it remains loopback plus independent key protected and must not become a player endpoint. Designer retrieval in Cloud Run requires an explicitly designed trusted diagnostic path; do not relax loopback restrictions. Current transition evidence is persisted; use generation/session IDs and bounded error-type diagnostics for queue repair. Do not add unbounded polling or body logging. The scheduled-manifest scan intentionally supports at most 1000 scheduled sessions; excess fails visibly rather than silently omitting work.

Cloud Logging default bucket retention is typically 30 days; explicitly select retention and exclusions/sampling in the project. Preserve needed security/error events; cap routine logs. Firestore currently has no automatic history cleanup: session records and chunks accumulate, so inventory finished sessions, export required replay evidence and use a reviewed deletion procedure with a stated retention policy. TTL/backups/PITR are paid options and need compatible chunk/manifest cleanup, not a blind TTL on manifests. Do not claim retention is enforced by these local files.

## Hosted evidence and family playtest form

Copy this form into `.scratch/race-continuity-ai-assistance/evidence/hosted-family.md` when authorized. Every blank is **not measured/pending** until observed.

- Project / region / billing account currency / observer / UTC dates:
- Git SHA / immutable image digest / Cloud Run revision / Hosting release / planner + recovery versions:
- Exact API and frontend origins / Firebase providers and authorized domains / redacted env fingerprint:
- Join and private membership check; unlisted-origin REST and WebSocket rejection; wrong task/sweep identity + audience rejection:
- SignalR timeout reconnect and refresh; idle min=0 wake; commit-before-enqueue failure repaired independently; duplicate/stale task ignored:
- Restart after accepted lock; restart final classification persistence; ordinary recovery closes once/pauses; trusted task cold delivery:
- Family participants/devices (consented, no tokens): missing player; shared tablet; takeover/reclaim; mixed Open/Guided; interrupted session; usability findings:
- Measured decision close / resolution / presentation / recovery latency: sample counts, p50/p95, cold/warm, UTC range:
- Measured billable usage and cost per product/currency/window: Tasks attempts, Scheduler requests, Firestore reads/writes/storage, Run CPU/memory/request/egress, build/images/logs. Separate estimates from invoiced costs:
- Public 360px/1920px screenshots and redacted replay evidence paths; compatibility rollback exercise:
- Remaining failures, remediation and retest observations:

Local tests and screenshots cannot check these hosted/family/cost boxes. Ticket07 remains ready-for-human / needs project and deployment authorization.
