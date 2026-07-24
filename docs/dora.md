# DORA Metrics — LabViroMol

**English** · [Português](./dora.pt-BR.md)

Documentation for the **4 DORA metrics** (DevOps Research and Assessment) implemented for this
repository. The goal is to track the **evolution of the software delivery process** over time — the
value is in the trend, not in the absolute number of any single week.

---

## The 4 metrics

### 1. Deployment Frequency

**What it measures:** how many times per day the team deploys to production, on average.

**How it's calculated:** number of GitHub Deployments with `state: success` on the `production`
environment, divided by the number of days in the analysis window (default: 30).

**Data source:** GitHub Deployments created by the `deploy` job of the `cd.yml` workflow via
`actions/github-script@v7` (the "Register GitHub Deployment" step).

**DORA 2023 classification:**
| Level | Frequency |
|-------|-----------|
| Elite | Several times a day (≥ 1/day) |
| High | Between once a week and once a day |
| Medium | Between once a month and once a week |
| Low | Less than once a month |

---

### 2. Lead Time for Changes

**What it measures:** how long it takes from a PR's first commit to that code being deployed to
production, as a median.

**How it's calculated:**
1. For each PR merged within the window, find the timestamp of the PR's first commit.
2. Find the closest successful (`success`) deploy **after** the PR merge.
3. Compute `delta = deploy_timestamp − first_commit_timestamp` in hours.
4. Return the **median** of all deltas.

**Data source:** GitHub Pulls API (each PR's commits) + GitHub Deployments.

**Why median and not mean:** the median is more robust to outliers (e.g., huge PRs that sat in
review for weeks, or hotfix re-deploys taking minutes). The median's central tendency reflects the
normal delivery process.

**DORA 2023 classification:**
| Level | Lead time |
|-------|-----------|
| Elite | < 1 hour |
| High | Between 1 hour and 1 day |
| Medium | Between 1 day and 1 week |
| Low | > 1 week |

---

### 3. Change Failure Rate

**What it measures:** the percentage of deploys that result in failure, degradation, or the need
for a hotfix/rollback.

**How it's calculated:**
```
CFR = (failed_deploys + PRs_with_hotfix_or_rollback_label) / total_deploys × 100
```

- `failed_deploys`: GitHub Deployments with `state: failure` or `error`.
- `PRs with hotfix/rollback label`: PRs merged within the window with the `hotfix` or `rollback`
  label (a proxy for "the previous deploy broke something").
- `total_deploys`: success + failure.

**Data source:** GitHub Deployments + GitHub Pulls API (labels).

**DORA 2023 classification:**
| Level | CFR |
|-------|-----|
| Elite | 0 – 5% |
| High | 5 – 10% |
| Medium | 10 – 15% |
| Low | > 15% |

---

### 4. MTTR — Mean Time To Recovery

**What it measures:** how long it takes to restore the service after a failed deploy. The name is
"mean" by historical convention, but here we use the **median** (more robust).

**How it's calculated:**
1. For each deploy with `state: failure/error`, find the next deploy with `state: success` (the
   recovery deploy).
2. Compute `delta = recovery_timestamp − failure_timestamp` in hours.
3. Return the **median** of all deltas.

**Data source:** GitHub Deployments.

**Note:** if there was no failed deploy within the window, MTTR = N/A (a good sign).

**DORA 2023 classification:**
| Level | MTTR |
|-------|------|
| Elite | < 1 hour |
| High | < 1 day |
| Medium | Between 1 day and 1 week |
| Low | > 1 week |

---

## Analysis window

The default is a rolling **30 days**. It can be adjusted via `workflow_dispatch` on the `dora.yml`
workflow (the `window_days` parameter).

**Why 30 days:** long enough to smooth out atypical weeks (holidays, stabilization sprints) without
including old history that no longer reflects the current process.

---

## How to read the results

1. **N/A is not an alarm.** Early in the project, with few deploys or few PRs in the window, some
   metrics show up as "N/A" for lack of data. This is expected — the metrics gain meaning as history
   grows.

2. **Trend matters more than absolute level.** A project that moves from "Medium" to "High" over 3
   months is making real progress, even if it isn't "Elite" yet.

3. **Low Deployment Frequency isn't necessarily bad.** For a lab-management system (not
   e-commerce), 2–3 deploys/week may be adequate. DORA defines "Elite" for systems that can — and
   should — deploy multiple times a day; use it as a reference, not an absolute target.

4. **CFR and MTTR are the most critical.** A high frequency with a high CFR means every deploy is
   risky. Prioritize reducing CFR before increasing frequency.

---

## Collection workflow

| File | Role |
|---------|-------|
| `.github/workflows/dora.yml` | Scheduled workflow (every Monday 08:00 UTC) + `workflow_dispatch` |
| `scripts/dora/compute-dora.sh` | Shell script that consumes the GitHub API and computes the 4 metrics |

**Required permissions** (already configured in the workflow):
- `deployments: read` — list GitHub Deployments and their statuses
- `pull-requests: read` — list PRs and their commits for lead time
- `contents: read` — checkout the repo to run the script

**Authentication:** `GITHUB_TOKEN` (no additional PAT). All the APIs used (`deployments`, `pulls`,
`commits`) are per-repo and `GITHUB_TOKEN` has sufficient access. For an **aggregated** view across
the 3 repositories (LabViroMol + admin-panel + institutional), a PAT with `repo:read` scope on all 3
repos would be needed — documented as future work; the current implementation measures each repo
independently.

---

## Instrumentation: where Deployments are created

The `deploy` job in `.github/workflows/cd.yml` (the "Register GitHub Deployment" step) creates a
GitHub Deployment + DeploymentStatus after every deploy attempt, with:

- `environment: production`
- `ref: <github.sha>` of the deployed commit
- `state: success` if `/health/ready` returned 200; `failure` otherwise
- `description`: a human message describing the result

This is the primary source for all 4 metrics above.

---

## Future evolution

- **Multi-repo view:** configure a PAT with `repo` scope across the 3 repositories and run the
  script once per repo, consolidating into a single summary. Requires an additional secret
  (`DORA_AGGREGATION_PAT`) and coordination across the repositories.
- **Persisted history:** commit the weekly summary to `docs/dora-history.jsonl` (one JSON line per
  week) to graph the evolution over time. Depends on adjusting the `**/docs/` entry in `.gitignore`,
  which currently would ignore that file.
- **Regression alerts:** add a step to `dora.yml` that fails if CFR > 20% or MTTR > 24h in the week,
  emitting an annotation in the summary and notifying via `SLACK_WEBHOOK` (if configured).

### Last automatic run

*No run yet — the workflow runs every Monday at 08:00 UTC.*
