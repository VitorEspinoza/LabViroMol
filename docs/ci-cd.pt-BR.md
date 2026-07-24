# CI/CD — Guia de ponta a ponta

[English](./ci-cd.md) · **Português**

Este documento explica todo o pipeline de CI/CD do LabViroMol: quais workflows existem,
quando cada um dispara, o que cada etapa valida ou produz, e como os comandos dentro dos
workflows comunicam sucesso/falha entre si.

> Arquivos-fonte: [`.github/workflows/`](../.github/workflows/), [`Dockerfile`](../Dockerfile),
> [`Dockerfile.migrate`](../Dockerfile.migrate), [`docker-compose.ci.yaml`](../docker-compose.ci.yaml),
> [`docker-compose.prod.yaml`](../docker-compose.prod.yaml), [`scripts/ci/`](../scripts/ci/),
> [`scripts/deploy/`](../scripts/deploy/), [`scripts/dora/`](../scripts/dora/).

---

## 1. Visão geral — os 12 workflows

| Workflow | Arquivo | Gatilho | Papel | Bloqueia merge/deploy? |
|---|---|---|---|---|
| CI - Build + Tests | `ci-build-test.yml` | PR → main; push em `feat/**`, `fix/**`, `refactor/**` | Compila, verifica formatação, roda testes unitários/arquitetura/integração | **Sim** (via sentinel `ci-required`) |
| CI - Migration Guard | `migration-guard.yml` | PR → main que toque em `Migrations/*.cs` | Detecta migrations destrutivas (DROP etc.) | **Sim**, salvo label `migration-reviewed` |
| API Contract | `api-contract.yml` | PR → main; push em main | Gera spec OpenAPI no build e compara com a da main (breaking changes) | **Sim**, salvo label `api-breaking-approved` |
| CodeQL | `codeql.yml` | PR; push em main; cron semanal | SAST (análise estática de segurança do C#) | Sim (check do PR) |
| SCA | `sca.yml` | PR; cron semanal | Vulnerabilidades em dependências (NuGet + Trivy fs) | **Sim** para High/Critical |
| Secrets scanning | `secrets.yml` | PR; push em main; cron semanal | Gitleaks — segredos commitados | Sim (check do PR) |
| Container Scan | `container-scan.yml` | PR; e **chamado pelo CD** (`workflow_call`) | Constrói a imagem da API e escaneia com Trivy | **Sim** no PR e como gate do CD |
| SBOM | `sbom.yml` | PR; push em main | Gera SBOM (CycloneDX/SPDX) da imagem e escaneia com Grype | Não gateia (informativo + Security tab) |
| Dynamic (DAST + Perf Smoke) | `dynamic.yml` | PR → main | Stack efêmera real → seed → NBomber smoke → ZAP API scan (contrato OpenAPI, autenticado) | **Sim** (thresholds NBomber + regras FAIL do ZAP) |
| CD | `cd.yml` | Push em main (= merge de PR) | Scan-gate → build/push de imagens assinadas → deploy migrate-first na droplet → gates pós-deploy | — (é o próprio deploy) |
| Perf - Load Tests | `perf-load.yml` | Manual (`workflow_dispatch`) ou cron semanal | Carga pesada (load/stress/soak/spike/breakpoint) fora do caminho do PR | Não |
| DORA Metrics | `dora.yml` | Cron semanal ou manual | Calcula métricas DORA a partir do histórico do GitHub | Não |

Todos usam `paths-ignore` para **não** disparar quando o commit só altera `**/*.md`, `docs/**`
ou `infra/**` — mudança de documentação não queima minutos de CI nem re-deploya nada.

Dois blocos comuns a quase todos:

- **`concurrency`** — agrupa execuções por ref (`ci-${{ github.ref }}` etc.) com
  `cancel-in-progress: true`: se você empurra um commit novo no mesmo PR, a execução antiga é
  cancelada em vez de correr em paralelo. Exceções deliberadas: CD, perf-load e DORA usam
  `cancel-in-progress: false` — um deploy ou um teste de carga em andamento nunca deve ser
  cancelado no meio.
- **`permissions: contents: read`** — o `GITHUB_TOKEN` do job nasce com o mínimo; cada job
  eleva pontualmente só o que precisa (`security-events: write` para publicar SARIF,
  `packages: write` para push no GHCR, etc.). É por isso que o ZAP roda com
  `allow_issue_writing: false`: o workflow não tem `issues: write` de propósito.

---

## 2. A vida de um Pull Request

Quando você abre um PR contra `main`, disparam **em paralelo**:

```
PR aberto/atualizado
 ├─ CI - Build + Tests        (build, format, unit+arch, integration)
 ├─ Migration Guard           (só se tocou em Migrations/*.cs)
 ├─ API Contract              (gera spec e diffa contra a main)
 ├─ CodeQL                    (SAST)
 ├─ SCA                       (dependências vulneráveis)
 ├─ Secrets scanning          (gitleaks no range do PR)
 ├─ Container Scan            (imagem da API + Trivy)
 ├─ SBOM                      (inventário de componentes + Grype)
 └─ Dynamic (DAST + Perf)     (stack efêmera + NBomber + ZAP)
```

O merge só é liberado quando os checks obrigatórios (branch protection) ficam verdes.
Detalhe de cada um:

### 2.1 CI - Build + Tests (`ci-build-test.yml`)

Três jobs paralelos + um sentinel:

- **build-lint** — `dotnet restore` → `dotnet build -c Release --no-restore` →
  `dotnet format --verify-no-changes --severity error`. O `--verify-no-changes` faz o
  formatador rodar em modo "só verificação": se algum arquivo precisaria ser reformatado,
  o comando retorna exit ≠ 0 e o job falha (formatação vira gate, não sugestão).
- **unit-arch-tests** — roda `dotnet test` num loop explícito sobre os projetos de testes
  unitários de domínio de cada módulo + os testes de arquitetura (que verificam regras como
  "Domain não referencia Infrastructure"). `--collect:"XPlat Code Coverage"` gera cobertura,
  publicada como artefato `coverage-unit-arch`.
- **integration-tests** — descobre os projetos em `tests/IntegrationTests` via `find` e roda
  cada um. Usam **Testcontainers**: cada teste sobe um Postgres real em Docker, por isso não
  há serviço de banco declarado no workflow — o próprio teste orquestra o container.
- **ci-required (sentinel)** — job com `needs: [build-lint, unit-arch-tests, integration-tests]`
  e `if: always()` que falha se qualquer upstream falhou. Existe para a branch protection
  apontar para **um único check** estável ("CI required") em vez de três, o que simplifica a
  configuração e continua funcionando se jobs forem renomeados/adicionados.

### 2.2 Migration Guard (`migration-guard.yml`)

Só dispara se o PR tocar em `src/Modules/*/Infrastructure/Persistence/Migrations/*.cs`.
Roda `scripts/ci/check-destructive-migrations.sh` comparando a base do PR com o head — o
script procura operações destrutivas (DropTable, DropColumn etc.).

Repare no padrão de **gate com válvula de escape**, usado aqui e no API Contract:

```
set +e                       # desliga o "falhe no primeiro erro"
bash script.sh ...           # roda a verificação
echo "exit_code=$?" >> "$GITHUB_OUTPUT"   # guarda o resultado como output do step
exit 0                       # o step em si nunca falha
```

O step seguinte lê `exit_code` + verifica se o PR tem a label `migration-reviewed`
(via `jq` sobre `github.event.pull_request.labels`). A decisão final ("Evaluate gate")
combina os dois: destrutiva **sem** label → `exit 1` (bloqueia); **com** label → libera,
apoiado na exigência de aprovação de CODEOWNERS do path de Migrations.

### 2.3 API Contract (`api-contract.yml`)

Protege os consumidores da API (Admin Panel e site institucional) contra breaking changes
acidentais:

1. **generate-spec** — o build da API emite o OpenAPI em build-time (propriedades
   `OpenApiGenerateDocumentsOnBuild`/`OpenApiDocumentsDirectory` no csproj apontam para
   `contracts/`). O JSON gerado vira `revision.openapi.json`; a versão "oficial" é lida da
   main com `git show origin/main:contracts/openapi.json`.
2. **breaking-change-gate** (só em PR) — roda o container `tufin/oasdiff` comparando base ×
   revision com `--fail-on ERR`. Breaking change sem a label `api-breaking-approved` → bloqueia;
   com a label → libera (mudança consciente, frontends avisados).
3. **publish-spec** (só em push na main) — commita o `contracts/openapi.json` atualizado com
   `[skip ci]` na mensagem para não disparar os workflows de novo.

### 2.4 Segurança estática: CodeQL, SCA, Secrets, Container Scan, SBOM

- **CodeQL** (SAST) — compila a solução sob instrumentação do CodeQL (`build-mode: manual`,
  por isso o `dotnet build` explícito entre `init` e `analyze`) e publica os achados na aba
  **Security → Code scanning** do repositório.
- **SCA** — duas frentes: `dotnet list package --vulnerable --include-transitive` (advisories
  do NuGet) com um grep que falha o job se aparecer High/Critical; e Trivy em modo filesystem
  rodando **duas vezes** — uma em SARIF com `exit-code: 0` (só relatório, vai para a Security
  tab) e outra em tabela com `exit-code: 1` restrita a HIGH/CRITICAL (essa é o gate). Esse
  padrão "scan duplo: um para visibilidade, um para gate" se repete no Container Scan.
- **Secrets** — Gitleaks. Em PR, escaneia só o range de commits do PR
  (`--no-merges base..head`); em push/cron, o histórico. `fetch-depth: 0` no checkout é
  necessário porque o scan olha o histórico do git, não só o working tree.
- **Container Scan** — constrói a imagem da API (`push: false, load: true` = imagem fica só
  no daemon local do runner) e roda Trivy contra ela (vuln + misconfig + secret).
  `ignore-unfixed: true` evita bloquear por CVE sem correção disponível. Este workflow tem
  `workflow_call` no gatilho — é assim que o **CD o reutiliza como gate** antes de publicar.
- **SBOM** — gera o inventário de componentes da imagem em dois formatos (CycloneDX e SPDX)
  com Syft e escaneia com Grype. Não gateia; serve para rastreabilidade de supply chain.

### 2.5 Dynamic — DAST + Perf Smoke (`dynamic.yml`)

O único workflow de PR que testa a aplicação **rodando de verdade**. Sequência:

1. **Subir stack efêmera** — `docker compose -f docker-compose.ci.yaml up -d --wait
   --wait-timeout 180`. O compose de CI define três serviços encadeados por dependências
   *com condição*:
   - `postgres` (com healthcheck `pg_isready`; publica `127.0.0.1:5432` para os steps do runner);
   - `migrate` (`depends_on: postgres: service_healthy`) — container one-shot construído do
     `Dockerfile.migrate`, que aplica as migrações e **termina**;
   - `api` (`depends_on: migrate: service_completed_successfully`) — só sobe se a migração
     saiu com exit 0; tem healthcheck HTTP em `/health/ready`.

   O `--wait` faz o comando bloquear até todos ficarem healthy/completed — ou falhar. Em caso
   de falha, o workflow despeja `docker compose logs migrate` e `logs api` no log do Actions
   (sem isso, o exit code do container fica invisível).

2. **Smoke check** — `curl -fsS $BASE_URL/health/ready`: o `-f` transforma HTTP ≥ 400 em exit ≠ 0.

3. **Seed** — `dotnet run ... -- --command=seed --profile=ci-smoke --baseUrl=...`. O seeder
   fala **direto com o Postgres** (por isso `ConnectionStrings__LabViroMol` aponta para
   `localhost:5432`, a porta publicada — o step roda no runner, fora da rede do compose).
   Atenção à sintaxe: o parser do LoadTests só aceita `--chave=valor` (com `=`).

4. **NBomber smoke** — `--profile=ci-smoke --campaign=ci --scenario=full`: carga leve contra a
   API via HTTP. Os thresholds do perfil funcionam como gate via exit code do processo.
   O relatório sobe como artefato (`nbomber-ci-smoke-report`) mesmo em falha (`if: always()`).

5. **Login para o DAST** — o ZAP não sabe autenticar sozinho; um step faz `curl` no
   `/api/identity/users/login` com um usuário criado pelo seed (`loadtest-user1@test.local`),
   extrai o cookie `X-Access-Token` do `Set-Cookie` e o publica como output mascarado
   (`::add-mask::` impede o token de vazar no log).

6. **ZAP API scan** — DAST **dirigido pelo contrato OpenAPI**, autenticado. Um build da API
   no runner emite `contracts/LabViroMol.Api.json` (mesmo mecanismo do workflow API Contract);
   a action `zaproxy/action-api-scan` importa o spec e requisita **todos** os endpoints com
   dados sintéticos gerados dos schemas + payloads de regras ativas (SQLi, injection etc.).
   Configuração relevante:
   - `ZAP_AUTH_HEADER: Cookie` / `ZAP_AUTH_HEADER_VALUE` — injeta o cookie de auth do passo
     anterior em toda requisição do scanner (a API autentica por cookie httpOnly, não por
     header `Authorization`).
   - `cmd_options: "-O http://localhost:8080 -a -I"` — `-O` aponta o server do spec para a
     stack efêmera; `-a` inclui regras alpha; `-I` faz WARNs **não** falharem o job (só regras
     marcadas como FAIL no tsv gateiam).
   - `rules_file_name: .zap/rules.tsv` — cada regra pode ser promovida a `FAIL` ou rebaixada a
     `WARN`/`IGNORE` com justificativa documentada (ex.: regras de CSP não se aplicam a uma API
     JSON pura; regras de cookie são FAIL porque a API emite cookies de autenticação).
   - `allow_issue_writing: false` — não tenta abrir issue (workflow de PR não tem
     `issues: write`; o relatório já vai de artefato).

   Notas de desenho: o scan roda **depois** do NBomber de propósito — as regras ativas
   escrevem/apagam dados reais no banco (seguro porque a stack é descartável). Muitos `400`
   nas respostas são esperados (validação rejeitando dado sintético). Limitação conhecida:
   o scan usa um único usuário privilegiado, então não testa autorização entre papéis
   (broken access control fica fora do escopo).

7. **Teardown** — `docker compose down -v` com `if: always()`: a stack é destruída mesmo se
   qualquer etapa anterior falhou (o `-v` remove os volumes, garantindo banco limpo por run).

### 2.6 Duas variáveis de conexão — por quê

O `dynamic.yml` mantém duas connection strings de propósito:

- `DB_CONNECTION_STRING` (`Host=postgres`) — usada **dentro** da rede do compose, pelos
  containers `migrate` e `api`, onde `postgres` é o hostname do serviço.
- `DB_CONNECTION_STRING_RUNNER` (`Host=localhost`) — usada pelos steps que rodam **no runner**
  (seed e NBomber), que enxergam o banco só pela porta publicada `127.0.0.1:5432`.

---

## 3. O que acontece no merge — CD (`cd.yml`)

Push na `main` (na prática, o merge de um PR) dispara o CD. Três jobs em sequência:

### 3.1 `container-scan-gate`

`uses: ./.github/workflows/container-scan.yml` — reutiliza o workflow de scan como **job
bloqueante**: se a imagem tem HIGH/CRITICAL com fix disponível, nada é publicado.

### 3.2 `build-push` — publicar imagens com cadeia de custódia

Para **cada** imagem (API via `Dockerfile`, migrate via `Dockerfile.migrate`):

1. **Build + push** para o GHCR com duas tags: `latest` e `${{ github.sha }}` (a tag imutável
   por commit é a que o deploy usa). `cache-from/to: type=gha` reaproveita camadas Docker
   entre execuções via cache do Actions.
2. **SBOM** da imagem publicada (Syft, CycloneDX).
3. **Atestação de SBOM** (`actions/attest-sbom`) — vincula criptograficamente o SBOM ao
   digest da imagem e grava no registry.
4. **Atestação de proveniência SLSA** (`actions/attest-build-provenance`) — registra "esta
   imagem foi construída por este workflow, deste commit, neste repositório".
5. **Assinatura cosign keyless** — `cosign sign` usando a identidade OIDC do próprio workflow
   (por isso `id-token: write` nas permissions; não há chave privada guardada).

Tudo referencia o **digest** (`@sha256:...`), não a tag — tag pode ser movida, digest não.
Os digests saem como outputs do job e vão para o job summary.

### 3.3 `deploy` — migrate-first, minimal-downtime, fix-forward

Roda contra o `environment: production` (permite exigir aprovação manual e escopar secrets).
Todo o trabalho remoto é feito via SSH (`appleboy/ssh-action`) e SCP na droplet:

1. **Sync de configuração** — cria diretórios remotos e envia `docker-compose.prod.yaml` e
   `nginx/gateway.conf`.
2. **Decrypt de secrets + migração (o gate central)** — na droplet:
   - `sops --decrypt secrets/prod.enc.env > .env` — os secrets de produção vivem no repo
     **criptografados** (SOPS/age); a chave age está só na droplet (ver
     [runbooks/secrets.md](runbooks/secrets.pt-BR.md)).
   - `docker login` no GHCR e `docker compose pull migrate` da tag do commit.
   - `docker compose run --rm migrate` — executa os bundles de migração **antes** de tocar na
     API. O exit code é capturado (`set +e` / `MIGRATE_EXIT=$?`) e re-emitido: o step só passa
     se a migração passou.
3. **Deploy da API** (`if: steps.migrate.outcome == 'success'`) — `pull api` +
   `up -d api`: o compose recria só o container da API (~2–5 s de janela). Postgres,
   libretranslate, gateway nginx, certbot e os frontends **não são tocados**.
4. **Validação pós-deploy** — polling de `http://localhost:8080/health/ready` (até 18
   tentativas × 10 s). Sem 200 → job falha. A política é **fix-forward**: não há rollback
   automático; a mensagem de erro orienta o procedimento ([runbooks/deploy.md](runbooks/deploy.pt-BR.md)).
5. **Deployment marker no New Relic** — mutation GraphQL `changeTrackingCreateDeployment`
   marca o deploy na timeline de observabilidade (best-effort: ausência de secret vira
   warning, não falha).
6. **Release gate (NRQL)** — espera 3 minutos acumulando tráfego real e roda
   `scripts/deploy/nr-release-gate.sh`, que consulta o New Relic: taxa de erro ≤ 2% e
   p95 ≤ 2000 ms na janela de 5 min. Estourou → o job falha, sinalizando que o deploy
   degradou produção (resposta em [runbooks/release-gate.md](runbooks/release-gate.pt-BR.md)).
7. **Registro** — cria um **GitHub Deployment** com status success/failure (é essa API que o
   workflow de DORA consome depois) e escreve um resumo no job summary.

O ponto-chave do desenho: **se a migração falha, a API antiga continua no ar** — o `run --rm
migrate` acontece antes de qualquer `up` da API, e todos os steps seguintes têm
`if: steps.migrate.outcome == 'success'`.

### 3.4 Como a migração funciona (Dockerfile.migrate + scripts)

- **Build da imagem** ([`Dockerfile.migrate`](../Dockerfile.migrate)): num SDK .NET completo,
  instala `dotnet-ef` e roda [`scripts/ci/build-migration-bundles.sh`](../scripts/ci/build-migration-bundles.sh),
  que gera **um bundle EF Core por módulo** (`efbundle-identity`, `efbundle-inventory`, ...,
  um por DbContext) com `dotnet ef migrations bundle`. A imagem final é
  `mcr.microsoft.com/dotnet/aspnet:10.0` — precisa ser `aspnet` (não `runtime`) porque os
  bundles embutem o startup project da API, que depende do shared framework ASP.NET Core.
- **Execução** ([`scripts/ci/run-migration-bundles.sh`](../scripts/ci/run-migration-bundles.sh),
  o ENTRYPOINT): aplica os bundles **em ordem fixa** (identity → inventory → assets →
  research → scheduling → notify), passando `--connection "$DB_CONNECTION_STRING"`; qualquer
  exit ≠ 0 aborta a sequência e propaga o código.
- **Pegadinhas conhecidas** (todas já tratadas, documentadas aqui para não regredir):
  - O `ef migrations bundle` **executa o `Program.cs` da API em design-time** (e o bundle
    também, ao rodar). Logo, tudo que o `Program.cs` exige antes do `builder.Build()` precisa
    estar presente via env: `ConnectionStrings__LabViroMol` e `Storage__RootFolder`.
  - `Storage__RootFolder` precisa ser um caminho **POSIX** no Linux: um valor estilo Windows
    (`C:\...`) não é "rooted" no Linux, vira diretório literal com `\` no nome dentro do
    projeto e quebra a expansão de globs do MSBuild (`MSB3552`, dotnet/sdk#10172).

---

## 4. Workflows agendados / manuais

### Perf - Load Tests (`perf-load.yml`)

Mesma stack efêmera do `dynamic.yml`, mas com perfis pesados (`load`, `stress`, `soak`,
`spike`, `breakpoint`) escolhidos via `workflow_dispatch` (ou `load` no cron de domingo).
`timeout-minutes: 120` porque soak é longo. Não gateia nada — o objetivo é o relatório
(artefato HTML do NBomber + `summary.json` no job summary). Detalhes de operação em
[tests/LoadTests/RUNBOOK.md](../tests/LoadTests/RUNBOOK.pt-BR.md).

### DORA Metrics (`dora.yml`)

Cron semanal (ou manual, com janela configurável). Roda
[`scripts/dora/compute-dora.sh`](../scripts/dora/compute-dora.sh), que usa a API do GitHub
(deployments criados pelo CD, PRs, etc.) para calcular as quatro métricas DORA —
Deployment Frequency, Lead Time, Change Failure Rate, MTTR — e publica no job summary.
Ver [docs/dora.md](dora.pt-BR.md).

### Crons de segurança

CodeQL (seg 05:00), SCA (seg 06:00) e Secrets (seg 07:00) também rodam semanalmente **fora**
de PRs: uma CVE nova pode ser publicada para uma dependência que já está na main — o cron
pega esses casos sem depender de alguém abrir PR.

---

## 5. Conceitos e padrões usados (glossário rápido)

| Conceito | Onde aparece | O que significa |
|---|---|---|
| **Gate via exit code** | todos | Em Actions, um step falha quando o comando retorna exit ≠ 0; `set -e` aborta o script no primeiro erro, `set +e` suspende isso para capturar o código manualmente (`$?`) e decidir depois. |
| **`$GITHUB_OUTPUT`** | migration-guard, api-contract, cd | Arquivo mágico: `echo "chave=valor" >> "$GITHUB_OUTPUT"` publica um output do step, lido por outros steps como `steps.<id>.outputs.chave`. É o mecanismo de comunicação entre steps. |
| **`$GITHUB_STEP_SUMMARY`** | cd, dora, perf-load | Markdown appendado aqui aparece na página do run — resumo legível sem abrir logs. |
| **`if: always()`** | teardown, uploads | Step roda mesmo se algo antes falhou — essencial para limpeza e para publicar relatórios de execuções falhas (que são justamente as que você quer investigar). |
| **`if: steps.X.outcome == 'success'`** | cd | Encadeamento condicional: deploy da API só depois de migração ok. |
| **Sentinel job** | ci-build-test | Job agregador para a branch protection exigir um único check. |
| **Label como válvula de escape** | migration-guard, api-contract | O gate automático bloqueia por padrão; um humano libera conscientemente adicionando uma label ao PR (`migration-reviewed`, `api-breaking-approved`). |
| **Scan duplo (report + gate)** | sca, container-scan | Primeira passada com `exit-code: 0` gera SARIF completo para a Security tab; segunda com `exit-code: 1` e severidade restrita é o gate. Visibilidade total, bloqueio seletivo. |
| **SARIF / Security tab** | codeql, sca, secrets, container-scan, sbom | Formato padrão de resultado de análise; `upload-sarif` centraliza tudo em Security → Code scanning. |
| **`workflow_call`** | container-scan ← cd | Permite um workflow ser chamado como job de outro (reuso do scan como gate do CD). |
| **Stack efêmera** | dynamic, perf-load | Ambiente completo (banco + migração + API) criado do zero para o teste e destruído no fim — determinístico e sem estado herdado. |
| **`depends_on` com condição** | docker-compose.ci.yaml | `service_healthy` (espera healthcheck) e `service_completed_successfully` (espera one-shot terminar com exit 0) — é o que ordena postgres → migrate → api. |
| **Migrate-first** | cd | Migração roda e é validada antes do container novo da API subir; falhou, a versão antiga continua servindo. |
| **Fix-forward** | cd | Sem rollback automático: falha pós-deploy se corrige com novo commit/deploy. Rollback manual de imagem só é seguro se o deploy não trouxe migração nova. |
| **SBOM / atestação / cosign** | sbom, cd | Inventário de componentes, prova criptográfica de origem (SLSA) e assinatura keyless — cadeia de custódia da imagem publicada. |
| **SOPS/age** | cd | Secrets de produção versionados no repo de forma criptografada; só a droplet tem a chave para decriptar. |
| **DAST × SAST × SCA** | dynamic × codeql × sca | DAST testa a aplicação rodando (caixa-preta); SAST analisa o código-fonte; SCA analisa as dependências. Camadas complementares. |

---

## 6. Mapa mental — do commit à produção

```
branch feat/xyz ──push──► CI Build+Tests (feedback rápido no branch)
       │
       ▼ abre PR → main
┌─────────────────────────────────────────────────────────────┐
│  Gates de PR (paralelos):                                   │
│  build/format/testes · migration guard · contrato OpenAPI   │
│  CodeQL · SCA · gitleaks · Trivy imagem · SBOM              │
│  stack efêmera → seed → NBomber smoke → ZAP API scan        │
└─────────────────────────────────────────────────────────────┘
       │ tudo verde + review → merge
       ▼ push na main
┌─────────────────────────────────────────────────────────────┐
│  CD:                                                        │
│  1. container-scan (gate)                                   │
│  2. build+push GHCR (api + migrate)                         │
│     └ SBOM + atestações SLSA + cosign, tags latest + SHA    │
│  3. deploy na droplet via SSH:                              │
│     sync config → sops decrypt → run migrate (gate)         │
│     → up api → poll /health/ready (gate)                    │
│     → NR deployment marker → release gate NRQL (gate)       │
│     → GitHub Deployment registrado                          │
└─────────────────────────────────────────────────────────────┘
       │
       ▼ contínuo
  crons semanais: CodeQL/SCA/gitleaks (main) · perf-load · DORA
```
