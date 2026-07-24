# CI/CD — End-to-end guide

**English** · [Português](./ci-cd.pt-BR.md)

This document explains the entire LabViroMol CI/CD pipeline: which workflows exist, when each one
fires, what each step validates or produces, and how the commands inside the workflows communicate
success/failure to each other.

> Source files: [`.github/workflows/`](../.github/workflows/), [`Dockerfile`](../Dockerfile),
> [`Dockerfile.migrate`](../Dockerfile.migrate), [`docker-compose.ci.yaml`](../docker-compose.ci.yaml),
> [`docker-compose.prod.yaml`](../docker-compose.prod.yaml), [`scripts/ci/`](../scripts/ci/),
> [`scripts/deploy/`](../scripts/deploy/), [`scripts/dora/`](../scripts/dora/).

---

## 1. Overview — the 12 workflows

| Workflow | File | Trigger | Role | Blocks merge/deploy? |
|---|---|---|---|---|
| CI - Build + Tests | `ci-build-test.yml` | PR → main; push on `feat/**`, `fix/**`, `refactor/**` | Builds, checks formatting, runs unit/architecture/integration tests | **Yes** (via `ci-required` sentinel) |
| CI - Migration Guard | `migration-guard.yml` | PR → main touching `Migrations/*.cs` | Detects destructive migrations (DROP etc.) | **Yes**, unless `migration-reviewed` label |
| API Contract | `api-contract.yml` | PR → main; push on main | Generates the OpenAPI spec at build time and diffs it against main's (breaking changes) | **Yes**, unless `api-breaking-approved` label |
| CodeQL | `codeql.yml` | PR; push on main; weekly cron | SAST (static security analysis of the C# code) | Yes (PR check) |
| SCA | `sca.yml` | PR; weekly cron | Vulnerabilities in dependencies (NuGet + Trivy fs) | **Yes** for High/Critical |
| Secrets scanning | `secrets.yml` | PR; push on main; weekly cron | Gitleaks — committed secrets | Yes (PR check) |
| Container Scan | `container-scan.yml` | PR; and **called by CD** (`workflow_call`) | Builds the API image and scans it with Trivy | **Yes** on PR and as a CD gate |
| SBOM | `sbom.yml` | PR; push on main | Generates the image SBOM (CycloneDX/SPDX) and scans it with Grype | Non-gating (informational + Security tab) |
| Dynamic (DAST + Perf Smoke) | `dynamic.yml` | PR → main | Real ephemeral stack → seed → NBomber smoke → ZAP API scan (OpenAPI-driven, authenticated) | **Yes** (NBomber thresholds + ZAP FAIL rules) |
| CD | `cd.yml` | Push on main (= PR merge) | Scan-gate → build/push signed images → migrate-first deploy on the droplet → post-deploy gates | — (it IS the deploy) |
| Perf - Load Tests | `perf-load.yml` | Manual (`workflow_dispatch`) or weekly cron | Heavy load (load/stress/soak/spike/breakpoint) outside the PR path | No |
| DORA Metrics | `dora.yml` | Weekly cron or manual | Computes DORA metrics from GitHub history | No |

All of them use `paths-ignore` to **not** fire when the commit only changes `**/*.md`, `docs/**` or
`infra/**` — a documentation-only change doesn't burn CI minutes or redeploy anything.

Two blocks are common to almost all of them:

- **`concurrency`** — groups runs by ref (`ci-${{ github.ref }}` etc.) with `cancel-in-progress:
  true`: if you push a new commit to the same PR, the old run is cancelled instead of running in
  parallel. Deliberate exceptions: CD, perf-load and DORA use `cancel-in-progress: false` — a deploy
  or a load test in progress should never be cancelled midway.
- **`permissions: contents: read`** — the job's `GITHUB_TOKEN` starts with the bare minimum; each job
  elevates only what it needs, point by point (`security-events: write` to publish SARIF,
  `packages: write` to push to GHCR, etc.). That's why ZAP runs with `allow_issue_writing: false`:
  the workflow deliberately doesn't have `issues: write`.

---

## 2. The life of a Pull Request

When you open a PR against `main`, the following fire **in parallel**:

```
PR opened/updated
 ├─ CI - Build + Tests        (build, format, unit+arch, integration)
 ├─ Migration Guard           (only if it touched Migrations/*.cs)
 ├─ API Contract              (generates spec and diffs against main)
 ├─ CodeQL                    (SAST)
 ├─ SCA                       (vulnerable dependencies)
 ├─ Secrets scanning          (gitleaks over the PR range)
 ├─ Container Scan            (API image + Trivy)
 ├─ SBOM                      (component inventory + Grype)
 └─ Dynamic (DAST + Perf)     (ephemeral stack + NBomber + ZAP)
```

The merge is only allowed once the required checks (branch protection) are green. Details for each:

### 2.1 CI - Build + Tests (`ci-build-test.yml`)

Three parallel jobs plus a sentinel:

- **build-lint** — `dotnet restore` → `dotnet build -c Release --no-restore` →
  `dotnet format --verify-no-changes --severity error`. The `--verify-no-changes` flag makes the
  formatter run in "check-only" mode: if any file would need reformatting, the command returns a
  non-zero exit code and the job fails (formatting becomes a gate, not a suggestion).
- **unit-arch-tests** — runs `dotnet test` in an explicit loop over each module's domain unit test
  projects plus the architecture tests (which verify rules such as "Domain does not reference
  Infrastructure"). `--collect:"XPlat Code Coverage"` generates coverage, published as the
  `coverage-unit-arch` artifact.
- **integration-tests** — discovers the projects under `tests/IntegrationTests` via `find` and runs
  each one. They use **Testcontainers**: each test spins up a real Postgres in Docker, which is why
  there's no database service declared in the workflow — the test itself orchestrates the container.
- **ci-required (sentinel)** — a job with `needs: [build-lint, unit-arch-tests, integration-tests]`
  and `if: always()` that fails if any upstream job failed. It exists so branch protection can point
  to **one single stable check** ("CI required") instead of three, which simplifies configuration and
  keeps working even if jobs get renamed/added.

### 2.2 Migration Guard (`migration-guard.yml`)

Only fires if the PR touches `src/Modules/*/Infrastructure/Persistence/Migrations/*.cs`. Runs
`scripts/ci/check-destructive-migrations.sh`, comparing the PR base against the head — the script
looks for destructive operations (DropTable, DropColumn, etc.).

Notice the **gate with an escape valve** pattern, used here and in API Contract:

```
set +e                       # turns off "fail on first error"
bash script.sh ...           # run the check
echo "exit_code=$?" >> "$GITHUB_OUTPUT"   # store the result as a step output
exit 0                       # the step itself never fails
```

The next step reads `exit_code` and checks whether the PR has the `migration-reviewed` label (via
`jq` over `github.event.pull_request.labels`). The final decision ("Evaluate gate") combines both:
destructive **without** the label → `exit 1` (blocks); **with** the label → passes, backed by the
CODEOWNERS approval requirement on the Migrations path.

### 2.3 API Contract (`api-contract.yml`)

Protects the API's consumers (Admin Panel and the institutional site) against accidental breaking
changes:

1. **generate-spec** — the API build emits the OpenAPI spec at build time (the
   `OpenApiGenerateDocumentsOnBuild`/`OpenApiDocumentsDirectory` csproj properties point to
   `contracts/`). The generated JSON becomes `revision.openapi.json`; the "official" version is read
   from main with `git show origin/main:contracts/openapi.json`.
2. **breaking-change-gate** (PR only) — runs the `tufin/oasdiff` container comparing base vs.
   revision with `--fail-on ERR`. A breaking change without the `api-breaking-approved` label →
   blocks; with the label → passes (a conscious change, frontends notified).
3. **publish-spec** (push to main only) — commits the updated `contracts/openapi.json` with
   `[skip ci]` in the message so it doesn't trigger the workflows again.

### 2.4 Static security: CodeQL, SCA, Secrets, Container Scan, SBOM

- **CodeQL** (SAST) — compiles the solution under CodeQL instrumentation (`build-mode: manual`,
  hence the explicit `dotnet build` between `init` and `analyze`) and publishes findings in the
  repository's **Security → Code scanning** tab.
- **SCA** — two fronts: `dotnet list package --vulnerable --include-transitive` (NuGet advisories)
  with a grep that fails the job if High/Critical shows up; and Trivy in filesystem mode, run
  **twice** — once in SARIF with `exit-code: 0` (report only, goes to the Security tab) and once in
  table format with `exit-code: 1` restricted to HIGH/CRITICAL (this one is the gate). This
  "double scan: one for visibility, one for gating" pattern repeats in Container Scan.
- **Secrets** — Gitleaks. On PR, it scans only the PR's commit range (`--no-merges base..head`); on
  push/cron, the full history. `fetch-depth: 0` on checkout is needed because the scan looks at git
  history, not just the working tree.
- **Container Scan** — builds the API image (`push: false, load: true` = the image only lives in the
  runner's local daemon) and runs Trivy against it (vuln + misconfig + secret). `ignore-unfixed: true`
  avoids blocking on a CVE with no fix available. This workflow has `workflow_call` in its trigger —
  that's how **CD reuses it as a gate** before publishing.
- **SBOM** — generates the image's component inventory in two formats (CycloneDX and SPDX) with Syft
  and scans it with Grype. Doesn't gate; it's for supply-chain traceability.

### 2.5 Dynamic — DAST + Perf Smoke (`dynamic.yml`)

The only PR workflow that tests the application **actually running**. Sequence:

1. **Bring up the ephemeral stack** — `docker compose -f docker-compose.ci.yaml up -d --wait
   --wait-timeout 180`. The CI compose defines three services chained by *conditional* dependencies:
   - `postgres` (with a `pg_isready` healthcheck; publishes `127.0.0.1:5432` for the runner steps);
   - `migrate` (`depends_on: postgres: service_healthy`) — a one-shot container built from
     `Dockerfile.migrate`, which applies the migrations and **exits**;
   - `api` (`depends_on: migrate: service_completed_successfully`) — only starts if the migration
     exited 0; has an HTTP healthcheck on `/health/ready`.

   `--wait` blocks the command until everything is healthy/completed — or fails. On failure, the
   workflow dumps `docker compose logs migrate` and `logs api` into the Actions log (without this,
   the container's exit code would be invisible).

2. **Smoke check** — `curl -fsS $BASE_URL/health/ready`: the `-f` flag turns HTTP ≥ 400 into a
   non-zero exit code.

3. **Seed** — `dotnet run ... -- --command=seed --profile=ci-smoke --baseUrl=...`. The seeder talks
   **directly to Postgres** (which is why `ConnectionStrings__LabViroMol` points at
   `localhost:5432`, the published port — the step runs on the runner, outside the compose network).
   Watch the syntax: the LoadTests parser only accepts `--key=value` (with `=`).

4. **NBomber smoke** — `--profile=ci-smoke --campaign=ci --scenario=full`: light load against the
   API over HTTP. The profile's thresholds act as a gate via the process's exit code. The report is
   uploaded as an artifact (`nbomber-ci-smoke-report`) even on failure (`if: always()`).

5. **DAST login** — ZAP doesn't know how to authenticate on its own; a step `curl`s
   `/api/identity/users/login` with a user created by the seed (`loadtest-user1@test.local`),
   extracts the `X-Access-Token` cookie from `Set-Cookie`, and publishes it as a masked output
   (`::add-mask::` prevents the token from leaking into the log).

6. **ZAP API scan** — DAST **driven by the OpenAPI contract**, authenticated. A build of the API on
   the runner emits `contracts/LabViroMol.Api.json` (the same mechanism as the API Contract
   workflow); the `zaproxy/action-api-scan` action imports the spec and hits **every** endpoint with
   synthetic data generated from the schemas plus payloads from active rules (SQLi, injection etc.).
   Relevant configuration:
   - `ZAP_AUTH_HEADER: Cookie` / `ZAP_AUTH_HEADER_VALUE` — injects the auth cookie from the previous
     step into every request the scanner makes (the API authenticates via httpOnly cookie, not the
     `Authorization` header).
   - `cmd_options: "-O http://localhost:8080 -a -I"` — `-O` points the spec's server at the ephemeral
     stack; `-a` includes alpha rules; `-I` makes WARNs **not** fail the job (only rules marked FAIL
     in the tsv gate it).
   - `rules_file_name: .zap/rules.tsv` — each rule can be promoted to `FAIL` or downgraded to
     `WARN`/`IGNORE` with documented justification (e.g., CSP rules don't apply to a pure JSON API;
     cookie rules are FAIL because the API issues authentication cookies).
   - `allow_issue_writing: false` — doesn't try to open an issue (the PR workflow doesn't have
     `issues: write`; the report already goes out as an artifact).

   Design notes: the scan runs **after** NBomber on purpose — active rules write/delete real data in
   the database (safe because the stack is disposable). Many `400` responses are expected
   (validation rejecting synthetic data). Known limitation: the scan uses a single privileged user,
   so it doesn't test cross-role authorization (broken access control is out of scope).

7. **Teardown** — `docker compose down -v` with `if: always()`: the stack is torn down even if any
   previous step failed (`-v` removes the volumes, guaranteeing a clean database per run).

### 2.6 Two connection variables — why

`dynamic.yml` deliberately keeps two connection strings:

- `DB_CONNECTION_STRING` (`Host=postgres`) — used **inside** the compose network, by the `migrate`
  and `api` containers, where `postgres` is the service hostname.
- `DB_CONNECTION_STRING_RUNNER` (`Host=localhost`) — used by the steps that run **on the runner**
  (seed and NBomber), which only see the database through the published port `127.0.0.1:5432`.

---

## 3. What happens at merge time — CD (`cd.yml`)

A push to `main` (in practice, a PR merge) triggers CD. Three jobs in sequence:

### 3.1 `container-scan-gate`

`uses: ./.github/workflows/container-scan.yml` — reuses the scan workflow as a **blocking job**: if
the image has HIGH/CRITICAL findings with a fix available, nothing gets published.

### 3.2 `build-push` — publishing images with a chain of custody

For **each** image (API via `Dockerfile`, migrate via `Dockerfile.migrate`):

1. **Build + push** to GHCR with two tags: `latest` and `${{ github.sha }}` (the immutable
   per-commit tag is what the deploy uses). `cache-from/to: type=gha` reuses Docker layers across
   runs via the Actions cache.
2. **SBOM** of the published image (Syft, CycloneDX).
3. **SBOM attestation** (`actions/attest-sbom`) — cryptographically links the SBOM to the image
   digest and records it in the registry.
4. **SLSA provenance attestation** (`actions/attest-build-provenance`) — records "this image was
   built by this workflow, from this commit, in this repository".
5. **Cosign keyless signature** — `cosign sign` using the workflow's own OIDC identity (hence
   `id-token: write` in permissions; no private key is stored).

Everything references the **digest** (`@sha256:...`), not the tag — a tag can move, a digest can't.
The digests come out as job outputs and go into the job summary.

### 3.3 `deploy` — migrate-first, minimal-downtime, fix-forward

Runs against `environment: production` (allows requiring manual approval and scoping secrets). All
remote work is done via SSH (`appleboy/ssh-action`) and SCP on the droplet:

1. **Config sync** — creates remote directories and uploads `docker-compose.prod.yaml` and
   `nginx/gateway.conf`.
2. **Secrets decrypt + migration (the central gate)** — on the droplet:
   - `sops --decrypt secrets/prod.enc.env > .env` — production secrets live in the repo
     **encrypted** (SOPS/age); the age key only lives on the droplet (see
     [runbooks/secrets.md](runbooks/secrets.md)).
   - `docker login` to GHCR and `docker compose pull migrate` at the commit's tag.
   - `docker compose run --rm migrate` — runs the migration bundles **before** touching the API.
     The exit code is captured (`set +e` / `MIGRATE_EXIT=$?`) and re-emitted: the step only passes
     if the migration passed.
3. **API deploy** (`if: steps.migrate.outcome == 'success'`) — `pull api` + `up -d api`: the compose
   recreates only the API container (~2–5 s window). Postgres, libretranslate, the nginx gateway,
   certbot and the frontends **are not touched**.
4. **Post-deploy validation** — polling `http://localhost:8080/health/ready` (up to 18 attempts ×
   10 s). No 200 → the job fails. The policy is **fix-forward**: there's no automatic rollback; the
   error message points to the procedure ([runbooks/deploy.md](runbooks/deploy.md)).
5. **New Relic deployment marker** — the `changeTrackingCreateDeployment` GraphQL mutation marks the
   deploy on the observability timeline (best-effort: a missing secret becomes a warning, not a
   failure).
6. **Release gate (NRQL)** — waits 3 minutes to accumulate real traffic and runs
   `scripts/deploy/nr-release-gate.sh`, which queries New Relic: error rate ≤ 2% and p95 ≤ 2000 ms
   over the 5-minute window. If exceeded → the job fails, signaling that the deploy degraded
   production (response procedure in [runbooks/release-gate.md](runbooks/release-gate.md)).
7. **Registration** — creates a **GitHub Deployment** with a success/failure status (this is the
   API the DORA workflow later consumes) and writes a summary into the job summary.

The key design point: **if the migration fails, the old API stays up** — `run --rm migrate` happens
before any `up` of the API, and every following step has `if: steps.migrate.outcome == 'success'`.

### 3.4 How migration works (Dockerfile.migrate + scripts)

- **Image build** ([`Dockerfile.migrate`](../Dockerfile.migrate)): on a full .NET SDK, installs
  `dotnet-ef` and runs [`scripts/ci/build-migration-bundles.sh`](../scripts/ci/build-migration-bundles.sh),
  which generates **one EF Core bundle per module** (`efbundle-identity`, `efbundle-inventory`, ...,
  one per DbContext) with `dotnet ef migrations bundle`. The final image is
  `mcr.microsoft.com/dotnet/aspnet:10.0` — it needs to be `aspnet` (not `runtime`) because the
  bundles embed the API's startup project, which depends on the ASP.NET Core shared framework.
- **Execution** ([`scripts/ci/run-migration-bundles.sh`](../scripts/ci/run-migration-bundles.sh),
  the ENTRYPOINT): applies the bundles **in a fixed order** (identity → inventory → assets →
  research → scheduling → notify), passing `--connection "$DB_CONNECTION_STRING"`; any non-zero exit
  aborts the sequence and propagates the code.
- **Known gotchas** (all already handled, documented here to avoid regressions):
  - `ef migrations bundle` **executes the API's `Program.cs` at design time** (and so does the
    bundle, when it runs). So everything `Program.cs` requires before `builder.Build()` needs to be
    present via env: `ConnectionStrings__LabViroMol` and `Storage__RootFolder`.
  - `Storage__RootFolder` needs to be a **POSIX** path on Linux: a Windows-style value (`C:\...`) is
    not "rooted" on Linux, becomes a literal directory with `\` in its name inside the project, and
    breaks MSBuild glob expansion (`MSB3552`, dotnet/sdk#10172).

---

## 4. Scheduled / manual workflows

### Perf - Load Tests (`perf-load.yml`)

Same ephemeral stack as `dynamic.yml`, but with heavy profiles (`load`, `stress`, `soak`, `spike`,
`breakpoint`) chosen via `workflow_dispatch` (or `load` on the Sunday cron). `timeout-minutes: 120`
because soak runs are long. Doesn't gate anything — the goal is the report (NBomber HTML artifact +
`summary.json` in the job summary). Operational details in
[tests/LoadTests/RUNBOOK.md](../tests/LoadTests/RUNBOOK.md).

### DORA Metrics (`dora.yml`)

Weekly cron (or manual, with a configurable window). Runs
[`scripts/dora/compute-dora.sh`](../scripts/dora/compute-dora.sh), which uses the GitHub API
(deployments created by CD, PRs, etc.) to compute the four DORA metrics — Deployment Frequency, Lead
Time, Change Failure Rate, MTTR — and publishes them in the job summary. See
[docs/dora.md](dora.md).

### Security crons

CodeQL (Mon 05:00), SCA (Mon 06:00) and Secrets (Mon 07:00) also run weekly **outside** of PRs: a new
CVE may be published for a dependency that's already on main — the cron catches those cases without
depending on someone opening a PR.

---

## 5. Concepts and patterns used (quick glossary)

| Concept | Where it appears | What it means |
|---|---|---|
| **Gate via exit code** | all | In Actions, a step fails when the command returns a non-zero exit code; `set -e` aborts the script on the first error, `set +e` suspends that so the code can be captured manually (`$?`) and decided on later. |
| **`$GITHUB_OUTPUT`** | migration-guard, api-contract, cd | Magic file: `echo "key=value" >> "$GITHUB_OUTPUT"` publishes a step output, read by other steps as `steps.<id>.outputs.key`. It's the mechanism for communication between steps. |
| **`$GITHUB_STEP_SUMMARY`** | cd, dora, perf-load | Markdown appended here shows up on the run's page — a readable summary without opening logs. |
| **`if: always()`** | teardown, uploads | Step runs even if something before it failed — essential for cleanup and for publishing reports of failed runs (which are exactly the ones you want to investigate). |
| **`if: steps.X.outcome == 'success'`** | cd | Conditional chaining: API deploy only happens after the migration is ok. |
| **Sentinel job** | ci-build-test | An aggregator job so branch protection can require a single check. |
| **Label as an escape valve** | migration-guard, api-contract | The automatic gate blocks by default; a human consciously unblocks it by adding a label to the PR (`migration-reviewed`, `api-breaking-approved`). |
| **Double scan (report + gate)** | sca, container-scan | First pass with `exit-code: 0` generates a full SARIF for the Security tab; second pass with `exit-code: 1` and restricted severity is the gate. Full visibility, selective blocking. |
| **SARIF / Security tab** | codeql, sca, secrets, container-scan, sbom | Standard analysis result format; `upload-sarif` centralizes everything under Security → Code scanning. |
| **`workflow_call`** | container-scan ← cd | Lets a workflow be called as a job of another one (reusing the scan as a CD gate). |
| **Ephemeral stack** | dynamic, perf-load | A full environment (database + migration + API) created from scratch for the test and torn down at the end — deterministic and with no inherited state. |
| **Conditional `depends_on`** | docker-compose.ci.yaml | `service_healthy` (waits for the healthcheck) and `service_completed_successfully` (waits for a one-shot to exit 0) — that's what orders postgres → migrate → api. |
| **Migrate-first** | cd | The migration runs and is validated before the new API container starts; if it fails, the old version keeps serving. |
| **Fix-forward** | cd | No automatic rollback: a post-deploy failure is fixed with a new commit/deploy. Manual image rollback is only safe if the deploy didn't bring a new migration. |
| **SBOM / attestation / cosign** | sbom, cd | Component inventory, cryptographic proof of origin (SLSA), and keyless signing — the chain of custody of the published image. |
| **SOPS/age** | cd | Production secrets versioned in the repo in encrypted form; only the droplet has the key to decrypt them. |
| **DAST × SAST × SCA** | dynamic × codeql × sca | DAST tests the running application (black box); SAST analyzes the source code; SCA analyzes the dependencies. Complementary layers. |

---

## 6. Mental map — from commit to production

```
branch feat/xyz ──push──► CI Build+Tests (fast feedback on the branch)
       │
       ▼ opens PR → main
┌─────────────────────────────────────────────────────────────┐
│  PR gates (parallel):                                        │
│  build/format/tests · migration guard · OpenAPI contract     │
│  CodeQL · SCA · gitleaks · image Trivy · SBOM                │
│  ephemeral stack → seed → NBomber smoke → ZAP API scan       │
└─────────────────────────────────────────────────────────────┘
       │ all green + review → merge
       ▼ push to main
┌─────────────────────────────────────────────────────────────┐
│  CD:                                                         │
│  1. container-scan (gate)                                    │
│  2. build+push GHCR (api + migrate)                           │
│     └ SBOM + SLSA attestations + cosign, latest + SHA tags   │
│  3. deploy on the droplet via SSH:                            │
│     sync config → sops decrypt → run migrate (gate)          │
│     → up api → poll /health/ready (gate)                     │
│     → NR deployment marker → NRQL release gate (gate)        │
│     → GitHub Deployment registered                           │
└─────────────────────────────────────────────────────────────┘
       │
       ▼ ongoing
  weekly crons: CodeQL/SCA/gitleaks (main) · perf-load · DORA
```
