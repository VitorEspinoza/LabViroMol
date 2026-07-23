# Getting Started

**English** · [Português](./getting-started.pt-BR.md)

## Prerequisites

- .NET 10 SDK
- Docker (for PostgreSQL and LibreTranslate)

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/VitorEspinoza/LabViroMol
cd LabViroMol
```

### 2. Start the database and translation service

```bash
docker compose up -d
```

This starts PostgreSQL 17 on port 5432 and LibreTranslate on port 5000.

### 3. Configure secrets

The API uses ASP.NET Core User Secrets. Set the required values:

```bash
cd src/LabViroMol.Api
dotnet user-secrets set "ConnectionStrings:LabViroMol" "Host=localhost;Port=5432;Database=LabViroMol;Username=labviromol;Password=labviromol_dev"
dotnet user-secrets set "Jwt:Key" "<your-secret>"
dotnet user-secrets set "Jwt:Issuer" "<issuer>"
dotnet user-secrets set "Jwt:Audience" "<audience>"
dotnet user-secrets set "Email:ApiKey" "<brevo-api-key>"
```

### 4. Apply migrations

Each module has its own `DbContext`. Run migrations for each:

```bash
dotnet ef database update --project src/Modules/Identity/Infrastructure --startup-project src/LabViroMol.Api --context LabViroMolIdentityDbContext
dotnet ef database update --project src/Modules/Inventory/Infrastructure --startup-project src/LabViroMol.Api --context InventoryDbContext
dotnet ef database update --project src/Modules/Assets/Infrastructure --startup-project src/LabViroMol.Api --context AssetsDbContext
dotnet ef database update --project src/Modules/Research/Infrastructure --startup-project src/LabViroMol.Api --context ResearchDbContext
dotnet ef database update --project src/Modules/Scheduling/Infrastructure --startup-project src/LabViroMol.Api --context SchedulingDbContext
dotnet ef database update --project src/Modules/Notify/Infrastructure --startup-project src/LabViroMol.Api --context NotifyDbContext
```

### 5. Run the API

```bash
dotnet run --project src/LabViroMol.Api
```

The API starts on `https://localhost:7xxx` (port shown in console). The interactive API explorer (Scalar) is available at `/scalar/v1` in the development environment.

## Frontend

The Angular frontend is a separate repository and is expected to run on `http://localhost:4200`. CORS is pre-configured for that origin.

## Running the full stack locally (Docker Compose)

The steps above run only the API, against a Docker-hosted Postgres/LibreTranslate. To run everything the way it runs in production — API + both frontends (Angular admin panel, Next.js institutional site) + an nginx gateway in front of all three — but entirely on your machine, from your own local source, use `docker-compose.override.yml`. It's already checked into this repo and Docker Compose merges it in automatically alongside `docker-compose.yaml`; there's nothing to enable.

### 1. Clone all three repositories as siblings

The override file builds both frontends from source, referencing them by relative path, so the three repositories need to sit next to each other with these exact folder names:

```
some-folder/
├── LabViroMol/                   # this repo
├── LabViroMol-Admin-Panel/       # Angular admin panel
└── labviromol-institucional/     # Next.js institutional site
```

```bash
git clone https://github.com/VitorEspinoza/LabViroMol
git clone https://github.com/VitorEspinoza/LabViroMol-Admin-Panel
git clone https://github.com/rafaelleonardodev/labviromol-institucional
```

### 2. Configure `.env`

```bash
cd LabViroMol
cp .env.example .env
```

Every value the containers need comes from this one file — there's no external database to provision beforehand and no connection string to go fetch from anywhere. `docker-compose.yaml` creates the `postgres` container itself, using whatever `POSTGRES_USER`/`POSTGRES_PASSWORD`/`POSTGRES_DB` you put in `.env`; the API's `DB_CONNECTION_STRING`, in that same file, just needs to reference those same three values, plus `Host=postgres` (the container's name on the Docker network — not `localhost`, since containers reach each other by service name, not by the host machine's loopback address). `.env.example` already keeps these consistent by default:

```
POSTGRES_USER=labviromol
POSTGRES_PASSWORD=labviromol_dev
POSTGRES_DB=LabViroMol
DB_CONNECTION_STRING=Host=postgres;Port=5432;Database=LabViroMol;Username=labviromol;Password=labviromol_dev
```

You can leave these three exactly as they are for local use — whatever values you pick, Postgres initializes itself with them the first time its volume is created, so as long as `POSTGRES_*` and `DB_CONNECTION_STRING` agree with each other, there's no "existing" database to match against. The one value you do need to change is `JWT_KEY` (any random string ≥32 characters) — the API refuses to start without a real one. `EMAIL_*` and `NR_LICENSE_KEY` can stay as the placeholder values; the API starts fine without a valid Brevo API key or New Relic account, those integrations just won't do anything.

### 3. Build and start everything

```bash
docker compose up -d --build
```

This builds the API from this repo's `Dockerfile`, the admin panel from `../LabViroMol-Admin-Panel`, and the institutional site from `../labviromol-institucional` (all declared in `docker-compose.override.yml`), then starts Postgres, LibreTranslate, the three application containers, and an nginx gateway (`nginx/gateway.local.conf`) in front of everything on port 80.

### 4. Apply migrations

The `migrate` service also comes up with `docker compose up -d` (it runs once and exits on its own), but you can run it explicitly at any point:

```bash
docker compose run --rm migrate
```

### 5. Access the stack

| URL | What |
|---|---|
| `http://localhost/` | Institutional site (Next.js) |
| `http://localhost/gestao-lab-ufpr/` | Admin panel (Angular) |
| `http://localhost/api/...` | API |

