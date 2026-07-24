# Architecture

**English** · [Português](./architecture.pt-BR.md)

LabViroMol is a modular ASP.NET Core REST API built for virology lab management. The system follows Clean Architecture principles with a CQRS-inspired design.

## High-Level Structure

```
LabViroMol/
├── src/
│   ├── LabViroMol.Api/           # Entry point, program composition
│   └── Modules/
│       ├── Assets/               # Equipment & maintenance
│       ├── Identity/             # Users, roles, permissions
│       ├── Inventory/            # Materials, kits, orders, stock
│       ├── Notify/               # Notifications and emails
│       ├── Research/             # Projects, publications, researchers
│       ├── Scheduling/           # Schedule management
│       └── Shared/               # Kernel, infrastructure base classes
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
└── docker-compose.yaml           # LibreTranslate service
```

## Module Internal Structure

Every module follows the same four-layer layout:

```
Module/
├── Domain/           # Entities, aggregates, value objects, repository interfaces
├── Application/      # Commands, queries, handlers, validators, view models
├── Infrastructure/   # DbContext, repository implementations, external services
└── Presentation/     # Minimal API endpoint definitions
```

Optional `Contracts/` layer is used when a module needs to expose types for cross-module communication.

## Key Architectural Decisions

### Mediator / CQRS
All requests flow through a mediator pipeline. Commands mutate state; queries read it. A `ValidationBehavior` pipeline step runs FluentValidation validators before handlers execute.

### Multi-Schema Database
Each module owns a separate `DbContext` and database schema, enabling future extraction to independent services without structural changes.

### Minimal APIs
No MVC controllers — endpoints are defined using `MapGroup()` extension methods per module and registered in `Program.cs`.

### JWT + Permission-Based Authorization
ASP.NET Core Identity issues JWTs. Authorization is enforced per-endpoint via granular permission constants (e.g., `Permissions.Inventory.MaterialsManage`).

### Domain Events
Aggregates raise domain events via `AddEvent()`. The `BaseUnitOfWork` publishes them on `CompleteAsync()`, enabling loose coupling between modules (e.g., low-stock triggers a notification).

## Frontend Integration

The Angular frontend runs separately on `http://localhost:4200`. The API is CORS-configured to accept requests from that origin. Uploaded images are served at `/images/*`.
