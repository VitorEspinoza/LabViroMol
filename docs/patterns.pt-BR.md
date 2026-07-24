# Padrões de Design

[English](./patterns.md) · **Português**

Este documento descreve os padrões recorrentes usados em toda a base de código.

## Mediator (inspirado em CQRS)

Todas as operações de negócio são expressas como commands (mutações) ou queries (leituras) enviadas através de um `IMediator`. Um step de pipeline `ValidationBehavior` roda o FluentValidation antes do handler.

```csharp
// Command
var result = await mediator.SendAsync(new CreateMaterialCommand(...));

// Query
var result = await mediator.SendAsync(new GetMaterialsQuery(pagedRequest));
```

## Repository + Query Objects

Repositórios cuidam da persistência de agregados. Query objects separados cuidam de leituras complexas, mantendo os repositórios focados.

Query objects são definidos como uma interface na camada Application (junto dos ViewModels que retornam) e implementados na Infrastructure. A Presentation depende apenas da interface da Application — nunca da classe concreta da Infrastructure — mantendo a dependência apontando para dentro, conforme a Clean Architecture.

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

// Endpoint da Presentation — depende só da interface
group.MapGet("/", async ([AsParameters] PagedRequest request, IMaterialQueries materialQueries) =>
    Results.Ok(await materialQueries.GetAllAsync(request)));
```

Repository para escrita:
```csharp
await materialRepository.AddAsync(material);
```

> Nota: o arquivo `XxxModule.AddXxxModule()` (composition root) na Presentation ainda chama `services.AddInfrastructure(...)` para configurar a DI no startup — isso é wiring do host, não lógica de negócio, sendo a única exceção aceita à regra "Presentation nunca referencia Infrastructure".

## Aggregate Root

Entidades de negócio herdam de `AggregateRoot<TId>`. A lógica de domínio vive dentro do agregado; código externo chama métodos, não setters.

```csharp
public class Material : AggregateRoot<MaterialId>
{
    public void UpdateStock(int quantity)
    {
        // valida + dispara domain event
        AddEvent(new StockUpdatedEvent(Id, quantity));
    }
}
```

## Strong IDs

Cada agregado tem um value object de ID dedicado, encapsulando um `Guid`. Isso evita misturar IDs de agregados diferentes no nível de tipo.

```csharp
public record MaterialId(Guid Value) : IEntityId;
```

Conversores customizados de EF e JSON tratam a serialização de forma transparente.

## SmartEnum

Enums com comportamento usam `SmartEnum` em vez de enums C# simples. Conversores customizados tratam o armazenamento em JSON e EF.

```csharp
public sealed class TransactionType : SmartEnum<TransactionType>
{
    public static readonly TransactionType Inbound = new(nameof(Inbound), 1);
    public static readonly TransactionType Outbound = new(nameof(Outbound), 2);
}
```

## Unit of Work

`BaseUnitOfWork<TContext>` encapsula o `DbContext` do EF. Em `CompleteAsync()`, ele:
1. Popula os campos de auditoria (CreatedAt, UpdatedAt, IsDeleted, etc.)
2. Publica os domain events disparados pelos agregados
3. Confirma a transação

## Soft Delete

Entidades que implementam `IDeletionAuditable` nunca são excluídas fisicamente. O `BaseUnitOfWork` define `IsDeleted = true`, `RemovedAt` e `RemovedBy` em vez disso.

## Paginação

Todos os endpoints de listagem aceitam um `PagedRequest` (número da página + tamanho da página) e retornam um `PagedResponse<T>`. `QueryableSearchExtensions` fornece helpers LINQ.

## Endpoints Minimal API

Os endpoints são agrupados com `MapGroup()` e definidos em métodos de extensão por recurso. A autorização é declarada inline.

```csharp
group.MapPost("/", CreateMaterialHandler)
     .RequireAuthorization(Permissions.Inventory.MaterialsManage);
```

## Autorização baseada em permissões

Uma classe estática `Permissions` define constantes de string hierárquicas no formato `"Module.Feature.Action"`.

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

## Tratamento global de exceções

Um middleware `GlobalExceptionHandler` captura exceções não tratadas e retorna respostas `ProblemDetails` (RFC 9457) de forma consistente em toda a API. `DomainException` mapeia para 422; erros inesperados mapeiam para 500.
