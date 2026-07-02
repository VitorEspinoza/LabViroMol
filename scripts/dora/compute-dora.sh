#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "Uso: $0 <owner> <repo> [<janela_dias>]" >&2
  exit 2
fi

OWNER="$1"
REPO="$2"
WINDOW_DAYS="${3:-30}"

if date --version >/dev/null 2>&1; then
  SINCE=$(date -u --date="${WINDOW_DAYS} days ago" '+%Y-%m-%dT%H:%M:%SZ')
else
  SINCE=$(date -u -v"-${WINDOW_DAYS}d" '+%Y-%m-%dT%H:%M:%SZ')
fi

echo "==> Janela de análise: últimos ${WINDOW_DAYS} dias (desde ${SINCE})" >&2

echo "==> Coletando deployments do environment 'production'..." >&2

ALL_DEPLOYMENTS="[]"
PAGE=1
while true; do
  PAGE_DATA=$(gh api \
    "repos/${OWNER}/${REPO}/deployments?environment=production&per_page=100&page=${PAGE}" \
    --jq "[.[] | select(.created_at >= \"${SINCE}\")]" 2>/dev/null || echo "[]")

  COUNT=$(echo "${PAGE_DATA}" | jq 'length')
  if [[ "${COUNT}" -eq 0 ]]; then
    break
  fi

  ALL_DEPLOYMENTS=$(echo "${ALL_DEPLOYMENTS} ${PAGE_DATA}" | jq -s 'add')
  PAGE=$((PAGE + 1))
  if [[ "${PAGE}" -gt 10 ]]; then
    break
  fi
done

TOTAL_DEPLOYMENTS=$(echo "${ALL_DEPLOYMENTS}" | jq 'length')
echo "==> Total de deployments coletados: ${TOTAL_DEPLOYMENTS}" >&2

echo "==> Coletando status dos deployments..." >&2

DEPLOY_IDS=$(echo "${ALL_DEPLOYMENTS}" | jq -r '.[].id')

SUCCESSES=0
FAILURES=0
SUCCESS_TIMESTAMPS="[]"
FAILURE_TIMESTAMPS="[]"

while IFS= read -r DID; do
  [[ -z "${DID}" ]] && continue

  STATUS_DATA=$(gh api "repos/${OWNER}/${REPO}/deployments/${DID}/statuses?per_page=1" \
    --jq '.[0] // {}' 2>/dev/null || echo "{}")

  STATE=$(echo "${STATUS_DATA}" | jq -r '.state // "unknown"')
  CREATED_AT=$(echo "${ALL_DEPLOYMENTS}" | jq -r --argjson id "${DID}" \
    '.[] | select(.id == $id) | .created_at')

  if [[ "${STATE}" == "success" ]]; then
    SUCCESSES=$((SUCCESSES + 1))
    SUCCESS_TIMESTAMPS=$(echo "${SUCCESS_TIMESTAMPS}" | \
      jq --arg ts "${CREATED_AT}" '. + [$ts]')
  elif [[ "${STATE}" == "failure" || "${STATE}" == "error" ]]; then
    FAILURES=$((FAILURES + 1))
    FAILURE_TIMESTAMPS=$(echo "${FAILURE_TIMESTAMPS}" | \
      jq --arg ts "${CREATED_AT}" '. + [$ts]')
  fi
done <<< "${DEPLOY_IDS}"

if [[ "${WINDOW_DAYS}" -gt 0 ]]; then
  DEPLOY_FREQ=$(echo "${SUCCESSES} ${WINDOW_DAYS}" | \
    awk '{printf "%.2f", $1 / $2}')
else
  DEPLOY_FREQ="N/A"
fi

echo "==> Deployment frequency: ${DEPLOY_FREQ} deploys/dia (${SUCCESSES} successes em ${WINDOW_DAYS} dias)" >&2

echo "==> Coletando PRs mergeados para lead time..." >&2

MERGED_PRS=$(gh api \
  "repos/${OWNER}/${REPO}/pulls?state=closed&per_page=100" \
  --jq "[.[] | select(.merged_at != null and .merged_at >= \"${SINCE}\")]" \
  2>/dev/null || echo "[]")

PR_COUNT=$(echo "${MERGED_PRS}" | jq 'length')
echo "==> PRs mergeados na janela: ${PR_COUNT}" >&2

LEAD_TIMES_HOURS="[]"

