# Runbook — Branch protection, Environments and Secrets

**English** · [Português](./repo-settings.pt-BR.md)

Reproduces from scratch the GitHub configuration of the
`VitorEspinoza/LabViroMol` repository: branch protection rules on `main`,
Environments (`production` and `infra-prod`) with required reviewers,
repository- and environment-level secrets, and enabling code scanning.

All the `gh api` commands below are reproducible. Run them with the `gh`
CLI authenticated with a PAT that has `admin:repo` permission (i.e., be an
owner of the repository).

---

## 0. Prerequisites

```bash
gh auth login          # authenticate (scope admin:repo)
gh auth status         # confirm user and repo
REPO="VitorEspinoza/LabViroMol"
```

---

## 1. Branch protection — `main`

### 1.1. Create/replace the rule

```bash
gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  /repos/${REPO}/branches/main/protection \
  --input - <<'EOF'
{
  "required_status_checks": {
    "strict": true,
    "checks": [
      { "context": "build-lint" },
      { "context": "unit-arch-tests" },
      { "context": "integration-tests" },
      { "context": "ci-required" },
      { "context": "Analyze (csharp)" },
      { "context": "dotnet list package --vulnerable" },
      { "context": "Trivy filesystem (vuln + license)" },
      { "context": "Gitleaks" },
      { "context": "Generate SBOM + scan (Grype)" },
      { "context": "Build + Trivy image scan (API)" },
      { "context": "Check destructive migrations" },
      { "context": "Generate OpenAPI spec (build-time)" },
      { "context": "oasdiff breaking-change gate" },
      { "context": "Ephemeral stack -> NBomber smoke -> ZAP baseline" }
    ]
  },
  "required_pull_request_reviews": {
    "dismiss_stale_reviews": true,
    "require_code_owner_reviews": true,
    "required_approving_review_count": 1,
    "require_last_push_approval": false
  },
  "enforce_admins": false,
  "restrictions": null,
  "required_conversation_resolution": true,
  "allow_force_pushes": false,
  "allow_deletions": false
}
EOF
```

### 1.2. Notes on the check names

The names above are the exact values of the `name:` field of each job in
the workflow files (confirmed against the actual files in
`.github/workflows/`):

| Check name (GitHub) | Job ID | File |
|---|---|---|
| `build-lint` | `build-lint` | `ci-build-test.yml` |
| `unit-arch-tests` | `unit-arch-tests` | `ci-build-test.yml` |
| `integration-tests` | `integration-tests` | `ci-build-test.yml` |
| `ci-required` | `ci-required` | `ci-build-test.yml` (sentinel) |
| `Analyze (csharp)` | `analyze` | `codeql.yml` |
| `dotnet list package --vulnerable` | `dotnet-vulnerable-packages` | `sca.yml` |
| `Trivy filesystem (vuln + license)` | `trivy-fs` | `sca.yml` |
| `Gitleaks` | `gitleaks` | `secrets.yml` |
| `Generate SBOM + scan (Grype)` | `sbom` | `sbom.yml` |
| `Build + Trivy image scan (API)` | `trivy-image` | `container-scan.yml` |
| `Check destructive migrations` | `migration-guard` | `migration-guard.yml` |
| `Generate OpenAPI spec (build-time)` | `generate-spec` | `api-contract.yml` |
| `oasdiff breaking-change gate` | `breaking-change-gate` | `api-contract.yml` |
| `Ephemeral stack -> NBomber smoke -> ZAP baseline` | `dynamic-dast-perf` | `dynamic.yml` |

**Important:** the `migration-guard.yml` and `api-contract.yml`
(`breaking-change-gate`) checks only run when the PR touches migrations
or any code, respectively — GitHub treats checks that "didn't run" as
`pending`/`expected`, not `success`, so they block the merge if they
haven't run. For these path-conditional checks, use the `"context"`
option without `"app_id"` and consider configuring them as
`"required_status_checks"` only if every PR can trigger them; otherwise,
prefer leaving them as informational checks and rely on the CODEOWNERS
gate for migrations and the label gate for breaking changes.

**Alternative (UI):** Settings → Branches → Add branch ruleset →
Protection rules for `main`. Fill in the fields corresponding to each item
in the JSON above. The UI is easier for the first setup; the command
above allows recreating everything on a fork/clone.

### 1.3. Verify the active rule

```bash
gh api \
  -H "Accept: application/vnd.github+json" \
  /repos/${REPO}/branches/main/protection \
  | jq '{
      required_checks: .required_status_checks.checks[].context,
      require_codeowner_reviews: .required_pull_request_reviews.require_code_owner_reviews,
      require_conversation_resolution: .required_conversation_resolution.enabled,
      allow_force_pushes: .allow_force_pushes.enabled,
      allow_deletions: .allow_deletions.enabled
    }'
```

---

## 2. Environments

### 2.1. `production` (API deploy)

```bash
# Create the environment (if it doesn't exist)
gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  /repos/${REPO}/environments/production \
  --input - <<'EOF'
{
  "wait_timer": 0,
  "reviewers": [
    { "type": "User", "id": <NUMERIC_USER_ID> }
  ],
  "deployment_branch_policy": {
    "protected_branches": true,
    "custom_branch_policies": false
  }
}
EOF
```

