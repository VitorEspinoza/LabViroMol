#!/usr/bin/env bash
set -euo pipefail

: "${NR_APP_NAME:=labviromol-api}"
: "${WINDOW_MINUTES:=5}"
: "${ERR_PCT_THRESHOLD:=2}"
: "${P95_THRESHOLD_MS:=2000}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --window)   WINDOW_MINUTES="$2";       shift 2 ;;
    --err-pct)  ERR_PCT_THRESHOLD="$2";    shift 2 ;;
    --p95)      P95_THRESHOLD_MS="$2";     shift 2 ;;
    *)
      echo "Uso: $0 [--window <min>] [--err-pct <pct>] [--p95 <ms>]" >&2
      echo "Variáveis obrigatórias: NR_USER_API_KEY, NR_ACCOUNT_ID" >&2
      exit 2
      ;;
  esac
done

for cmd in curl jq; do
  if ! command -v "$cmd" &>/dev/null; then
    echo "ERRO: '$cmd' não encontrado no PATH." >&2
    exit 2
  fi
done

if [[ -z "${NR_USER_API_KEY:-}" ]]; then
  echo "ERRO: NR_USER_API_KEY não definida." >&2
  echo "  Deve ser uma User key do New Relic (não a License/Ingest key)." >&2
  exit 2
fi

if [[ -z "${NR_ACCOUNT_ID:-}" ]]; then
  echo "ERRO: NR_ACCOUNT_ID não definida." >&2
  exit 2
fi

NRQL="SELECT percentage(count(*), WHERE error IS true) AS errPct, \
percentile(duration * 1000, 95) AS p95Ms \
FROM Transaction \
WHERE appName = '${NR_APP_NAME}' \
SINCE ${WINDOW_MINUTES} minutes ago"

GRAPHQL_PAYLOAD=$(jq -n \
  --arg nrql   "$NRQL" \
  --argjson aid "$NR_ACCOUNT_ID" \
  '{
    query: "query($accountId: Int!, $nrql: Nrql!) { actor { account(id: $accountId) { nrql(query: $nrql) { results } } } }",
    variables: { accountId: $aid, nrql: $nrql }
  }')

echo "==> Release gate — consultando New Relic NerdGraph..."
echo "    App:     ${NR_APP_NAME}"
echo "    Janela:  últimos ${WINDOW_MINUTES} minutos"
echo "    Limites: errPct <= ${ERR_PCT_THRESHOLD}% | p95 <= ${P95_THRESHOLD_MS}ms"

HTTP_RESPONSE=$(curl -s -w "\n__HTTP_STATUS__%{http_code}" \
  -X POST "https://api.newrelic.com/graphql" \
  -H "Content-Type: application/json" \
  -H "API-Key: ${NR_USER_API_KEY}" \
  -d "$GRAPHQL_PAYLOAD")

HTTP_BODY=$(echo "$HTTP_RESPONSE" | sed '$d')
HTTP_STATUS=$(echo "$HTTP_RESPONSE" | tail -n1 | sed 's/__HTTP_STATUS__//')

if [[ "$HTTP_STATUS" != "200" ]]; then
  echo "ERRO: NerdGraph retornou HTTP ${HTTP_STATUS}." >&2
  echo "$HTTP_BODY" >&2
  exit 1
fi

GQL_ERRORS=$(echo "$HTTP_BODY" | jq -r '.errors // empty')
if [[ -n "$GQL_ERRORS" ]]; then
  echo "ERRO: NerdGraph retornou erros:" >&2
  echo "$GQL_ERRORS" >&2
  exit 1
fi

ERR_PCT=$(echo "$HTTP_BODY" | \
  jq -r '.data.actor.account.nrql.results[0].errPct // "null"')
P95_MS=$(echo "$HTTP_BODY" | \
  jq -r '.data.actor.account.nrql.results[0].p95Ms // "null"')

echo "    Resultado: errPct=${ERR_PCT}% | p95=${P95_MS}ms"

if [[ "$ERR_PCT" == "null" || "$P95_MS" == "null" ]]; then
  echo "AVISO: New Relic não retornou dados para '${NR_APP_NAME}' nos últimos"
  echo "       ${WINDOW_MINUTES} minutos. Isso pode significar:"
  echo "       (a) A app ainda não recebeu tráfego desde o deploy."
  echo "       (b) O serviço não está instrumentado ou exportando para New Relic."
  echo "       (c) O nome da app (NR_APP_NAME='${NR_APP_NAME}') está incorreto."
  echo ""
  echo "  O gate NÃO falha por ausência de dados — falha apenas por violação"
  echo "  de threshold. Verifique o New Relic manualmente se o deploy foi em"
  echo "  produção real com tráfego real."
  exit 0
fi

GATE_FAILED=0

if awk "BEGIN { exit !(${ERR_PCT} > ${ERR_PCT_THRESHOLD}) }"; then
  echo "FALHA: errPct=${ERR_PCT}% > threshold=${ERR_PCT_THRESHOLD}%" >&2
  GATE_FAILED=1
fi

if awk "BEGIN { exit !(${P95_MS} > ${P95_THRESHOLD_MS}) }"; then
  echo "FALHA: p95=${P95_MS}ms > threshold=${P95_THRESHOLD_MS}ms" >&2
  GATE_FAILED=1
fi

if [[ "$GATE_FAILED" -eq 1 ]]; then
  echo "" >&2
  echo "==> Release gate FALHOU. Opções:" >&2
  echo "    1. Fix-forward: faça um novo commit corrigindo o problema e re-deploje." >&2
  echo "    2. Rollback manual de imagem (somente se não houve migração de schema" >&2
  echo "       neste deploy): ver docs/runbooks/deploy.md." >&2
  echo "    3. Ajustar thresholds se os limites estiverem incorretos para o" >&2
  echo "       volume de tráfego atual: ver docs/runbooks/release-gate.md." >&2
  exit 1
fi

echo "==> Release gate PASSOU. Métricas dentro dos thresholds."
exit 0
