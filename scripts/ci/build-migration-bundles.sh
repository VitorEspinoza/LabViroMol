#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

OUTPUT_DIR="${1:-${REPO_ROOT}/artifacts/migrate}"
RUNTIME_ID="${2:-linux-x64}"
STARTUP_PROJECT="${REPO_ROOT}/src/LabViroMol.Api/LabViroMol.Api.csproj"
CONFIGURATION="Release"

declare -A MODULE_PROJECT=(
  [identity]="${REPO_ROOT}/src/Modules/Identity/Infrastructure/LabViroMol.Modules.Identity.Infrastructure.csproj"
  [inventory]="${REPO_ROOT}/src/Modules/Inventory/Infrastructure/LabViroMol.Modules.Inventory.Infrastructure.csproj"
  [assets]="${REPO_ROOT}/src/Modules/Assets/Infrastructure/LabViroMol.Modules.Assets.Infrastructure.csproj"
  [research]="${REPO_ROOT}/src/Modules/Research/Infrastructure/LabViroMol.Modules.Research.Infrastructure.csproj"
  [scheduling]="${REPO_ROOT}/src/Modules/Scheduling/Infrastructure/LabViroMol.Modules.Scheduling.Infrastructure.csproj"
  [notify]="${REPO_ROOT}/src/Modules/Notify/Infrastructure/LabViroMol.Modules.Notify.Infrastructure.csproj"
)

declare -A MODULE_CONTEXT=(
  [identity]="LabViroMolIdentityDbContext"
  [inventory]="InventoryDbContext"
  [assets]="AssetsDbContext"
  [research]="ResearchDbContext"
  [scheduling]="SchedulingDbContext"
  [notify]="NotifyDbContext"
)

MODULE_ORDER=(identity inventory assets research scheduling notify)

mkdir -p "${OUTPUT_DIR}"

# Microsoft.Extensions.ApiDescription.Server injects a **/*.resx EmbeddedResource glob during
# dotnet publish (called internally by EF bundle). When no .resx files exist the build fails
# with MSB3552. Disabling OpenAPI doc generation for this publish avoids the error entirely.
export MSBUILDADDITIONALCOMMANDLINEARGS="/p:OpenApiGenerateDocuments=false"

echo "== Gerando EF Core migration bundles =="
echo "Output dir : ${OUTPUT_DIR}"
echo "Runtime    : ${RUNTIME_ID}"
echo "Startup    : ${STARTUP_PROJECT}"
echo

if ! command -v dotnet-ef >/dev/null 2>&1 && ! dotnet ef --version >/dev/null 2>&1; then
  echo "ERRO: ferramenta 'dotnet-ef' não encontrada. Instale com:" >&2
  echo "  dotnet tool install --global dotnet-ef" >&2
  exit 1
fi

for module in "${MODULE_ORDER[@]}"; do
  project="${MODULE_PROJECT[${module}]}"
  context="${MODULE_CONTEXT[${module}]}"
  bundle_name="efbundle-${module}"
  bundle_path="${OUTPUT_DIR}/${bundle_name}"

  echo "---- Módulo: ${module} (context: ${context}) ----"

  if [ ! -f "${project}" ]; then
    echo "ERRO: projeto não encontrado: ${project}" >&2
    exit 1
  fi

  dotnet ef migrations bundle \
    --project "${project}" \
    --startup-project "${STARTUP_PROJECT}" \
    --context "${context}" \
    --configuration "${CONFIGURATION}" \
    --output "${bundle_path}" \
    --force --verbose

  chmod +x "${bundle_path}" || true

  echo "OK -> ${bundle_path}"
  echo
done

echo "== Todos os bundles foram gerados em ${OUTPUT_DIR} =="
ls -la "${OUTPUT_DIR}"