if [[ "${PR_COUNT}" -gt 0 && "${SUCCESSES}" -gt 0 ]]; then
  while IFS= read -r PR_NUMBER; do
    [[ -z "${PR_NUMBER}" ]] && continue

    MERGED_AT=$(echo "${MERGED_PRS}" | \
      jq -r --argjson n "${PR_NUMBER}" '.[] | select(.number == $n) | .merged_at')

    COMMITS_DATA=$(gh api \
      "repos/${OWNER}/${REPO}/pulls/${PR_NUMBER}/commits?per_page=100" \
      --jq '[.[].commit.author.date] | sort | .[0]' \
      2>/dev/null || echo "null")

    FIRST_COMMIT_AT="${COMMITS_DATA}"
    if [[ "${FIRST_COMMIT_AT}" == "null" || -z "${FIRST_COMMIT_AT}" ]]; then
      FIRST_COMMIT_AT="${MERGED_AT}"
    fi

    NEXT_DEPLOY_AT=$(echo "${SUCCESS_TIMESTAMPS}" | \
      jq -r --arg merged "${MERGED_AT}" \
      '[.[] | select(. >= $merged)] | sort | .[0] // empty')

    if [[ -z "${NEXT_DEPLOY_AT}" ]]; then
      continue
    fi

    DELTA_HOURS=$(python3 - <<EOF 2>/dev/null || echo ""
from datetime import datetime, timezone
fmt = "%Y-%m-%dT%H:%M:%SZ"
t1 = datetime.strptime("${FIRST_COMMIT_AT}", fmt).replace(tzinfo=timezone.utc)
t2 = datetime.strptime("${NEXT_DEPLOY_AT}", fmt).replace(tzinfo=timezone.utc)
delta = (t2 - t1).total_seconds() / 3600
print(f"{delta:.2f}")
EOF
)

    if [[ -n "${DELTA_HOURS}" ]]; then
      LEAD_TIMES_HOURS=$(echo "${LEAD_TIMES_HOURS}" | \
        jq --argjson h "${DELTA_HOURS}" '. + [$h]')
    fi

  done < <(echo "${MERGED_PRS}" | jq -r '.[].number')
fi

