#!/usr/bin/env bash

set -euo pipefail

BASE_REF="${1:-}"
HEAD_REF="${2:-HEAD}"

if [[ -z "$BASE_REF" ]]; then
  echo "Uso: $0 <base-ref> [head-ref]" >&2
  exit 2
fi

MIGRATIONS_PATH_GLOB='src/Modules/*/Infrastructure/Persistence/Migrations/*.cs'

mapfile -t CHANGED_FILES < <(
  git diff --name-only --diff-filter=AM "${BASE_REF}...${HEAD_REF}" -- "$MIGRATIONS_PATH_GLOB" \
    | grep -v '\.Designer\.cs$' \
    | grep -v 'ModelSnapshot\.cs$' \
    || true
)

if [[ "${#CHANGED_FILES[@]}" -eq 0 ]]; then
  echo "Nenhuma migration nova/modificada sob Migrations/ neste PR. OK."
  exit 0
fi

echo "Migrations alteradas neste PR:"
printf '  - %s\n' "${CHANGED_FILES[@]}"
echo

DESTRUCTIVE_PATTERN='migrationBuilder\.(DropTable|DropColumn|Sql|DropForeignKey|DropPrimaryKey)\('

FOUND=0

for file in "${CHANGED_FILES[@]}"; do
  [[ -f "$file" ]] || continue

  up_block_with_lines="$(
    awk '
      /protected override void Up\(/ { in_up = 1 }
      /protected override void Down\(/ { in_up = 0 }
      in_up { print NR": "$0 }
    ' "$file"
  )"

  [[ -n "$up_block_with_lines" ]] || continue

  matches="$(echo "$up_block_with_lines" | grep -E "$DESTRUCTIVE_PATTERN" || true)"

  if [[ -n "$matches" ]]; then
    FOUND=1
    echo "DESTRUTIVO em $file (dentro de Up()):"
    while IFS= read -r line; do
      echo "    $line"
    done <<< "$matches"
    echo
  fi
done

if [[ "$FOUND" -eq 1 ]]; then
  echo "----------------------------------------------------------------------"
  echo "Este PR contém operação(ões) destrutiva(s) em migration(ões) EF Core"
  echo "(DropTable/DropColumn/Sql bruto/DropForeignKey/DropPrimaryKey)."
  echo
  echo "Para liberar o merge:"
  echo "  1. Adicione a label 'migration-reviewed' ao PR."
  echo "  2. Obtenha aprovação de um CODEOWNER do caminho de Migrations."
  echo "----------------------------------------------------------------------"
  exit 1
fi

echo "Nenhuma operação destrutiva encontrada no bloco Up() das migrations alteradas. OK."
exit 0
