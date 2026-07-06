# Métricas DORA — LabViroMol

[English](./dora.md) · **Português**

Documentação das **4 métricas DORA** (DevOps Research and Assessment) implementadas
para este repositório. O objetivo é acompanhar a **evolução do processo de entrega de
software** ao longo do tempo — o valor está na tendência, não no número absoluto de
qualquer semana isolada.

---

## As 4 métricas

### 1. Deployment Frequency (Frequência de Deploy)

**O que mede:** quantas vezes por dia a equipe faz deploy em produção, em média.

**Como é calculada:** número de GitHub Deployments com `state: success` no
environment `production`, dividido pelo número de dias na janela de análise (padrão: 30).

**Fonte de dados:** GitHub Deployments criados pelo job `deploy` do workflow
`cd.yml` via `actions/github-script@v7` (step "Register GitHub Deployment").

**Classificação DORA 2023:**
| Nível | Frequência |
|-------|-----------|
| Elite | Várias vezes por dia (≥ 1/dia) |
| Alto | Entre uma vez por semana e uma vez por dia |
| Médio | Entre uma vez por mês e uma vez por semana |
| Baixo | Menos de uma vez por mês |

---

### 2. Lead Time for Changes (Tempo do Primeiro Commit ao Deploy)

**O que mede:** quanto tempo leva desde o primeiro commit de um PR até esse código
ser deployado em produção, em mediana.

**Como é calculada:**
1. Para cada PR mergeado na janela, busca o timestamp do primeiro commit do PR.
2. Encontra o deploy de sucesso (`success`) mais próximo **após** o merge do PR.
3. Calcula `delta = timestamp_deploy − timestamp_primeiro_commit` em horas.
4. Retorna a **mediana** de todos os deltas.

**Fonte de dados:** GitHub Pulls API (commits de cada PR) + GitHub Deployments.

**Por que mediana e não média:** a mediana é mais robusta a outliers (ex.: PRs
gigantes que ficaram semanas em review, ou re-deploys de hotfixes em minutos). A
tendência central da mediana reflete o processo normal de entrega.

**Classificação DORA 2023:**
| Nível | Lead time |
|-------|-----------|
| Elite | < 1 hora |
| Alto | Entre 1 hora e 1 dia |
| Médio | Entre 1 dia e 1 semana |
| Baixo | > 1 semana |

---

### 3. Change Failure Rate (Taxa de Falha em Mudanças)

**O que mede:** percentual de deploys que resultam em falha, degradação ou necessidade
de hotfix/rollback.

**Como é calculada:**
```
CFR = (deploys_failure + PRs_com_label_hotfix_ou_rollback) / total_deploys × 100
```

- `deploys_failure`: GitHub Deployments com `state: failure` ou `error`.
- `PRs com label hotfix/rollback`: PRs mergeados na janela com label `hotfix` ou
  `rollback` (proxy para "o deploy anterior quebrou algo").
- `total_deploys`: success + failure.

**Fonte de dados:** GitHub Deployments + GitHub Pulls API (labels).

**Classificação DORA 2023:**
| Nível | CFR |
|-------|-----|
| Elite | 0 – 5% |
| Alto | 5 – 10% |
| Médio | 10 – 15% |
| Baixo | > 15% |

---

### 4. MTTR — Mean Time To Recovery (Tempo Médio de Recuperação)

**O que mede:** quanto tempo leva para restaurar o serviço após um deploy com falha.
O nome é "mean" por convenção histórica, mas aqui usamos a **mediana** (mais robusta).

**Como é calculada:**
1. Para cada deploy com `state: failure/error`, localiza o próximo deploy com
   `state: success` (deploy de recuperação).
2. Calcula `delta = timestamp_recovery − timestamp_failure` em horas.
3. Retorna a **mediana** de todos os deltas.

**Fonte de dados:** GitHub Deployments.

**Nota:** se não houve nenhum deploy com falha na janela, MTTR = N/A (bom sinal).

