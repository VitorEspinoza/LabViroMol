# Modules

**English** · [Português](./modules.pt-BR.md)

Each module is a self-contained vertical slice of the application. Below is a description of each module's responsibility, key entities, and API surface.

---

## Identity

Manages users, roles, and permission-based access control.

**Key entities**: User, Role, Permission

**Features**:
- User registration, login, and password reset (email-based)
- Soft-delete user deactivation
- Role assignment and permission management
- JWT token issuance

**Endpoints**: `/api/identity/*`

---

## Inventory

Manages lab materials, stock levels, kits, and purchase orders.

**Key entities**: Material, MaterialType, Kit, Order, StockTransaction

**Features**:
- Material CRUD with minimum stock thresholds
- Kit bundles (pre-configured sets of materials with quantities)
- Purchase order creation and processing
- Stock transaction history
- Domain event raised when stock falls below minimum (triggers notification)

**Endpoints**: `/api/inventory/*`

---

## Research

Manages the academic and research activities of the lab.

**Key entities**: Project, Researcher, Partner, Position, Publication

**Features**:
- Research project lifecycle management
- Researcher and external partner management
- Academic publication tracking
- Public-facing institutional endpoints (no auth required)

**Endpoints**: `/api/research/*` (some routes under `/public`)

---

## Scheduling

Manages lab schedules and appointments.

**Key entities**: Schedule

**Features**:
- Schedule creation and management
- Public institutional schedule listing
- Rate limiting: 5 requests/hour on public endpoints

**Endpoints**: `/api/scheduling/*` (some routes under `/public`)

---

## Assets

Manages lab equipment inventory and maintenance requests.

**Key entities**: Equipment, MaintenanceRequest

**Features**:
- Equipment CRUD with image upload
- Multi-language equipment descriptions via LibreTranslate
- Maintenance request tracking with status workflow
- Public-facing equipment listing

**Endpoints**: `/api/assets/*` (some routes under `/public`)

---

## Notify

Handles in-app notifications and outbound emails.

**Key entities**: Notification

**Features**:
- Create, dismiss, and batch-dismiss notifications
- Email delivery via Brevo HTTP API

**Endpoints**: `/api/notify/*`

---

## Shared

Cross-cutting infrastructure consumed by all other modules. Not directly exposed via HTTP.

**Provides**:
- `AggregateRoot<TId>` base class with domain event support
- `BaseUnitOfWork<TContext>` with audit fields and event publishing
- Auditable entity interfaces (`ICreationAuditable`, `IModificationAuditable`, `IDeletionAuditable`)
- Strong ID value object pattern (`IEntityId`)
- Pagination utilities (`PagedRequest`, `PagedResponse<T>`)
- `GlobalExceptionHandler` middleware with ProblemDetails
- Permission constants (`Permissions` static class)
- `SmartEnum` JSON and EF converters
