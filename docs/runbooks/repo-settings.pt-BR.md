# Runbook — Branch protection, Environments e Secrets

[English](./repo-settings.md) · **Português**

Reproduz do zero a configuração do repositório `VitorEspinoza/LabViroMol`
no GitHub: regras de branch protection em `main`, Environments (`production`
e `infra-prod`) com required reviewers, secrets de repositório e de
environment, e habilitação do code scanning.

Todos os comandos `gh api` abaixo são reproduzíveis. Execute-os com a
`gh` CLI autenticada com um PAT que tenha permissão `admin:repo` (ou seja
owner do repositório).

---

## 0. Pré-requisitos

```bash
gh auth login          # autenticar (scope admin:repo)
gh auth status         # confirmar usuário e repo
REPO="VitorEspinoza/LabViroMol"
```

---

## 1. Branch protection — `main`

### 1.1. Criar/substituir a regra

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

### 1.2. Notas sobre os nomes dos checks

Os nomes acima são os valores exatos do campo `name:` de cada job nos
arquivos de workflow (confirmados contra os arquivos reais em
`.github/workflows/`):

| Nome do check (GitHub) | Job ID | Arquivo |
|---|---|---|
| `build-lint` | `build-lint` | `ci-build-test.yml` |
| `unit-arch-tests` | `unit-arch-tests` | `ci-build-test.yml` |
| `integration-tests` | `integration-tests` | `ci-build-test.yml` |
| `ci-required` | `ci-required` | `ci-build-test.yml` (sentinela) |
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

**Importante:** os checks do `migration-guard.yml` e do `api-contract.yml`
(`breaking-change-gate`) só rodam quando o PR toca migrations ou qualquer
código, respectivamente — o GitHub trata checks que "não rodaram" como
`pending`/`expected`, não como `success`, então eles bloqueiam o merge se
não tiverem rodado. Para esses checks opcionais-por-path, use a opção
`"context"` sem `"app_id"` e considere configurá-los como
`"required_status_checks"` apenas se todo PR pode acioná-los; do contrário,
prefira deixá-los como checks de informação e confiar no gate de CODEOWNERS
para migrations e no gate de label para breaking changes.

**Alternativa (UI):** Settings → Branches → Add branch ruleset → Protection
rules for `main`. Preencher os campos correspondentes a cada item do JSON
acima. A UI é mais fácil para a primeira configuração; o comando acima
permite recriar tudo em um fork/clone.

### 1.3. Verificar a regra ativa

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

### 2.1. `production` (deploy da API)

```bash
# Criar o environment (se não existir)
gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  /repos/${REPO}/environments/production \
  --input - <<'EOF'
{
  "wait_timer": 0,
  "reviewers": [
    { "type": "User", "id": <USER_ID_NUMERICO> }
  ],
  "deployment_branch_policy": {
    "protected_branches": true,
    "custom_branch_policies": false
  }
}
EOF
```

Substituir `<USER_ID_NUMERICO>` pelo ID numérico do reviewer (obter via
`gh api /users/<username> | jq .id`).

---

## 3. Secrets

### 3.1. Secrets de repositório (nível Actions)

Os secrets abaixo são necessários para os workflows de deploy e infra.
Configure via `gh secret set` ou Settings → Secrets and variables →
Actions.

```bash
# Deploy
gh secret set DROPLET_HOST    --repo ${REPO}  # IP ou hostname do servidor
gh secret set DROPLET_USER    --repo ${REPO}  # usuário SSH (ex.: deploy)
gh secret set DROPLET_SSH_KEY --repo ${REPO}  # chave privada PEM
gh secret set GHCR_PAT        --repo ${REPO}  # PAT com read:packages (docker login no servidor)
gh secret set SOPS_AGE_KEY    --repo ${REPO}  # chave privada age para decriptar secrets
```

### 3.2. Secrets de environment `production` (valores de app)

```bash
gh secret set DB_CONNECTION_STRING --env production --repo ${REPO}
gh secret set JWT_KEY              --env production --repo ${REPO}
gh secret set JWT_ISSUER           --env production --repo ${REPO}
gh secret set JWT_AUDIENCE         --env production --repo ${REPO}
gh secret set EMAIL_API_KEY        --env production --repo ${REPO}  # API key da Brevo
gh secret set FRONTEND_BASE_URL    --env production --repo ${REPO}
gh secret set CORS_ORIGIN_ADMIN    --env production --repo ${REPO}
gh secret set CORS_ORIGIN_INST     --env production --repo ${REPO}
gh secret set NR_LICENSE_KEY       --env production --repo ${REPO}
```

