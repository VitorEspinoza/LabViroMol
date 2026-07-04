# Testing

**English** · [Português](./testing.pt-BR.md)

## Test Projects

```
tests/
├── UnitTests/
│   └── Modules/
│       └── <Module>/
│           └── Domain/       # Entity and aggregate logic tests
└── IntegrationTests/
    └── Modules/
        └── <Module>/         # Full HTTP endpoint tests
```

## Unit Tests

Unit tests cover domain logic in isolation — aggregate methods, value object behavior, and business rules. No database or HTTP stack involved.

**Libraries**:
- xUnit for test runner and assertions
- Bogus for realistic fake data generation
- NSubstitute for mocking dependencies

**Example pattern**:

```csharp
public class MaterialTests
{
    [Fact]
    public void UpdateStock_BelowMinimum_RaisesLowStockEvent()
    {
        var material = MaterialFaker.Generate();
        material.UpdateStock(material.MinStock - 1);
        material.Events.Should().ContainSingle(e => e is LowStockEvent);
    }
}
```

## Integration Tests

Integration tests spin up the full ASP.NET Core pipeline in-memory using `WebApplicationFactory`. They hit real endpoints over HTTP with an in-memory EF database.

**Libraries**:
- Microsoft.AspNetCore.Mvc.Testing
- xUnit
- Bogus
- NSubstitute (for external service mocks, e.g. email)

**Key helpers**:
- `IdentityIntegrationTestWebAppFactory` — custom factory that configures in-memory DB and seeds test data
- JWT token helper — generates valid tokens for authenticated requests

**Example pattern**:

```csharp
public class CreateMaterialTests : IClassFixture<InventoryIntegrationTestWebAppFactory>
{
    [Fact]
    public async Task Post_ValidRequest_Returns201()
    {
        var client = _factory.CreateAuthenticatedClient();
        var response = await client.PostAsJsonAsync("/api/inventory/materials", new { ... });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

## Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/UnitTests

# Integration tests only
dotnet test tests/IntegrationTests

# With coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## Coverage

Coverage is collected via Coverlet. Configuration is in each test `.csproj` via the `coverlet.collector` package.

## Architecture Tests

The repository now enforces architecture and convention checks in three complementary layers:

- `tests/ArchitectureTests/LabViroMol.ArchitectureTests/` uses ArchUnitNET to validate layer dependencies, module isolation, CQRS conventions, DDD modeling, persistence boundaries, and presentation rules.
- `Directory.Build.props`, `.editorconfig`, and `BannedSymbols.txt` enable Roslyn-based analyzers for `src/**`, with build-breaking banned APIs such as `DateTime.Now`, `DateTime.UtcNow`, and `Console`.
- `tests/IntegrationTests/Shared/EndpointAuthorizationTests.cs` validates runtime endpoint metadata to ensure every route is either protected by authorization or explicitly allowlisted as public.

### How To Run

```bash
# Build production projects with analyzers
dotnet build LabViroMol.sln

# Architecture tests
dotnet test tests/ArchitectureTests/LabViroMol.ArchitectureTests/LabViroMol.ArchitectureTests.csproj

# Integration authorization guard
dotnet test tests/IntegrationTests/Shared/LabViroMol.IntegrationTests.Shared.csproj --filter EndpointAuthorizationTests
```

### Opt-in Rules

Some architecture rules are intentionally present but skipped until the codebase is prepared for them:

- handlers must be `sealed`
- repository/query implementations must be `internal`
- aggregates should expose no public setters
- intra-module feature slices should be cycle-free

These rules should only be activated together with the corresponding refactor that makes the current code conform.

### Intentionally Not Enforced

- `ConfigureAwait(false)` is not enforced because it is not useful in ASP.NET Core request handling.
- Coverage thresholds are still a QA concern, not an architectural rule.