Replace `<NUMERIC_USER_ID>` with the reviewer's numeric ID (get it via
`gh api /users/<username> | jq .id`).

---

## 3. Secrets

### 3.1. Repository-level secrets (Actions)

The secrets below are required for the deploy and infra workflows.
Configure them via `gh secret set` or Settings → Secrets and variables →
Actions.

```bash
# Deploy
gh secret set DROPLET_HOST    --repo ${REPO}  # server IP or hostname
gh secret set DROPLET_USER    --repo ${REPO}  # SSH user (e.g., deploy)
gh secret set DROPLET_SSH_KEY --repo ${REPO}  # PEM private key
gh secret set GHCR_PAT        --repo ${REPO}  # PAT with read:packages (docker login on the server)
gh secret set SOPS_AGE_KEY    --repo ${REPO}  # age private key to decrypt secrets
```

### 3.2. `production` environment secrets (app values)

```bash
gh secret set DB_CONNECTION_STRING --env production --repo ${REPO}
gh secret set JWT_KEY              --env production --repo ${REPO}
gh secret set JWT_ISSUER           --env production --repo ${REPO}
gh secret set JWT_AUDIENCE         --env production --repo ${REPO}
gh secret set EMAIL_API_KEY        --env production --repo ${REPO}  # Brevo API key
gh secret set FRONTEND_BASE_URL    --env production --repo ${REPO}
gh secret set CORS_ORIGIN_ADMIN    --env production --repo ${REPO}
gh secret set CORS_ORIGIN_INST     --env production --repo ${REPO}
gh secret set NR_LICENSE_KEY       --env production --repo ${REPO}
```

> The same values are encrypted in `secrets/prod.enc.env` (plan 23 —
> SOPS/age). `cd.yml` decrypts that file on the droplet via `sops
> --decrypt` during deploy; the environment secrets above are an
> additional layer for direct use in runner steps, if needed.

### 3.3. Validate secret presence (doesn't show values)

```bash
gh api /repos/${REPO}/actions/secrets | jq '[.secrets[].name] | sort'
gh api /repos/${REPO}/environments/production/secrets | jq '[.secrets[].name] | sort'
```

---

## 4. Code scanning (SARIF)

The `codeql.yml`, `sca.yml`, `secrets.yml`, `sbom.yml` and
`container-scan.yml` workflows upload SARIF results to the repository's
Security → Code scanning tab. To receive these uploads, code scanning
must be enabled:

```bash
gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  /repos/${REPO}/code-scanning/default-setup \
  -f state=configured \
  -f languages='["csharp"]'
```

Or via the UI: Settings → Security → Code scanning → Set up → Advanced
(to avoid conflicting with the already-existing custom `codeql.yml`,
prefer "Advanced" over "Default" — the workflow is already configured
manually).

**Native secret scanning** (optional): Settings → Security → Secret
scanning → Enable. Complements Gitleaks with GitHub-managed detection.
Doesn't conflict with `secrets.yml`/Gitleaks.

---

## 5. CODEOWNERS — action required before enabling

The `.github/CODEOWNERS` file (created by plan 14) contains the
placeholder `@<owner-senior>`. **The "Require review from Code Owners"
rule in branch protection only works correctly after replacing this
placeholder** with the real GitHub user or team that should review
destructive migrations.

```bash
# Edit .github/CODEOWNERS — replace @<owner-senior>
# with e.g.: @VitorEspinoza  or  @VitorEspinoza/senior-reviewers
```

After the commit, branch protection, once enabled, automatically uses the
updated CODEOWNERS.

---

## 6. Labels required in the repository

The `migration-guard.yml` and `api-contract.yml` workflows read PR labels
to allow merging in controlled cases. Create the labels if they don't
exist:

```bash
gh label create "migration-reviewed" \
  --repo ${REPO} \
  --color "e4e669" \
  --description "Destructive migration reviewed and approved for merge"

gh label create "api-breaking-approved" \
  --repo ${REPO} \
  --color "d93f0b" \
  --description "API breaking change approved — consumers have been notified"
```

---

## 7. Post-configuration verification checklist

- [ ] `gh api /repos/${REPO}/branches/main/protection` returns the rule
      with all the checks listed in section 1.
- [ ] Environment `production` exists with required reviewers configured.
- [ ] All secrets from sections 3.1 and 3.2 are present (no empty value).
- [ ] `.github/CODEOWNERS` no longer contains `@<owner-senior>` (replaced).
- [ ] Labels `migration-reviewed` and `api-breaking-approved` exist in the
      repo.
- [ ] Test PR: open a dummy PR against `main` and confirm all checks
      appear as "required" in the PR UI.
- [ ] A PR touching `src/Modules/**/Migrations/` shows "Changes
      requested" from the defined CODEOWNER.
- [ ] Merging without PR approval is blocked (test via API or a direct
      merge attempt).

---

## 8. Recreating everything on a fork/clone

1. `export REPO="<owner>/<fork>"`
2. Run sections 1 through 6 in order.
3. Make sure CODEOWNERS has a real owner of the fork (not
   `@VitorEspinoza`, who won't automatically be a collaborator on the
   fork).
4. Configure the secrets with the real values of the new environment.
