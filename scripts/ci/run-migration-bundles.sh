#!/bin/sh

set -e

if [ -z "${DB_CONNECTION_STRING:-}" ]; then
  echo "ERRO: variável de ambiente DB_CONNECTION_STRING não definida." >&2
  exit 1
fi

BUNDLES="efbundle-identity efbundle-inventory efbundle-assets efbundle-research efbundle-scheduling efbundle-notify"

cd "$(dirname "$0")"

for bundle in ${BUNDLES}; do
  if [ ! -x "./${bundle}" ]; then
    echo "ERRO: bundle não encontrado ou sem permissão de execução: ${bundle}" >&2
    exit 1
  fi

  echo "== Aplicando migrações: ${bundle} =="
  ./"${bundle}" --connection "${DB_CONNECTION_STRING}"
  status=$?

  if [ "${status}" -ne 0 ]; then
    echo "ERRO: ${bundle} falhou com exit code ${status}. Abortando." >&2
    exit "${status}"
  fi

  echo "OK: ${bundle} aplicado com sucesso."
  echo
done

echo "== Todas as migrações foram aplicadas com sucesso =="
exit 0
