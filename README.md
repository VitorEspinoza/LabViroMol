# LabViroMol

**English** · [Português](./README.pt-BR.md)

Modular REST API for virology lab management, built with ASP.NET Core on .NET 10. It handles the lab's inventory, equipment, research projects, scheduling, users and notifications, and serves both an authenticated admin panel (Angular) and a public institutional site.

## Overview

The backend is organized as independent business modules, each following Clean Architecture and communicating through a mediator pipeline. Every module owns its own database schema, which keeps the boundaries explicit and leaves the door open to extracting a module into its own service later without reshaping the code.

- **Architecture:** Clean Architecture per module, CQRS via Mediator, domain events for cross-module reactions (e.g. low stock triggers a notification).
- **Persistence:** one `DbContext` and one PostgreSQL schema per module.
- **API:** Minimal APIs grouped per module, documented via OpenAPI (Scalar UI at `/scalar/v1` in development).
- **Auth:** JWT Bearer with string-based permissions (`Module.Resource.Action`).
- **Observability:** OpenTelemetry (logs, metrics, traces) exported to New Relic over OTLP.

## Tech stack

.NET 10 · ASP.NET Core · EF Core 10 (PostgreSQL / Npgsql) · Mediator · FluentValidation · JWT · OpenTelemetry · Docker · xUnit · NBomber

## Modules

| Module | Responsibility |
|--------|----------------|
| **Identity** | Users, roles and permissions; JWT authentication |
| **Inventory** | Materials, kits, purchase orders and stock movement |
| **Assets** | Equipment and maintenance requests |
| **Research** | Projects, researchers and publications |
| **Scheduling** | Lab-usage scheduling and its lifecycle |
| **Notify** | Internal notifications and outbound e-mails |
| **Shared** | Shared Kernel: base entities, aggregate roots, strong IDs, SmartEnum |

## Project structure

```
src/
├── LabViroMol.Api/        # Entry point, host composition
└── Modules/<Module>/
    ├── Domain/            # Entities, aggregates, value objects, repository interfaces
    ├── Application/       # Commands, queries, handlers, validators, view models
    ├── Infrastructure/    # DbContext, repositories, external services
    └── Presentation/      # Minimal API endpoints
tests/
├── UnitTests/            # Domain
├── IntegrationTests/     # HTTP endpoints
└── LoadTests/            # Performance & resilience (NBomber)
```

## Getting started

Prerequisites: .NET 10 SDK and Docker.

```bash
git clone https://github.com/VitorEspinoza/LabViroMol
cd LabViroMol
docker compose up -d                    # PostgreSQL + LibreTranslate
dotnet run --project src/LabViroMol.Api
```

Before the first run you also need to configure user secrets and apply the per-module migrations. The full walkthrough is in [`docs/getting-started.md`](./docs/getting-started.md).

To run the whole stack the way it runs in production — API + Angular admin panel + Next.js institutional site behind an nginx gateway, all built from local source — clone [`LabViroMol-Admin-Panel`](https://github.com/VitorEspinoza/LabViroMol-Admin-Panel) and [`labviromol-institucional`](https://github.com/VitorEspinoza/labviromol-institucional) as siblings of this repo and run `docker compose up -d --build`; `docker-compose.override.yml` picks up the frontends automatically. See [Running the full stack locally](./docs/getting-started.md#running-the-full-stack-locally-docker-compose) for the full steps, including how `.env` is filled in without an existing database to connect to.

## Testing

```bash
dotnet test
```

Unit tests cover the domain; integration tests exercise the HTTP endpoints. See [`docs/testing.md`](./docs/testing.md) for the strategy and [`tests/LoadTests/`](./tests/LoadTests/) for the load/resilience suite.

## Documentation

Start at [`docs/README.md`](./docs/README.md) for the full index. Quick links:

- [Architecture](./docs/architecture.md) · [Modules](./docs/modules.md) · [Patterns](./docs/patterns.md)
- [API reference](./docs/api.md) · [API contract](./docs/api-contract.md)
- [Getting started](./docs/getting-started.md) · [Migrations](./docs/migrations.md) · [Testing](./docs/testing.md)
- [CI/CD](./docs/ci-cd.md) · [DORA metrics](./docs/dora.md) · [Runbooks](./docs/runbooks/)
- Architecture diagrams (C4, context map, cross-module views) under [`docs/architecture/`](./docs/architecture/)

The Angular admin panel and the institutional site live in their own repositories; the integration contract between them and this API is [`docs/api.md`](./docs/api.md).