> Os mesmos valores ficam criptografados em `secrets/prod.enc.env` (plano 23 —
> SOPS/age). O `cd.yml` decripta esse arquivo na droplet via `sops --decrypt`
> durante o deploy; os secrets de environment acima são uma camada adicional
> para uso direto em steps do runner, se necessário.

### 3.3. Validar presença dos secrets (não exibe valores)

```bash
gh api /repos/${REPO}/actions/secrets | jq '[.secrets[].name] | sort'
gh api /repos/${REPO}/environments/production/secrets | jq '[.secrets[].name] | sort'
```

---

## 4. Code scanning (SARIF)

Os workflows `codeql.yml`, `sca.yml`, `secrets.yml`, `sbom.yml` e
`container-scan.yml` sobem resultados SARIF para a aba Security →
Code scanning do repositório. Para receber esses uploads, o code scanning
deve estar habilitado:

```bash
gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  /repos/${REPO}/code-scanning/default-setup \
  -f state=configured \
  -f languages='["csharp"]'
```

Ou via UI: Settings → Security → Code scanning → Set up → Advanced (para
não conflitar com o `codeql.yml` customizado já existente, prefira
"Advanced" e não "Default" — o workflow já está configurado manualmente).

**Secret scanning nativo** (opcional): Settings → Security → Secret
scanning → Enable. Complementa o Gitleaks com detecção gerenciada pelo
GitHub. Não gera conflito com `secrets.yml`/Gitleaks.

---

## 5. CODEOWNERS — ação necessária antes de ativar

O arquivo `.github/CODEOWNERS` (criado pelo plano 14) contém o placeholder
`@<owner-senior>`. **A regra "Require review from Code Owners" na branch
protection só funciona corretamente após substituir esse placeholder** pelo
usuário ou time GitHub real que deve revisar migrations destrutivas.

```bash
# Editar .github/CODEOWNERS — substituir @<owner-senior>
# por ex.: @VitorEspinoza  ou  @VitorEspinoza/senior-reviewers
```

Após o commit, a branch protection já ativada usa o CODEOWNERS atualizado
automaticamente.

---

## 6. Labels necessárias no repositório

Os workflows `migration-guard.yml` e `api-contract.yml` leem labels do PR
para liberar o merge em casos controlados. Criar as labels se não existirem:

```bash
gh label create "migration-reviewed" \
  --repo ${REPO} \
  --color "e4e669" \
  --description "Migration destrutiva revisada e aprovada para merge"

gh label create "api-breaking-approved" \
  --repo ${REPO} \
  --color "d93f0b" \
  --description "Breaking change de API aprovado — consumidores foram notificados"
```

---

## 7. Checklist de verificação pós-configuração

- [ ] `gh api /repos/${REPO}/branches/main/protection` retorna a regra com
      todos os checks listados na seção 1.
- [ ] Environment `production` existe com required reviewers configurados.
- [ ] Todos os secrets da seção 3.1 e 3.2 estão presentes (sem valor vazio).
- [ ] `.github/CODEOWNERS` não contém mais `@<owner-senior>` (substituído).
- [ ] Labels `migration-reviewed` e `api-breaking-approved` existem no repo.
- [ ] PR de teste: abrir um PR dummy em `main` e confirmar que todos os
      checks aparecem como "required" na UI do PR.
- [ ] PR que toca `src/Modules/**/Migrations/` aparece como "Changes
      requested" pelo CODEOWNER definido.
- [ ] Merge sem aprovação de PR é bloqueado (testar via API ou tentativa de
      merge direto).

---

## 8. Recriar tudo num fork/clone

1. `export REPO="<owner>/<fork>"`
2. Executar as seções 1 a 6 na ordem.
3. Garantir que o CODEOWNERS tem um owner real do fork (não
   `@VitorEspinoza`, que não será collaborator do fork automaticamente).
4. Configurar os secrets com os valores reais do novo ambiente.
