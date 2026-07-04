# API Contract — OpenAPI as the single source of truth

**English** · [Português](./api-contract.pt-BR.md)

## Decision

The backend is the **single source of truth for the API contract** (schema-first). The OpenAPI spec
is generated from the code (minimal API attributes / `AddOpenApi()`) — never hand-written — and
versioned in `contracts/openapi.json`, at the root of the repository. Pact (consumer-driven contracts)
was discarded: it would require a dedicated broker/infra, which is over-engineering for the size of
the team.

This model covers the **structural** contract (endpoints, shapes, types, status codes). The
**behavioral** contract (business rules, side effects) remains the responsibility of each frontend's
E2E suite.

## Where the published spec lives

- **File:** [`contracts/openapi.json`](../contracts/openapi.json) — always reflects the state of
  `main`.
- **Consumption by the frontends:** fetch via the raw GitHub URL
  (`https://raw.githubusercontent.com/VitorEspinoza/LabViroMol/main/contracts/openapi.json`) or via
  clone/submodule, depending on each repo's client generator (NSwag/openapi-typescript/etc. — a
  decision for each frontend's own plan).
- Do not use the production app's `/openapi/v1.json` endpoint as the source of truth for client
  generation: it is only mapped in `Development` (`app.MapOpenApi()` in
  `src/LabViroMol.Api/Program.cs`). The source of truth is always the versioned file at
  `contracts/openapi.json`.

## How the spec is generated

The `Microsoft.Extensions.ApiDescription.Server` package (configured in
`src/LabViroMol.Api/LabViroMol.Api.csproj`) generates the spec at **build time**, via an MSBuild
target (`dotnet build`), without starting the application (no `Listen`, no exposed HTTP route). The
`dotnet-getdocument` tool process needs to build the `IHost` (to discover the endpoints registered
via Mediator), and therefore requires a `ConnectionStrings:LabViroMol` value — but no real
connection is opened in this flow. In CI this is satisfied with a dummy connection string
(`ConnectionStrings__LabViroMol` in the workflow's `env:`); it is never used to actually connect to
a database.

The generated artifact is named `contracts/LabViroMol.Api.json` (name derived from the project); the
workflow renames it to `openapi.json` before publishing/comparing.

Locally:

```bash
export ConnectionStrings__LabViroMol="Host=localhost;Port=5432;Database=dummy;Username=dummy;Password=dummy"
dotnet build src/LabViroMol.Api/LabViroMol.Api.csproj -c Release
mv contracts/LabViroMol.Api.json contracts/openapi.json
```

## Breaking-change gate (`oasdiff`)

The workflow [`.github/workflows/api-contract.yml`](../.github/workflows/api-contract.yml) runs on
every PR to `main`:

1. Generates the spec for the PR revision (`contracts/revision.openapi.json`).
2. Fetches the spec published on `main` (`contracts/openapi.json` via `git show
   origin/main:...`) as the comparison baseline. If it doesn't exist yet (first run), the gate is
   skipped with a warning.
3. Runs `oasdiff breaking base.json revision.json` (Docker image `tufin/oasdiff`) comparing the two
   specs.
4. **If there is a breaking change** (removed field, changed type, removed endpoint, etc.) → the job
   fails, **unless** the PR has the `api-breaking-approved` label — the same convention used by
   [`migration-guard`](../.github/workflows/migration-guard.yml) (plan 14) for destructive
   migrations.
5. Additive PRs (new field, new endpoint) pass without friction.

On `push` to `main`, the `publish-spec` job regenerates the spec, overwrites
`contracts/openapi.json`, and makes an automatic commit (`[skip ci]`) if there's a diff — keeping the
root file always in sync with the code on `main`.

## Versioning / breaking-change policy

- **Additive** change (new endpoint, new optional field, new enum value in a field already treated
  as an open string): not breaking, passes straight through.
- **Breaking** change (new required field, removal/rename of field/endpoint, type change, default
  status code change): requires a conscious decision — add the `api-breaking-approved` label to the
  PR and **notify the consuming frontend teams** (Admin Panel, Institutional) before merging, since
  their client will break the next time they regenerate it from the updated contract.
- It's recommended to open the breaking PR first as a draft, to give the frontend time to prepare,
  especially for authentication changes or widely-used endpoints (`/api/identity/*`,
  `/api/inventory/*`).

## Response body coverage (GET)

The initial spec generation (plan 27) only captured the `requestBody` of write commands — ASP.NET
Core minimal APIs only infer the response schema in OpenAPI when the handler declares a concrete
return type (`Results<Ok<T>, NotFound>`) or explicitly uses `.Produces<T>(...)`. 43 `GET` endpoints
were left with `content?: never` in the generated spec (no 200 schema), which prevented type safety
in the frontends' typed clients for read flows.

All `GET` endpoints in the spec today annotate the real response schema via
`.Produces<TViewModel>(StatusCodes.Status200OK)` (the same pattern already used by write endpoints) —
including `404`/`403` where applicable. The PDF report endpoints (`/api/inventory/reports/*.pdf`)
annotate `.Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf")` instead of a
JSON schema, since they return binary content.

Coverage: **43 endpoints without a schema → 0** (verified in `contracts/openapi.json` after build; no
`GET` in the final spec has missing or empty `content` on `200`).

## For frontend teams

- The typed client must be generated from `contracts/openapi.json` on `main` (not from the runtime
  endpoint).
- Structural drift between the generated client and the actual backend automatically becomes a
  *typecheck* error in the frontend's CI, whenever the contract is regenerated from a newer version
  of `openapi.json`.
- If the backend introduces an approved breaking change (`api-breaking-approved` label), the
  frontend team will be notified out of band (PR description / direct communication) — the
  `oasdiff` gate does not block the merge on the backend when the label is present, so manual
  communication is mandatory in that case.
