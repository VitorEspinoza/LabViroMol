#!/usr/bin/env bash
set -euo pipefail

TARGET="${1:?Uso: scripts/deploy/sync-config.sh usuario@host-ssh [pasta_remota]}"
REMOTE_DIR="${2:-labviromol-deploy}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

SSH=(ssh "$TARGET")

COMPOSE_FILE="docker-compose.prod.yaml"
NGINX_CONF="nginx/gateway.conf"
CLOUDFLARE_INI="certbot/cloudflare.ini"

for f in "$COMPOSE_FILE" "$NGINX_CONF"; do
  if [ ! -f "$f" ]; then
    echo "ERRO: '$f' nao encontrado em ${REPO_ROOT} - abortando (nada foi enviado)." >&2
    exit 2
  fi
done

echo "==> Garantindo pastas remotas ${REMOTE_DIR}/nginx e ${REMOTE_DIR}/certbot"
"${SSH[@]}" "mkdir -p ${REMOTE_DIR}/nginx ${REMOTE_DIR}/certbot"

sync_file() {
  local local_path="$1"
  local remote_path="$2"

  if command -v rsync >/dev/null 2>&1; then
    rsync -az --checksum -e ssh "$local_path" "${TARGET}:${remote_path}"
  else
    scp "$local_path" "${TARGET}:${remote_path}"
  fi
}

echo "==> Enviando ${COMPOSE_FILE}"
sync_file "$COMPOSE_FILE" "${REMOTE_DIR}/docker-compose.prod.yaml"

echo "==> Enviando ${NGINX_CONF}"
sync_file "$NGINX_CONF" "${REMOTE_DIR}/nginx/gateway.conf"

if [ -f "$CLOUDFLARE_INI" ]; then
  echo "==> Enviando ${CLOUDFLARE_INI}"
  sync_file "$CLOUDFLARE_INI" "${REMOTE_DIR}/certbot/cloudflare.ini"
  "${SSH[@]}" "chmod 600 ${REMOTE_DIR}/certbot/cloudflare.ini"
else
  echo "    ${CLOUDFLARE_INI} nao existe localmente - pulando."
  echo "    Precisa existir na droplet (gerenciado fora deste repo/script,"
  echo "    contem o token da API Cloudflare) antes do certbot funcionar."
fi

echo "==> .env do servidor: intocado (gerenciado via SOPS - este"
echo "    script nunca envia nem sobrescreve ${REMOTE_DIR}/.env)"

echo "==> Sincronizacao de config concluida em ${TARGET}:${REMOTE_DIR}"
