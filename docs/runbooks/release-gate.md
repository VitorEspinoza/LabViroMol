# Runbook — Post-deploy release gate (New Relic)

**English** · [Português](./release-gate.pt-BR.md)

## What it is

The release gate is an automated check that runs **after** the API deploy
and the `/health/ready` validation. It queries real error and latency
metrics via NerdGraph (New Relic) and fails the CD job if the thresholds
are violated.

The health check says "the process is up"; the release gate says "it's
healthy under real traffic".

## Configured thresholds

| Metric | Default threshold | Env variable |
|---------|-----------------|-----------------|
| Error rate (`errPct`) | 2% | `ERR_PCT_THRESHOLD` |
| P95 latency (`p95Ms`) | 2000 ms | `P95_THRESHOLD_MS` |
| Lookback window | 5 min | `WINDOW_MINUTES` |

The 5-minute window balances two concerns: capturing real errors
introduced by the deploy (which show up right in the first requests after
the cold start) and avoiding excessive pipeline timeout. In production
with steady traffic, 5 minutes is enough for a statistically relevant
sample.

## Required credentials

- `NR_USER_API_KEY` — New Relic's **User key** (NerdGraph). Created at
  [one.newrelic.com → API keys → Create a key → type User](https://one.newrelic.com/api-keys).
  **Not the License key** (`NR_LICENSE_KEY`) used for telemetry ingest —
  the two have completely different scopes; mixing them up is a common
  mistake.
- `NR_ACCOUNT_ID` — the numeric New Relic account ID (visible at
  one.newrelic.com → Account settings).

Both must be configured as GitHub secrets (Settings → Secrets and
variables → Actions) and in the `production` environment.

## Deployment marker

Besides the gate, every successful deploy creates a **deployment marker**
in New Relic via the NerdGraph mutation `changeTrackingCreateDeployment`.
This draws a vertical line on the APM dashboards, allowing visual
correlation of any metric variation with the exact moment of the deploy.

The marker carries:
- `version`: commit SHA (`github.sha`)
- `user`: the Actions actor that triggered the workflow
- `deepLink`: URL of the Actions run
- `entityGuid`: GUID of the API's APM entity in New Relic

The APM entity's `entityGuid` is required for the marker. It's obtained
at: one.newrelic.com → APM → `labviromol-api` → Settings → Application →
Entity GUID. Configure it as the secret `NEW_RELIC_ENTITY_GUID`.

## What to do when the gate fires

### 1. Assess whether it's a real signal or noise

Before acting, check:

- **Real traffic?** If the deploy happened outside of peak hours and the
  5-min window had zero or few requests, `errPct` can be 100% from a
  single error in a single request. Check the total volume at:
  one.newrelic.com → APM → `labviromol-api` → Transactions.

- **Real regression or infra problem?** Check whether the error is an
  HTTP 5xx from the API (code regression) or a dependency failure
  (database, LibreTranslate, SMTP). The trace ID in the Actions logs and
  in New Relic lets you pivot.

- **Threshold appropriate?** If production traffic has grown and 2%
  errors represents an acceptable volume of transient failures (e.g.,
  LibreTranslate timeouts that get retried), adjust
  `ERR_PCT_THRESHOLD` per the tuning section below.

### 2. Fix-forward (main path)

The project's default policy is fix-forward. If the gate fired due to a
real regression:

1. Identify the error in New Relic's logs/traces (use the deployment
   marker to filter the post-deploy window).
2. Commit a fix on the branch and open a PR.
3. The next deploy will run the gate again.

While the gate is firing, the problematic version's API **keeps
serving** (the job failed but the API was brought up — the gate is a
post-fact check, not an automatic rollback).

### 3. Manual image rollback (only without a new migration)

If the deploy didn't bring a schema migration and the regression is
critical:

```bash
# On the droplet
cd ~/labviromol-deploy
API_TAG=<previous-sha> docker compose -f docker-compose.prod.yaml \
  --env-file .env up -d api
```

See `docs/runbooks/deploy.md` for the full procedure. If this deploy
included a new migration, an image rollback **would break
compatibility** — use fix-forward in that case.

### 4. Temporarily silence the gate (last resort)

If it's necessary to silence the gate for an emergency deploy:

- In the `cd.yml` workflow, the `New Relic deployment marker` step and the
  `Release gate (NRQL)` step both have `if: steps.health.outcome ==
  'success'`. To skip only the gate, temporarily add `if: false` to the
  `Release gate (NRQL)` step via a direct commit.
- Document the reason and revert it in the following commit.

## Tuning thresholds

Thresholds are configured via environment variables in the
`Release gate (NRQL)` step of `.github/workflows/cd.yml`:

```yaml
env:
  NR_USER_API_KEY: ${{ secrets.NR_USER_API_KEY }}
  NR_ACCOUNT_ID: ${{ secrets.NR_ACCOUNT_ID }}
  NR_APP_NAME: labviromol-api
  WINDOW_MINUTES: "5"
  ERR_PCT_THRESHOLD: "2"
  P95_THRESHOLD_MS: "2000"
```

Edit the values directly in the workflow. The variables are documented in
the header of `scripts/deploy/nr-release-gate.sh`.

## Instrumentation prerequisites

The gate is only useful if the API is actually exporting traces to New
Relic. Confirm:

- `NR_LICENSE_KEY` configured in the production secrets and injected via
  `secrets/prod.enc.env` (plan 23).
- `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_HEADERS`
  configured in `appsettings.json` or via env vars (`Shared.Infrastructure`
  OTel observability plan).
- The service showing up as `labviromol-api` at one.newrelic.com → APM.

If the API isn't exporting, the gate returns "no data" and passes (exit
0) with a warning — it doesn't fail the deploy for lack of data. This is
intentional: a deploy to an environment without real traffic (e.g.,
staging without active instrumentation) shouldn't be blocked by the gate.

## Alert channel

When the gate fails, the GitHub Actions job emits an error annotation
(`::error`) visible in the PR/push check. For external alerts (Slack,
PagerDuty, e-mail), configure a notification workflow on the
`workflow_run → cd → failure` event, or use the New Relic alert condition
"Deployment marker followed by error rate spike" in New Relic Alerts.