LEAD_TIME_MEDIAN=$(echo "${LEAD_TIMES_HOURS}" | jq -r '
  if length == 0 then "N/A"
  else
    (sort) as $sorted |
    (length) as $n |
    if ($n % 2) == 1
    then $sorted[($n / 2 | floor)] | tostring
    else (($sorted[$n/2 - 1] + $sorted[$n/2]) / 2) | tostring
    end
  end
')

echo "==> Lead time (mediana): ${LEAD_TIME_MEDIAN} horas" >&2

HOTFIX_PRS=$(gh api \
  "repos/${OWNER}/${REPO}/pulls?state=closed&labels=hotfix&per_page=100" \
  --jq "[.[] | select(.merged_at != null and .merged_at >= \"${SINCE}\")] | length" \
  2>/dev/null || echo "0")

ROLLBACK_PRS=$(gh api \
  "repos/${OWNER}/${REPO}/pulls?state=closed&labels=rollback&per_page=100" \
  --jq "[.[] | select(.merged_at != null and .merged_at >= \"${SINCE}\")] | length" \
  2>/dev/null || echo "0")

HOTFIX_COUNT=$((HOTFIX_PRS + ROLLBACK_PRS))

TOTAL_FOR_CFR=$((SUCCESSES + FAILURES))
CFR_NUMERATOR=$((FAILURES + HOTFIX_COUNT))

if [[ "${TOTAL_FOR_CFR}" -gt 0 ]]; then
  CFR=$(echo "${CFR_NUMERATOR} ${TOTAL_FOR_CFR}" | \
    awk '{printf "%.1f", ($1 / $2) * 100}')
else
  CFR="N/A"
fi

echo "==> CFR: ${CFR}% (${CFR_NUMERATOR} falhas + hotfixes em ${TOTAL_FOR_CFR} deploys)" >&2

MTTR_HOURS="[]"

if [[ "${FAILURES}" -gt 0 && "${SUCCESSES}" -gt 0 ]]; then
  FAILURE_SORTED=$(echo "${FAILURE_TIMESTAMPS}" | jq -r 'sort | .[]')
  SUCCESS_SORTED=$(echo "${SUCCESS_TIMESTAMPS}" | jq -r 'sort | .[]')

  while IFS= read -r FAIL_TS; do
    [[ -z "${FAIL_TS}" ]] && continue

    RECOVERY_TS=$(echo "${SUCCESS_TIMESTAMPS}" | \
      jq -r --arg ft "${FAIL_TS}" \
      '[.[] | select(. > $ft)] | sort | .[0] // empty')

    if [[ -z "${RECOVERY_TS}" ]]; then
      continue
    fi

    DELTA_H=$(python3 - <<EOF 2>/dev/null || echo ""
from datetime import datetime, timezone
fmt = "%Y-%m-%dT%H:%M:%SZ"
t1 = datetime.strptime("${FAIL_TS}", fmt).replace(tzinfo=timezone.utc)
t2 = datetime.strptime("${RECOVERY_TS}", fmt).replace(tzinfo=timezone.utc)
print(f"{(t2 - t1).total_seconds() / 3600:.2f}")
EOF
)

    if [[ -n "${DELTA_H}" ]]; then
      MTTR_HOURS=$(echo "${MTTR_HOURS}" | jq --argjson h "${DELTA_H}" '. + [$h]')
    fi

  done <<< "${FAILURE_SORTED}"
fi

MTTR_MEDIAN=$(echo "${MTTR_HOURS}" | jq -r '
  if length == 0 then "N/A"
  else
    (sort) as $sorted |
    (length) as $n |
    if ($n % 2) == 1
    then $sorted[($n / 2 | floor)] | tostring
    else (($sorted[$n/2 - 1] + $sorted[$n/2]) / 2) | tostring
    end
  end
')

echo "==> MTTR (mediana): ${MTTR_MEDIAN} horas" >&2

export DORA_DEPLOY_FREQ="${DEPLOY_FREQ}"
export DORA_LEAD_TIME_MEDIAN="${LEAD_TIME_MEDIAN}"
export DORA_CFR="${CFR}"
export DORA_MTTR="${MTTR_MEDIAN}"
export DORA_WINDOW_DAYS="${WINDOW_DAYS}"
export DORA_SUCCESSES="${SUCCESSES}"
export DORA_FAILURES="${FAILURES}"
export DORA_TOTAL="${TOTAL_FOR_CFR}"
export DORA_HOTFIX_COUNT="${HOTFIX_COUNT}"
export DORA_PR_COUNT="${PR_COUNT}"

if [[ -n "${GITHUB_ENV:-}" ]]; then
  {
    echo "DORA_DEPLOY_FREQ=${DEPLOY_FREQ}"
    echo "DORA_LEAD_TIME_MEDIAN=${LEAD_TIME_MEDIAN}"
    echo "DORA_CFR=${CFR}"
    echo "DORA_MTTR=${MTTR_MEDIAN}"
    echo "DORA_WINDOW_DAYS=${WINDOW_DAYS}"
    echo "DORA_SUCCESSES=${SUCCESSES}"
    echo "DORA_FAILURES=${FAILURES}"
    echo "DORA_TOTAL=${TOTAL_FOR_CFR}"
    echo "DORA_HOTFIX_COUNT=${HOTFIX_COUNT}"
    echo "DORA_PR_COUNT=${PR_COUNT}"
  } >> "${GITHUB_ENV}"
fi

REPORT_DATE=$(date -u '+%Y-%m-%d %H:%M UTC')

cat <<MARKDOWN

## Métricas DORA — ${OWNER}/${REPO}

> Janela: últimos **${WINDOW_DAYS} dias** (desde ${SINCE})
> Gerado em: ${REPORT_DATE}

### Resultados

| Métrica | Valor | Referência Elite (DORA 2023) |
|---------|-------|------------------------------|
| Deployment Frequency | **${DEPLOY_FREQ} deploys/dia** | ≥ 1 deploy/dia |
| Lead Time for Changes | **${LEAD_TIME_MEDIAN} h** | < 1 hora |
| Change Failure Rate | **${CFR}%** | 0 – 5% |
| MTTR | **${MTTR_MEDIAN} h** | < 1 hora |

### Dados brutos

| | Valor |
|---|---|
| Deploys com sucesso | ${SUCCESSES} |
| Deploys com falha | ${FAILURES} |
| Total de deploys | ${TOTAL_FOR_CFR} |
| PRs hotfix/rollback | ${HOTFIX_COUNT} |
| PRs mergeados (lead time) | ${PR_COUNT} |

> **Nota:** valores "N/A" indicam dados insuficientes na janela (nenhum deploy ou nenhum PR
> mergeado). Isso é esperado no início do projeto — as métricas ganham significado à medida
> que o histórico de deploys cresce. DORA mede **tendência**, não número absoluto.

MARKDOWN