Because everything is served from the same origin (`http://localhost`) through the gateway, `CORS_ORIGIN_ADMIN`/`CORS_ORIGIN_INSTITUCIONAL` in `.env` don't come into play here — those only matter when a frontend is run directly (`ng serve`/`next dev`), hitting the API on a different port.

### Dev seed data

The first time the API starts in `Development` against an empty database, it automatically seeds about 20 valid records per module (users, materials, equipment, projects, schedules, etc.) so there's something to click through right away — see `src/LabViroMol.Api/DevSeed/DevSeeder.cs`. It runs once (skips if the database already has data) and logs the admin credentials on success:

```
DevSeed: done. Log in with admin@labviromol.local / Labviromol@123
```

Disable it by setting `DevSeed:Enabled` to `false` in `appsettings.Development.json`, or via the `DevSeed__Enabled=false` environment variable.

## Configuration Reference

Key values in `appsettings.json`:

| Key | Description |
|-----|-------------|
| `Storage:RootFolder` | Root path for uploaded files (`Upload/Images`) |
| `Storage:Folders:Equipments` | Sub-folder for equipment images |
| `Storage:Folders:ScheduleTerms` | Sub-folder for schedule term files |
| `Translation:IntervalMinutes` | How often the background translation job runs |
| `Frontend:BaseUrl` | Angular app URL for CORS and email links |
| `OpenTelemetry:OtlpEndpoint` | OTLP endpoint for logs/traces/metrics (appsettings fallback; see "Observability / New Relic" below) |
| `OpenTelemetry:Tracing:SamplingRatio` | Trace sampling ratio (`0.0`–`1.0`, default `1.0`) |
| `Observability:SlowQueryMs` | Threshold in ms above which an EF Core/Npgsql query is logged as `Warning` (default `500`) |

## Observability / New Relic

The API exports logs, metrics and traces via OpenTelemetry (OTel) to New Relic using the
OTLP protocol. Logging is OTel-native: `Microsoft.Extensions.Logging` (`ILogger<T>`) with the
`Logging.AddOpenTelemetry()` provider, no Serilog. The rest of the pipeline
(OTel SDK, sampler, metric names, log levels, PII policy) follows the project's
instrumentation conventions — worth knowing them before instrumenting new code. The full flow
diagram (HTTP request → Mediator
pipeline → ILogger/Activity → OTel SDK → OTLP → New Relic, including Outbox, translation and e-mail)
is in [`architecture/observability-overview.md`](./architecture/observability-overview.md).
HTTP request timing lives only in the span (ASP.NET Core's automatic instrumentation) — there is no
dedicated request log.

This section covers only what's needed to configure and validate telemetry locally.

### Environment variables

The API reads the standard OTel variables (a .NET convention, no additional code required) plus
one specific to New Relic, all already declared in `.env.example` and passed through by
`docker-compose.yaml` to the `api` service:

| Variable | Description | Default in dev |
|---|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP endpoint (gRPC/HTTP) to which logs/traces/metrics are exported. If not set, no OTLP exporter/sink is registered and the API starts normally with only console logging. | empty (telemetry off) |
| `OTEL_EXPORTER_OTLP_HEADERS` | OTLP authentication headers. In production this is built automatically as `api-key=${NR_LICENSE_KEY}`. | empty |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | OTLP protocol (`http/protobuf` or `grpc`). | `http/protobuf` |
| `OTEL_EXPORTER_OTLP_COMPRESSION` | OTLP payload compression. | `gzip` |
| `OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE` | Metrics temporality — New Relic requires `delta` (.NET defaults to `cumulative`). | `delta` |
| `NR_LICENSE_KEY` | New Relic ingest license key. **Secret** — never commit the real value or log it. | empty |

Alternatively, the endpoint can be configured via `appsettings`/User Secrets at
`OpenTelemetry:OtlpEndpoint` (read by `ObservabilityExtensions.ResolveOtlpEndpoint`) — the
`OTEL_EXPORTER_OTLP_ENDPOINT` env var has lower precedence and acts as a fallback when the
configuration key is not set.

### Getting the New Relic license key

1. Create an account (or use an existing one) at [newrelic.com](https://newrelic.com).
2. In the dashboard, go to **API keys** (profile/organization menu) and copy the ingest
   **license key** (not the *User key*, which is for New Relic's REST API — you want the key used
   by agents/OTLP).
3. Set `NR_LICENSE_KEY` in your local `.env` (never in `.env.example` or any versioned file).

### Validating locally

Two options, from simplest to closest to the production environment:

**Option 1 — export nothing (default):** don't set `OTEL_EXPORTER_OTLP_ENDPOINT`. The API starts
normally, `Microsoft.Extensions.Logging`'s default console provider writes to the console, and
the OTel SDK runs with no OTLP exporter registered (see
`ObservabilityExtensions.AddObservabilityTelemetry` — the `AddOtlpExporter` block is only added
when `otlpEndpoint` is not empty). Sufficient to validate that logs and metrics are being
generated without needing any external backend.

**Option 2 — point to the real New Relic:** set `OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.nr-data.net`
and `NR_LICENSE_KEY` with a valid key, start the API (locally or via `docker compose up -d`) and
generate traffic (calls to endpoints, an Outbox cycle, a translation job). Within a few minutes the
data shows up under **APM & Services** (traces), **Logs** and **Metrics & events** in the New
Relic dashboard, filtering by `service.name = labviromol-api`.

For a local OTel collector (e.g. to inspect payloads before spending New Relic ingest quota),
point `OTEL_EXPORTER_OTLP_ENDPOINT` to your local
[OpenTelemetry Collector](https://opentelemetry.io/docs/collector/) endpoint (e.g.
`http://localhost:4317` for gRPC) and use the collector's `debug`/`logging` exporter to print
what it receives — the API needs no code changes for this, just swap the env var.
