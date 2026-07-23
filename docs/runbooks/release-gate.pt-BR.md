# Runbook — Release gate pós-deploy (New Relic)

[English](./release-gate.md) · **Português**

## O que é

O release gate é uma verificação automática que roda **após** o deploy da API e
a validação do `/health/ready`. Ele consulta métricas reais de erro e latência
via NerdGraph (New Relic) e falha o job de CD se os thresholds forem violados.

O health check diz "o processo está de pé"; o release gate diz "está saudável
sob tráfego real".

## Thresholds configurados

| Métrica | Threshold padrão | Variável de env |
|---------|-----------------|-----------------|
| Taxa de erro (`errPct`) | 2% | `ERR_PCT_THRESHOLD` |
| Latência P95 (`p95Ms`) | 2000 ms | `P95_THRESHOLD_MS` |
| Janela de lookback | 5 min | `WINDOW_MINUTES` |

A janela de 5 minutos é um equilíbrio entre: capturar erros reais introduzidos
pelo deploy (que aparecem logo nas primeiras requisições após o cold start) e
evitar timeout excessivo no pipeline. Em produção com tráfego constante, 5 min
é suficiente para uma amostra estatisticamente relevante.

## Credenciais necessárias

- `NR_USER_API_KEY` — **User key** do New Relic (NerdGraph). Criada em
  [one.newrelic.com → API keys → Create a key → tipo User](https://one.newrelic.com/api-keys).
  **Não é a License key** (`NR_LICENSE_KEY`) usada para ingest de telemetria
  — as duas têm escopos completamente diferentes; misturá-las é um erro comum.
- `NR_ACCOUNT_ID` — ID numérico da conta New Relic (visível em
  one.newrelic.com → Account settings).

Ambas devem ser configuradas como secrets no GitHub
(Settings → Secrets and variables → Actions) e no environment `production`.

## Deployment marker

Além do gate, cada deploy bem-sucedido cria um **deployment marker** no New
Relic via a mutation NerdGraph `changeTrackingCreateDeployment`. Isso gera uma
linha vertical nos dashboards de APM, permitindo correlacionar visualmente
qualquer variação de métrica com o momento exato do deploy.

O marker carrega:
- `version`: SHA do commit (`github.sha`)
- `user`: ator do Actions que disparou o workflow
- `deepLink`: URL do run do Actions
- `entityGuid`: GUID da entidade APM da API no New Relic

O `entityGuid` da entidade APM é necessário para o marker. Ele é obtido em:
one.newrelic.com → APM → `labviromol-api` → Settings → Application → Entity
GUID. Configure como secret `NEW_RELIC_ENTITY_GUID`.

## O que fazer quando o gate dispara

### 1. Avaliar se é sinal real ou ruído

Antes de agir, verifique:

- **Tráfego real?** Se o deploy ocorreu fora do horário de pico e a janela
  de 5 min teve zero ou poucas requisições, `errPct` pode ser 100% a partir
  de 1 erro em 1 requisição. Verifique o volume total em:
  one.newrelic.com → APM → `labviromol-api` → Transactions.

- **Regressão real ou problema de infra?** Verifique se o erro é HTTP 5xx
  na API (regressão de código) ou falha de dependência (banco, LibreTranslate,
  Brevo). O trace ID nos logs do Actions e no New Relic permite pivotar.

- **Threshold adequado?** Se o tráfego de produção tiver crescido e 2% de
  erros representar um volume aceitável de falhas temporárias (ex.: timeouts
  de LibreTranslate que são retriados), ajuste `ERR_PCT_THRESHOLD` conforme
  a seção de ajuste abaixo.

### 2. Fix-forward (caminho principal)

A política padrão do projeto é fix-forward. Se o gate
disparou por regressão real:

1. Identifique o erro nos logs/traces do New Relic (use o deployment marker
   para filtrar a janela pós-deploy).
2. Faça um commit de fix na branch e abra um PR.
3. O próximo deploy vai rodar o gate novamente.

Enquanto o gate está disparado, a API da versão com problema **continua
servindo** (o job falhou mas a API foi subida — o gate é uma verificação
pós-fato, não um rollback automático).

### 3. Rollback manual de imagem (somente sem migração nova)

Se o deploy não trouxe migração de schema e a regressão é crítica:

```bash
# Na droplet
cd ~/labviromol-deploy
API_TAG=<sha-anterior> docker compose -f docker-compose.prod.yaml \
  --env-file .env up -d api
```

Ver `docs/runbooks/deploy.md` para o procedimento completo. Se houve migração
nova neste deploy, rollback de imagem **quebraria compatibilidade** — use
fix-forward nesse caso.

### 4. Silenciar o gate temporariamente (último recurso)

Se necessário silenciar o gate para um deploy emergencial:

- No workflow `cd.yml`, o step `New Relic deployment marker` e o step
  `Release gate (NRQL)` têm `if: steps.health.outcome == 'success'`. Para
  pular apenas o gate, adicione temporariamente `if: false` ao step
  `Release gate (NRQL)` via commit direto.
- Documente o motivo e reverta no commit seguinte.

## Ajustar thresholds

Os thresholds são configurados via variáveis de ambiente no step
`Release gate (NRQL)` de `.github/workflows/cd.yml`:

```yaml
env:
  NR_USER_API_KEY: ${{ secrets.NR_USER_API_KEY }}
  NR_ACCOUNT_ID: ${{ secrets.NR_ACCOUNT_ID }}
  NR_APP_NAME: labviromol-api
  WINDOW_MINUTES: "5"
  ERR_PCT_THRESHOLD: "2"
  P95_THRESHOLD_MS: "2000"
```

Edite os valores diretamente no workflow. As variáveis são documentadas no
header de `scripts/deploy/nr-release-gate.sh`.

## Pré-requisitos de instrumentação

O gate só tem utilidade se a API estiver de fato exportando traces para o
New Relic. Confirmar:

- `NR_LICENSE_KEY` configurada nos secrets de produção e injetada via
  `secrets/prod.enc.env` (plano 23).
- `OTEL_EXPORTER_OTLP_ENDPOINT` e `OTEL_EXPORTER_OTLP_HEADERS` configurados
  no `appsettings.json` ou via env vars (plano de observabilidade OTel do
  `Shared.Infrastructure`).
- Serviço aparecendo como `labviromol-api` em one.newrelic.com → APM.

Se a API não estiver exportando, o gate retorna "sem dados" e passa (exit 0)
com um aviso — não falha o deploy por falta de dados. Isso é intencional:
um deploy em ambiente sem tráfego real (ex.: staging sem instrumentação
ativa) não deve ser bloqueado pelo gate.

## Canal de alerta

Quando o gate falha, o job do GitHub Actions emite uma anotação de erro
(`::error`) visível no check do PR/push. Para alertas externos (Slack, PagerDuty,
e-mail), configure um workflow de notificação no evento
`workflow_run → cd → failure` ou use a New Relic alert condition
"Deployment marker followed by error rate spike" no New Relic Alerts.
