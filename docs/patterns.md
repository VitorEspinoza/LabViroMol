# Design Patterns

**English** · [Português](./patterns.pt-BR.md)

This document describes the recurring patterns used throughout the codebase.

## Mediator (CQRS-inspired)

All business operations are expressed as commands (mutations) or queries (reads) sent through an `IMediator`. A `ValidationBehavior` pipeline step runs FluentValidation before the handler.

```csharp
// Command
var result = await mediator.SendAsync(new CreateMaterialCommand(...));

// Query
var result = await mediator.SendAsync(new GetMaterialsQuery(pagedRequest));
```

## Repository + Query Objects

Repositories handle aggregate persistence. Separate query objects handle complex reads to keep repositories focused.

Query objects are defined as an interface in the Application layer (alongside the ViewModels they return) and implemented in Infrastructure. Presentation depends only on the Application interface — never on the concrete Infrastructure class — keeping the dependency pointing inward per Clean Architecture.

```csharp
// Application/Materials/Queries/IMaterialQueries.cs
public interface IMaterialQueries
{
    Task<PagedResponse<MaterialViewModel>> GetAllAsync(PagedRequest request);
}

// Infrastructure/Materials/MaterialQueries.cs
public class MaterialQueries(InventoryDbContext context) : IMaterialQueries
{
    public async Task<PagedResponse<MaterialViewModel>> GetAllAsync(PagedRequest request) { /* ... */ }
}

// Infrastructure/InfrastructureModule.cs
services.AddScoped<IMaterialQueries, MaterialQueries>();

// Presentation endpoint — depends on the interface only
group.MapGet("/", async ([AsParameters] PagedRequest request, IMaterialQueries materialQueries) =>
    Results.Ok(await materialQueries.GetAllAsync(request)));
```

Repository for writes:
```csharp
await materialRepository.AddAsync(material);
```

> Note: the `XxxModule.AddXxxModule()` composition-root file in Presentation still calls `services.AddInfrastructure(...)` to wire DI at startup — that's host wiring, not business logic, so it's the one accepted exception to "Presentation never references Infrastructure".

## Aggregate Root

Business entities inherit from `AggregateRoot<TId>`. Domain logic lives inside the aggregate; external code calls methods, not setters.

```csharp
public class Material : AggregateRoot<MaterialId>
{
    public void UpdateStock(int quantity)
    {
        // validate + raise domain event
        AddEvent(new StockUpdatedEvent(Id, quantity));
    }
}
```

## Strong IDs

Each aggregate has a dedicated ID value object wrapping a `Guid`. This prevents mixing up IDs from different aggregates at the type level.

```csharp
public record MaterialId(Guid Value) : IEntityId;
```

Custom EF and JSON converters handle serialization transparently.

## SmartEnum

Enums with behavior use `SmartEnum` instead of plain C# enums. Custom converters handle JSON and EF storage.

```csharp
public sealed class TransactionType : SmartEnum<TransactionType>
{
    public static readonly TransactionType Inbound = new(nameof(Inbound), 1);
    public static readonly TransactionType Outbound = new(nameof(Outbound), 2);
}
```

## Unit of Work

`BaseUnitOfWork<TContext>` wraps the EF `DbContext`. On `CompleteAsync()` it:
1. Populates audit fields (CreatedAt, UpdatedAt, IsDeleted, etc.)
2. Publishes domain events raised by aggregates
3. Commits the transaction

## Soft Delete

Entities implementing `IDeletionAuditable` are never hard-deleted. The `BaseUnitOfWork` sets `IsDeleted = true`, `RemovedAt`, and `RemovedBy` instead.

## Pagination

All list endpoints accept a `PagedRequest` (page number + page size) and return a `PagedResponse<T>`. `QueryableSearchExtensions` provides LINQ helpers.

## Minimal API Endpoints

Endpoints are grouped with `MapGroup()` and defined in extension methods per resource. Authorization is declared inline.

```csharp
group.MapPost("/", CreateMaterialHandler)
     .RequireAuthorization(Permissions.Inventory.MaterialsManage);
```

## Permission-Based Authorization

A static `Permissions` class defines hierarchical string constants in the form `"Module.Feature.Action"`.

```csharp
public static class Permissions
{
    public static class Inventory
    {
        public const string MaterialsManage = "Inventory.Materials.Manage";
        public const string MaterialsView   = "Inventory.Materials.View";
    }
}
```

## Global Exception Handling

A `GlobalExceptionHandler` middleware catches unhandled exceptions and returns RFC 9457 `ProblemDetails` responses consistently across the API. `DomainException` maps to 422; unexpected errors map to 500.
