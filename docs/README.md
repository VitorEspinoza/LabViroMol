# Documentation — LabViroMol

**English** · [Português](./README.pt-BR.md)

Starting point for understanding the project before contributing. The order below builds context bottom-up; if this is your first time in the repository, follow it top to bottom.

## Where to start

**[`architecture.md`](./architecture.md)** — the structural view: how modules are organized, the layering (Domain / Application / Infrastructure / Presentation), and the core decisions (CQRS via Mediator, multi-schema database, domain events, Minimal APIs). Worth reading before anything else, since it defines the vocabulary the rest of the documentation assumes.

**[`modules.md`](./modules.md)** — what the system does. Describes the 6 business modules (Assets, Identity, Inventory, Notify, Research, Scheduling) and the cross-cutting Shared module, with each one's responsibility, main entities, features and API surface. The place to find which module a feature belongs to.

**[`patterns.md`](./patterns.md)** — how the code is written. Catalogs the recurring patterns: Mediator, Repository, Aggregate Root, Strong IDs, SmartEnum, Unit of Work, Soft Delete, pagination, Minimal API endpoints and permission-based authorization. New code should follow what's here.

**[`api.md`](./api.md)** — the HTTP contracts. Endpoints per module (method, path, description, auth requirement), the standard error format (ProblemDetails) and the status codes used. This is what the Angular repository consumes; see [Frontend boundary](#frontend-boundary).

**[`getting-started.md`](./getting-started.md)** — running the project. Prerequisites, secrets configuration, bringing up Docker (LibreTranslate, Postgres), applying each module's migrations and starting the API.

**[`testing.md`](./testing.md)** — the testing strategy: suite structure, libraries, patterns for unit tests (domain) and integration tests (HTTP endpoints), and how to run with coverage.

**[`ci-cd.md`](./ci-cd.md)** — the end-to-end pipeline. Covers the 12 GitHub Actions workflows: the PR gates (build/tests, migration guard, OpenAPI contract, CodeQL, SCA, gitleaks, Trivy, SBOM, DAST/perf smoke on an ephemeral stack), the CD flow (scan-gate → signed images on GHCR → migrate-first deploy on the droplet → release gate via New Relic), the scheduled jobs (perf-load, DORA, security crons) and a glossary of the patterns used. Check this before touching any workflow, Dockerfile, CI compose file or deploy script.

## Visual documentation

Besides the text above, the architecture is documented in diagrams — some crossing modules (in `architecture/`), some specific to each module (in `src/Modules/<Module>/docs/`).

### Cross-module (`docs/architecture/`)

Follows the [C4 Model](https://c4model.com/) for the structural view and the strategic patterns of [Domain-Driven Design](https://martinfowler.com/bliki/BoundedContext.html) for the relationships between bounded contexts. It uses three notations: most diagrams in Mermaid (renders natively on GitHub); the C4 Model in Structurizr DSL (a single model, navigable across levels); the Context Map in D2 ([d2lang.com](https://d2lang.com)), with role markers (OHS/PL/ACL/CF/SK/U/D) on the relationship endpoints, following Vaughn Vernon's "Implementing Domain-Driven Design".

Suggested reading order, broadest to most detailed:

1. **Strategic view (DDD)** — [`architecture/context-map/context-map.md`](./architecture/context-map/context-map.md): how the 7 bounded contexts relate, in D2 ([`context-map.d2`](./architecture/context-map/context-map.d2)).
2. **C4 Level 1 — Context** — [`architecture/c4-model/c4-context.md`](./architecture/c4-model/c4-context.md): the system, its users and the real external integrations (only Gmail SMTP — LibreTranslate is self-hosted and only shows up from Level 2 on). Source: [`workspace.dsl`](./architecture/c4-model/workspace.dsl).
3. **C4 Level 2 — Containers** — [`architecture/c4-model/c4-container.md`](./architecture/c4-model/c4-container.md): the deployable units (API, frontends, database, gateway, LibreTranslate).
4. **C4 Level 3 — Components** — [`architecture/c4-model/c4-component.md`](./architecture/c4-model/c4-component.md): the modules and internal pieces of the API.
5. **Cross-module view** — [`architecture/cross-module-overview.md`](./architecture/cross-module-overview.md): the 4 actors, the 14 Aggregate Roots with their cross-module relationships, and the data references without database FKs. Per-module detail (use cases, classes, ER) lives in `src/Modules/<Module>/docs/`.
6. **Deployment** — [`architecture/deployment/deployment.md`](./architecture/deployment/deployment.md): the physical container topology on the production droplet.
7. **Observability** — [`architecture/observability-overview.md`](./architecture/observability-overview.md): the telemetry flow from the HTTP request to New Relic, through the Mediator pipeline (`ValidationBehavior`/`LoggingBehavior`), OTel-native logging, `Activity`/OTel spans, the OTLP exporter, plus the parallel paths for Outbox, translation and e-mail. Local configuration and validation are in [`getting-started.md`](./getting-started.md#observability--new-relic).

### Per module (`src/Modules/<Module>/docs/`)

Each module has its own `docs/` folder with the diagrams specific to it — only the types that make sense for that module (not all of them have their own sequence or state diagrams):

| Module         | Folder                                                                     | Content                                                                                                             |
| -------------- | -------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| **Identity**   | [`src/Modules/Identity/docs/`](../src/Modules/Identity/docs/README.md)     | Use cases, classes, sequence (Login, Refresh Token), ER (`identity`)                                        |
| **Inventory**  | [`src/Modules/Inventory/docs/`](../src/Modules/Inventory/docs/README.md)   | Use cases, classes, sequence (7 flows, incl. consuming material for a project), state (`Order`), ER (`inventory`) |
| **Assets**     | [`src/Modules/Assets/docs/`](../src/Modules/Assets/docs/README.md)         | Use cases, classes, state (`MaintenanceRequest`), ER (`assets`)                                              |
| **Research**   | [`src/Modules/Research/docs/`](../src/Modules/Research/docs/README.md)     | Use cases, classes and ER split into 3 sub-blocks (Project / Researcher / Publication)                      |
| **Scheduling** | [`src/Modules/Scheduling/docs/`](../src/Modules/Scheduling/docs/README.md) | Use cases, classes, sequence (4 scheduling flows), state (`Schedule`), ER (`scheduling`)                    |
| **Notify**     | [`src/Modules/Notify/docs/`](../src/Modules/Notify/docs/README.md)         | Use cases, classes, ER (`notify`)                                                                            |
| **Shared**     | [`src/Modules/Shared/docs/`](../src/Modules/Shared/docs/README.md)         | Shared Kernel classes (`BaseEntity`, `AggregateRoot`, `SmartEnum`, `IStrongId`, etc.)                        |

## Quick summary

```
Project:   LabViroMol — REST API for virology lab management
Stack:     .NET 10 · ASP.NET Core · EF Core 10 (PostgreSQL/Npgsql) · Mediator · FluentValidation · JWT
Frontend:  Angular — separate repository, with its own docs/
Modules:   Assets · Identity · Inventory · Notify · Research · Scheduling · Shared
Pattern:   Clean Architecture per module + CQRS via Mediator + Domain Events
Auth:      JWT Bearer + string permissions ("Module.Resource.Action")
Tests:     xUnit + Bogus + NSubstitute · unit (domain) + integration (endpoints)
```

## Frontend boundary

The Angular frontend is developed in its own repository, which has its own `docs/`. This repository does not contain or coordinate the frontend.

The single integration point maintained here is the API contract in [`api.md`](./api.md). Whenever an endpoint is created, changed or removed, `api.md` needs to be kept in sync — it's what the Angular side consumes to keep the integration up to date.
