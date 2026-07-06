# API Reference

**English** · [Português](./api.pt-BR.md)

The API is documented interactively via Scalar (OpenAPI). In development, visit `/scalar/v1` after starting the server.

## Base URL

```
https://localhost:<port>/api
```

## Authentication

All protected endpoints require a Bearer JWT token in the `Authorization` header:

```
Authorization: Bearer <token>
```

Obtain a token via `POST /api/identity/users/login`.

---

## Endpoint Groups

### Identity — `/api/identity`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | `/users` | Register a new user | Required |
| POST | `/users/login` | Authenticate and receive a JWT | Public |
| POST | `/users/forgot-password` | Send password reset email | Public |
| DELETE | `/users/{id}` | Deactivate (soft-delete) a user | Required |
| GET | `/roles` | List roles | Required |
| POST | `/roles` | Create a role | Required |
| PUT | `/roles/{id}` | Update a role's permissions | Required |

---

### Inventory — `/api/inventory`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/materials` | Paginated material list | Required |
| POST | `/materials` | Create material | Required |
| PUT | `/materials/{id}` | Update material | Required |
| DELETE | `/materials/{id}` | Delete material | Required |
| GET | `/material-types` | List material types | Required |
| POST | `/material-types` | Create type | Required |
| GET | `/kits` | Paginated kit list | Required |
| POST | `/kits` | Create kit | Required |
| PUT | `/kits/{id}` | Update kit | Required |
| DELETE | `/kits/{id}` | Delete kit | Required |
| GET | `/orders` | List purchase orders | Required |
| POST | `/orders` | Create order | Required |
| GET | `/reports/stock-outflows/by-project.pdf` | PDF report: material outflows by project | Required |
| GET | `/reports/stock-outflows/by-month.pdf` | PDF report: material outflows by month | Required |
| GET | `/reports/stock-outflows/totals.pdf` | PDF report: total material outflows | Required |
| GET | `/reports/stock-inflows/by-order-material-month.pdf` | PDF report: material inflows by order/material/month | Required |
| GET | `/reports/critical-stock-balance.pdf` | PDF report: current stock vs minimum stock | Required |
| GET | `/reports/material-audit-movements.pdf` | PDF report: auditable material movements | Required |
| GET | `/reports/manual-stock-adjustments.pdf` | PDF report: manual stock adjustments | Required |

Inventory report endpoints return `application/pdf` binary responses generated with QuestPDF.
They require an authenticated user with `Inventory.Stock.View` or `Inventory.Stock.Manage`.

Common query filters:

- `from`: required UTC date/time lower bound for transaction reports.
- `to`: required UTC date/time upper bound for transaction reports.
- `materialId`: optional material id.
- `materialTypeId`: optional material type id.
- `projectId`: optional project id for `/reports/stock-outflows/by-project.pdf`.

Transaction reports require `from` and `to`, and the accepted range is capped at 366 days. The critical stock balance report is based on current material state and does not require a date range.

Specific filters:

- `onlyCritical`: optional boolean for `/reports/critical-stock-balance.pdf`; default is `true`.
- `transactionType`: optional transaction type for `/reports/material-audit-movements.pdf`.
- `limit`: optional row limit for `/reports/material-audit-movements.pdf`; capped by the backend.

Frontend handoff: call these endpoints as file downloads/blob requests. Do not parse JSON from successful responses. Validation and authorization failures still use the API's standard error behavior/status codes.

---

### Assets — `/api/assets`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/equipments` | Paginated equipment list | Required |
| POST | `/equipments` | Create equipment (with image) | Required |
| PUT | `/equipments/{id}` | Update equipment | Required |
| DELETE | `/equipments/{id}` | Delete equipment | Required |
| GET | `/public/equipments` | Public equipment listing | Public |
| GET | `/maintenance-requests` | List maintenance requests | Required |
| POST | `/maintenance-requests` | Create request | Required |
| PUT | `/maintenance-requests/{id}` | Update request status | Required |

---

### Research — `/api/research`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET/POST/PUT/DELETE | `/projects` | Research project CRUD | Required |
| GET/POST/PUT/DELETE | `/researchers` | Researcher CRUD | Required |
| GET/POST/PUT/DELETE | `/partners` | Partner organization CRUD | Required |
| GET/POST/PUT/DELETE | `/positions` | Position CRUD | Required |
| GET/POST/PUT/DELETE | `/publications` | Publication CRUD | Required |
| GET | `/public/*` | Public institutional data | Public |

---

### Scheduling — `/api/scheduling`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/schedules` | List schedules | Required |
| POST | `/schedules` | Create schedule | Required |
| GET | `/public/schedules` | Public schedule listing (rate-limited) | Public |

---

### Notify — `/api/notify`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/notifications` | List user notifications | Required |
| POST | `/notifications/{id}/dismiss` | Dismiss a notification | Required |
| POST | `/notifications/dismiss-all` | Dismiss all notifications | Required |
| POST | `/notifications/dismiss-batch` | Dismiss a set of notifications | Required |

---

### Admin BFF - `/api/admin`

Read-model endpoints tailored to the Angular admin panel.

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/dashboard/summary` | Dashboard counters and small preview lists | Required |

`GET /api/admin/dashboard/summary` requires an authenticated user. Each response section is returned only when the user has the matching module permission:

- `scheduling`: `Scheduling.Schedules.View` or `Scheduling.Schedules.Manage`
- `inventory`: `Inventory.Materials.View` or `Inventory.Materials.Manage`
- `assets`: `Assets.Maintenance.View` or `Assets.Maintenance.Manage`

If the user has no dashboard-relevant permission, the endpoint returns `403 Forbidden`. If the user has partial permissions, unauthorized sections are `null` and their queries are not executed.

Example response:

```json
{
  "scheduling": {
    "pendingSchedulesCount": 3,
    "approvedSchedulesThisMonthCount": 12,
    "upcomingSchedules": [
      {
        "id": "00000000-0000-0000-0000-000000000000",
        "schedulerName": "Joao da Silva",
        "date": "2026-06-25",
        "startDateHour": "2026-06-25T14:00:00Z",
        "equipmentNames": ["Microscopio Optico"],
        "status": "SCHEDULED"
      }
    ]
  },
  "inventory": {
    "lowStockMaterialsCount": 5,
    "lowStockMaterials": [
      {
        "id": "00000000-0000-0000-0000-000000000000",
        "name": "Alcool 70%",
        "location": "Armario 3",
        "stockQuantity": 1,
        "minStock": 5,
        "unit": "Milliliter"
      }
    ]
  },
  "assets": {
    "activeMaintenanceRequestsCount": 4
  },
  "generatedAt": "2026-06-20T18:00:00Z"
}
```

Frontend handoff: replace the dashboard's multiple calls to scheduling, materials and maintenance endpoints with this single endpoint. Keep UI guards by permission; use `null` sections as the backend source of truth for unavailable data.

---

## Error Responses

All errors follow RFC 9457 ProblemDetails format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed",
  "status": 422,
  "errors": {
    "Name": ["'Name' must not be empty."]
  }
}
```

Common status codes:

| Status | Meaning |
|--------|---------|
| 400 | Bad request / validation error |
| 401 | Missing or invalid JWT |
| 403 | Insufficient permissions |
| 404 | Resource not found |
| 422 | Domain rule violation |
| 429 | Rate limit exceeded |
| 500 | Unexpected server error |

## Static Files

Uploaded images are served at:

```
/images/equipments/<filename>
/images/schedule-terms/<filename>
```