**Classificação DORA 2023:**
| Nível | MTTR |
|-------|------|
| Elite | < 1 hora |
| Alto | < 1 dia |
| Médio | Entre 1 dia e 1 semana |
| Baixo | > 1 semana |

---

## Janela de análise

O padrão é **30 dias** corridos. Pode ser ajustado via `workflow_dispatch` no
workflow `dora.yml` (parâmetro `window_days`).

**Por que 30 dias:** longo o suficiente para suavizar semanas atípicas (feriados,
sprints de estabilização) sem incluir histórico antigo que não reflete o processo
atual.

---

## Como ler os resultados

1. **N/A não é alarme.** No início do projeto, com poucos deploys ou poucos PRs
   na janela, algumas métricas aparecem como "N/A" por falta de dados. Isso é
   esperado — as métricas ganham significado à medida que o histórico cresce.

2. **Tendência importa mais que nível absoluto.** Um projeto que evolui de
   "Médio" para "Alto" ao longo de 3 meses está fazendo progresso real, mesmo que
   ainda não seja "Elite".

3. **Deployment Frequency baixa não é necessariamente ruim.** Para um sistema
   de gestão de laboratório (não e-commerce), 2–3 deploys/semana pode ser
   adequado. O DORA define "Elite" para sistemas que podem — e devem — deployar
   múltiplas vezes por dia; use como referência, não como meta absoluta.

4. **CFR e MTTR são os mais críticos.** Uma frequência alta com CFR alta significa
   que cada deploy é arriscado. Priorizar reduzir CFR antes de aumentar frequência.

---

## Workflow de coleta

| Arquivo | Papel |
|---------|-------|
| `.github/workflows/dora.yml` | Workflow agendado (toda segunda 08:00 UTC) + `workflow_dispatch` |
| `scripts/dora/compute-dora.sh` | Script shell que consome a GitHub API e calcula as 4 métricas |

**Permissões necessárias** (já configuradas no workflow):
- `deployments: read` — listar GitHub Deployments e seus statuses
- `pull-requests: read` — listar PRs e seus commits para lead time
- `contents: read` — checkout do repo para executar o script

**Autenticação:** `GITHUB_TOKEN` (sem PAT adicional). Todas as APIs usadas
(`deployments`, `pulls`, `commits`) são por-repo e o `GITHUB_TOKEN` tem acesso
suficiente. Para visão **agregada** dos 3 repositórios (LabViroMol + admin-panel +
institucional), seria necessário um PAT com escopo `repo:read` nos 3 repos —
documentado como evolução futura; a implementação atual mede cada repo de forma
independente.

---

## Instrumentação: onde os Deployments são criados

O job `deploy` de `.github/workflows/cd.yml` (step "Register GitHub Deployment")
cria um GitHub Deployment + DeploymentStatus após cada tentativa de deploy, com:

- `environment: production`
- `ref: <github.sha>` do commit deployado
- `state: success` se `/health/ready` retornou 200; `failure` caso contrário
- `description`: mensagem humana descrevendo o resultado

Essa é a fonte primária de todas as 4 métricas acima.

---

## Evolução futura

- **Visão multi-repo:** configurar um PAT com `repo` nos 3 repositórios e rodar o
  script uma vez por repo, consolidando num único summary. Requer secret adicional
  (`DORA_AGGREGATION_PAT`) e coordenação entre os repositórios.
- **Histórico persistido:** commitar o summary semanal em `docs/dora-history.jsonl`
  (uma linha JSON por semana) para grafar a evolução. Depende de ajustar a entrada
  `**/docs/` no `.gitignore`, que hoje ignoraria esse arquivo.
- **Alertas de regressão:** acrescentar um step no `dora.yml` que falha se CFR > 20%
  ou MTTR > 24h na semana, emitindo uma annotation no summary e notificando via
  `SLACK_WEBHOOK` (se configurado).

### Última execução automática

*Ainda não houve execução — o workflow roda toda segunda-feira às 08:00 UTC.*
