# Database migrations — production flow (EF Core migrations bundle)

**English** · [Português](./migrations.pt-BR.md)

## Overview

LabViroMol has **6 DbContexts** (one per business module), each with its own EF Core migration history, in separate Postgres schemas:

| Module | DbContext | Project |
|---|---|---|
| Identity | `LabViroMolIdentityDbContext` | `src/Modules/Identity/Infrastructure` |
| Inventory | `InventoryDbContext` | `src/Modules/Inventory/Infrastructure` |
| Assets | `AssetsDbContext` | `src/Modules/Assets/Infrastructure` |
| Research | `ResearchDbContext` | `src/Modules/Research/Infrastructure` |
| Scheduling | `SchedulingDbContext` | `src/Modules/Scheduling/Infrastructure` |
| Notify | `NotifyDbContext` | `src/Modules/Notify/Infrastructure` |

Instead of installing the .NET SDK plus the `dotnet-ef` tool in the container that runs in production (the old approach — heavy and ambiguous with multiple DbContexts), we use an **EF Core migrations bundle**: a *self-contained* executable per DbContext, generated at build time, that applies that module's migrations without needing the SDK at runtime.

## Generating the bundles locally

Prerequisites: .NET SDK 10 and the `dotnet-ef` tool installed (`dotnet tool install --global dotnet-ef`).

```bash
bash scripts/ci/build-migration-bundles.sh [output_dir] [runtime_identifier]
# default: output_dir=./artifacts/migrate, runtime_identifier=linux-x64
```

This generates 6 self-contained executables (`efbundle-identity`, `efbundle-inventory`, `efbundle-assets`, `efbundle-research`, `efbundle-scheduling`, `efbundle-notify`) in `<output_dir>`.

## Docker image (`Dockerfile.migrate`)

Multi-stage:
- **`build` stage** (`mcr.microsoft.com/dotnet/sdk:10.0`): restores the solution, installs `dotnet-ef`, runs `build-migration-bundles.sh`.
- **`final` stage** (`mcr.microsoft.com/dotnet/runtime-deps:10.0`): copies only the 6 executables plus the run script. **No SDK, no source code, no `dotnet-ef`** in the final image.

```bash
docker build -f Dockerfile.migrate -t labviromol-migrate:local .
docker run --rm -e DB_CONNECTION_STRING="Host=...;Port=5432;Database=...;Username=...;Password=..." labviromol-migrate:local
```

## Execution order

The bundles run **sequentially**, always in this order:

```
identity → inventory → assets → research → scheduling → notify
```

Identity runs first because the other modules reference users/roles by integration convention (even without a physical FK between schemas). This order is encoded in two places that must stay in sync:
- `scripts/ci/build-migration-bundles.sh` (`MODULE_ORDER` array, used only for generation — the generation order doesn't technically matter, but it's kept the same as the execution order for clarity)
- `scripts/ci/run-migration-bundles.sh` (`BUNDLES` variable, used during actual execution — **this order matters**)

## Environment variable contract

The entrypoint (`run-migration-bundles.sh`) expects the full Npgsql connection string in the environment variable **`DB_CONNECTION_STRING`** (raw value, passed as `--connection` to each bundle — **not** the `ConnectionStrings__LabViroMol` format used by the API via `IConfiguration`, since the bundle doesn't go through ASP.NET's configuration binding — it's a direct CLI call).

Any compose service that runs this image (`migrate`) needs to set `DB_CONNECTION_STRING` directly — **not** `ConnectionStrings__LabViroMol`.

## Exit code gate (critical for deploy)

`run-migration-bundles.sh` **stops at the first bundle that fails** and propagates its exit code to the parent process. This is what allows the deploy pipeline to detect a migration failure and abort the API rollout **before** switching the version in production — the old API keeps serving if the migration fails.

## Migration rule: backward-compatible (expand/contract)

Since there is no automatic image rollback once a migration has already altered the schema, every new migration must be **additive/backward-compatible**: the *previous* version of the API needs to keep working while the new migration has already been applied (the deploy window). In practice:

- Adding a column: always nullable or with a default, never `NOT NULL` without a default on an existing column.
- Removing a column/table: only after a previous deploy has already stopped using it (*expand* first, *contract* later, in separate deploys).
- Renaming: treat it as "add the new one + stop using the old one + remove the old one later" — never an atomic rename that breaks the previous version.

If a migration violates this and the new API fails its post-deploy health check, the policy is **fix-forward** (fix and redeploy), not automatic rollback.
