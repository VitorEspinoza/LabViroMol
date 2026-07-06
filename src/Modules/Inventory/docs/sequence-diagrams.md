# Sequence Diagrams — Inventory Module

**English** · [Português](./sequence-diagrams.pt-BR.md)

This document gathers the 7 sequence diagrams of the **Inventory** module: **Consume Material for
Project**, **Create Purchase Order**, **Process Order**, **Receive Order**,
**Cancel Order**, **Add Stock by Exception**, and **Remove Stock by
Exception**. They cover the full lifecycle of the central `Order` aggregate and the stock
movement operations of the `Material` aggregate, since these are the system's core modules.
They follow the same conventions (`autonumber`, solid/dashed
arrows for calls/returns, `alt`/`else` blocks for conditional business rules, `loop`
blocks for domain event publication in
`BaseUnitOfWork.CompleteAsync()`, `Note over` only for module boundaries and business
rules that manifest as flow branching).

---

## 1. Consume Material for Project

Sources: `src/Modules/Inventory/Presentation/Materials/MaterialStockEndpoints.cs`, `src/Modules/Inventory/Application/Materials/Commands/ConsumeForProject/{ConsumeMaterialForProjectCommand,ConsumeMaterialForProjectCommandHandler,ConsumeMaterialForProjectValidator}.cs`, `src/Modules/Research/Contracts/IProjectChecker.cs`, `src/Modules/Inventory/Domain/Materials/{Material,StockTransaction,LowStockDomainEvent}.cs`, `src/Modules/Inventory/Application/Materials/EventHandlers/LowStockEventHandler.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as MaterialStockEndpoints
 participant Mediator as IMediator
 participant Handler as ConsumeMaterialForProjectCommandHandler
 participant MatRepo as IMaterialRepository
 participant ProjectChecker as IProjectChecker (Research.Contracts)
 participant Material as Material (Domain)
 participant StockTx as StockTransaction (Domain)
 participant UoW as IInventoryUnitOfWork
 participant Observers as Domain Event Handlers

 Admin->>Endpoint: POST /api/inventory/materials/{id}/write-off (ProjectId provided)
 Endpoint->>Endpoint: hasProjectAssociated = true -> ConsumeMaterialForProjectCommand
 Endpoint->>Mediator: Send(ConsumeMaterialForProjectCommand)
 Note over Mediator: Runs the Validation Pipeline (FluentValidation) — MaterialId/ProjectId not empty, Quantity > 0
 Mediator->>+Handler: Handle(command)

 Handler->>MatRepo: Looks up Material by Id
 MatRepo-->>Handler: Material
 Note over Handler: Returns Result.NotFound if it does not exist

 Note over Handler,ProjectChecker: Inventory -> Research boundary (via Contracts) — SYNCHRONOUS cross-module call, not a domain event
 Handler->>ProjectChecker: IsEligibleForConsumptionAsync(ProjectId, currentUser.Id)
 ProjectChecker-->>Handler: Result
 alt Project not eligible for consumption
 Handler-->>Mediator: Result (checker error)
 else Project eligible
 Handler->>+Material: ConsumeForProject(ProjectId, Quantity, userId)
 alt Quantity > StockQuantity
 Material-->>Handler: Result.BusinessRule("Insufficient quantity for the project's consumption.")
 else Sufficient quantity
 Material->>StockTx: StockTransaction.CreateProjectConsumption(materialId, projectId, quantity, userId)
 StockTx-->>Material: StockTransaction
 Material->>Material: StockQuantity -= quantity
 Material->>Material: CheckMinStockThreshold()
 alt StockQuantity <= MinStock
 Material->>Material: AddEvent(LowStockDomainEvent(Id, Name, StockQuantity))
 end
 Material-->>-Handler: Result.Success()
 end

 alt Result.IsFailure (insufficient stock)
 Handler-->>Mediator: Result (error) — CompleteAsync is NOT called
 else Result.IsSuccess
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(LowStockDomainEvent) [if triggered]
 Note over Observers: LowStockEventHandler triggers an internal notification (ISendNotification) only if the event was added to the buffer
 UoW->>UoW: Auditing + SaveChangesAsync()
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 204 No Content
```

**Highlighted business rule:** the project eligibility check via `IProjectChecker.IsEligibleForConsumptionAsync` is a SYNCHRONOUS cross-module call (Inventory consumes a contract implemented by Research.Infrastructure via DI), unlike the asynchronous domain-event pattern used between aggregates within the same module. The firing of `LowStockDomainEvent` is conditional: it only occurs if, after consumption, `StockQuantity <= MinStock`.

---

## 2. Create Purchase Order

Sources: `src/Modules/Inventory/Presentation/Orders/OrderEndpoints.cs`, `src/Modules/Inventory/Application/Orders/Commands/Create/{CreateOrderCommand,CreateOrderHandler,CreateOrderValidator}.cs`, `src/Modules/Inventory/Domain/Orders/{Order,OrderStatus}.cs`, `src/Modules/Research/Contracts/IProjectChecker.cs`, `src/Modules/Inventory/Application/Orders/Commands/FixDetails/{FixOrderDetailsCommand,FixOrderDetailsHandler}.cs` (note).

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as OrderEndpoints
 participant Mediator as IMediator
 participant Handler as CreateOrderHandler
 participant MatRepo as IMaterialRepository
 participant ProjectChecker as IProjectChecker (Research.Contracts)
 participant Order as Order (Domain Aggregate)
 participant OrderRepo as IOrderRepository
 participant UoW as IInventoryUnitOfWork

 Admin->>Endpoint: POST /api/inventory/orders
 Endpoint->>Mediator: Send(CreateOrderCommand)
 Note over Mediator: Runs the Validation Pipeline (FluentValidation) — MaterialId/ProjectId required, Quantity > 0, description up to 500 chars
 Mediator->>+Handler: Handle(command)

 Handler->>MatRepo: Looks up Material by Id
 MatRepo-->>Handler: Material
 Note over Handler: Returns Result.NotFound if it does not exist

 Note over Handler,ProjectChecker: Inventory -> Research boundary (via Contracts)
 Handler->>ProjectChecker: IsEligibleForOrdersAsync(ProjectId)
 ProjectChecker-->>Handler: Result
 alt Project not eligible for orders
 Handler-->>Mediator: Result (checker error)
 else Project eligible
 Handler->>+Order: Order.Create(MaterialId, ProjectId, Quantity, description)
 Order-->>-Handler: Order (initial Status = Pending)
 Handler->>OrderRepo: AddAsync(order)
 Handler->>+UoW: CompleteAsync()
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 201 Created
```

**Highlighted business rule:** the exact validation order is: Material lookup (if it does not exist, `NotFound`) → `IsEligibleForOrdersAsync(ProjectId)` (if it fails, returns the checker's error) → `Order.Create(...)`. Unlike `Schedule.Create`, `Order.Create` does not trigger any domain event — there is no `CompleteAsync` publishing events in this flow, the pipeline goes straight to auditing + `SaveChangesAsync()`; the order's lifecycle only generates events on the `Receive` transition (see Diagram 4). *Side note:* `FixOrderDetailsCommand` (`PUT /{id}/fix-details`) follows a similar design (looks up `Order`, `IsEligibleForOrdersAsync` for the new `ProjectId`, `order.FixDetails`), but it is only allowed if `Order.Status == Pending`, otherwise it returns a `BusinessRule` for details not corrected — there is not enough logic to justify a diagram of its own.

---

## 3. Process Order

Sources: `src/Modules/Inventory/Presentation/Orders/OrderEndpoints.cs`, `src/Modules/Inventory/Application/Orders/Commands/Process/{ProcessOrderCommand,ProcessOrderCommandHandler,ProcessOrderCommandValidator}.cs`, `src/Modules/Inventory/Domain/Orders/{Order,OrderProcessing}.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as OrderEndpoints
 participant Mediator as IMediator
 participant Handler as ProcessOrderCommandHandler
 participant OrderRepo as IOrderRepository
 participant Order as Order (Domain Aggregate)
 participant UoW as IInventoryUnitOfWork

 Admin->>Endpoint: POST /api/inventory/orders/{id}/process
 Endpoint->>Mediator: Send(ProcessOrderCommand)
 Note over Mediator: Runs the Validation Pipeline (FluentValidation)
 Mediator->>+Handler: Handle(command)
 Handler->>OrderRepo: Looks up Order by Id
 OrderRepo-->>Handler: Order
 Note over Handler: Returns Result.NotFound if it does not exist

 Handler->>+Order: Process(processedBy, processedByName, notes)
 alt Status != Pending
 Order-->>Handler: Result.BusinessRule("Only pending orders can be processed.")
 else Status == Pending
 Order->>Order: Status = Processing
 Order->>Order: Processing = new OrderProcessing(processedBy, processedByName, UtcNow, notes)
 Order-->>-Handler: Result.Success()
 end
 alt Result.IsFailure
 Handler-->>Mediator: Result (error) — CompleteAsync is NOT called
 else Result.IsSuccess
 Handler->>UoW: CompleteAsync()
 Handler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 204 No Content
```

**Highlighted business rule:** `Order.Process` only allows the transition when `Status == Pending`, changing it to `Processing` and recording an `OrderProcessing` (who processed it, when, notes). This transition does NOT trigger any domain event — `CompleteAsync()` here merely persists standard auditing (`CreatedAt`/`CreatedBy`) via `SaveChangesAsync()`, without publishing domain events.

---

## 4. Receive Order

Sources: `src/Modules/Inventory/Presentation/Orders/OrderEndpoints.cs`, `src/Modules/Inventory/Application/Orders/Commands/Receive/{ReceiveOrderCommand,ReceiveOrderCommandHandler,ReceiveOrderCommandValidator}.cs`, `src/Modules/Inventory/Domain/Orders/{Order,OrderReceipt,OrderReceivedDomainEvent}.cs`, `src/Modules/Inventory/Application/Materials/EventHandlers/OrderReceivedEventHandler.cs`, `src/Modules/Inventory/Domain/Materials/{Material,StockTransaction}.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as OrderEndpoints
 participant Mediator as IMediator
 participant Handler as ReceiveOrderCommandHandler
 participant OrderRepo as IOrderRepository
 participant Order as Order (Domain Aggregate)
 participant UoW as IInventoryUnitOfWork
 participant Observers as Domain Event Handlers
 participant MatRepo as IMaterialRepository
 participant Material as Material (Domain)
 participant StockTx as StockTransaction (Domain)

 Admin->>Endpoint: POST /api/inventory/orders/{id}/receive
 Endpoint->>Mediator: Send(ReceiveOrderCommand)
 Note over Mediator: Runs the Validation Pipeline (FluentValidation) — QuantityReceived > 0
 Mediator->>+Handler: Handle(command)
 Handler->>OrderRepo: Looks up Order by Id
 OrderRepo-->>Handler: Order
 Note over Handler: Returns Result.NotFound if it does not exist

 Handler->>+Order: Receive(receivedBy, receivedByName, quantityReceived, notes)
 alt Status != Processing
 Order-->>Handler: Result.BusinessRule("Only orders in processing can be received.")
 else Status == Processing
 Order->>Order: Status = Completed
 Order->>Order: Receipt = new OrderReceipt(receivedBy, receivedByName, notes, quantityReceived, UtcNow)
 Order->>Order: AddEvent(OrderReceivedDomainEvent(Id, MaterialId, quantityReceived, receivedBy))
 Order-->>-Handler: Result.Success()
 end
 alt Result.IsFailure
 Handler-->>Mediator: Result (error) — CompleteAsync is NOT called
 else Result.IsSuccess
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(OrderReceivedDomainEvent)
 activate Observers
 Note over Observers: INTRA-module cascade effect (Inventory) via domain event, within the SAME CompleteAsync — OrderReceivedEventHandler updates another aggregate (Material)
 Observers->>MatRepo: GetByIdAsync(MaterialId) — second Material lookup, inside the event handler
 MatRepo-->>Observers: Material?
 Observers->>+Material: ReceiveFromOrder(OrderId, QuantityReceived, ReceivedBy)
 Material->>StockTx: StockTransaction.CreateReceipt(Id, orderId, quantity, userId)
 StockTx-->>Material: StockTransaction
 Material->>Material: StockQuantity += quantity
 Material-->>-Observers: Result.Success()
 deactivate Observers
 UoW->>UoW: Auditing + SaveChangesAsync()
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 204 No Content
```

**Highlighted business rule:** only `Receive` (unlike `Process`, see Diagram 3) triggers `OrderReceivedDomainEvent`, which is consumed by `OrderReceivedEventHandler` within the SAME `CompleteAsync` of the receipt phase, updating the `Material` aggregate (intra-module cascade effect via event, not a direct call). `Material.ReceiveFromOrder` NEVER calls `CheckMinStockThreshold()` — receiving an order never triggers `LowStockDomainEvent`, since it is a stock inflow; only outflows (consumption for project, write-off by exception) check the minimum threshold.

---

## 5. Cancel Order

Sources: `src/Modules/Inventory/Presentation/Orders/OrderEndpoints.cs`, `src/Modules/Inventory/Application/Orders/Commands/Cancel/{CancelOrderCommand,CancelOrderCommandHandler,CancelOrderCommandValidator}.cs`, `src/Modules/Inventory/Domain/Orders/Order.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as OrderEndpoints
 participant Mediator as IMediator
 participant Handler as CancelOrderCommandHandler
 participant OrderRepo as IOrderRepository
 participant Order as Order (Domain Aggregate)
 participant UoW as IInventoryUnitOfWork

 Admin->>Endpoint: POST /api/inventory/orders/{id}/cancel
 Endpoint->>Mediator: Send(CancelOrderCommand)
 Note over Mediator: Runs the Validation Pipeline (FluentValidation) — OrderId not empty
 Mediator->>+Handler: Handle(command)
 Handler->>OrderRepo: Looks up Order by Id
 OrderRepo-->>Handler: Order
 Note over Handler: Returns Result.NotFound if it does not exist

 Handler->>+Order: Cancel()
 alt Status != Pending
 Order-->>Handler: Result.BusinessRule("Only pending orders can be canceled.")
 else Status == Pending
 Order->>Order: Status = Canceled
 Order-->>-Handler: Result.Success()
 end
 alt Result.IsFailure
 Handler-->>Mediator: Result (error) — CompleteAsync is NEVER called on the failure path
 else Result.IsSuccess
 Handler->>+UoW: CompleteAsync()
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 204 No Content
```

**Highlighted business rule:** `Order.Cancel()` only allows the transition when `Status == Pending` — orders in `Processing` (already processed) or `Completed` (already received) are immutable regarding cancellation and can no longer be reverted. The transition does not trigger any domain event. On the failure path, `_unitOfWork.CompleteAsync()` is never invoked, so no changes are persisted.

---

## 6. Add Stock by Exception

Sources: `src/Modules/Inventory/Presentation/Materials/MaterialStockEndpoints.cs`, `src/Modules/Inventory/Application/Materials/Commands/AddStockException/{AddStockMaterialExceptionCommand,AddStockMaterialExceptionHandler,AddStockMaterialExceptionValidator}.cs`, `src/Modules/Inventory/Domain/Materials/{Material,StockTransaction}.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as MaterialStockEndpoints
 participant Mediator as IMediator
 participant AddHandler as AddStockMaterialExceptionHandler
 participant MatRepo as IMaterialRepository
 participant Material as Material (Domain)
 participant StockTx as StockTransaction (Domain)
 participant UoW as IInventoryUnitOfWork

 Admin->>Endpoint: POST /api/inventory/materials/{id}/add-stock
 Endpoint->>Mediator: Send(AddStockMaterialExceptionCommand)
 Note over Mediator: Runs the Validation Pipeline (FluentValidation) — Quantity > 0, Reason >= 10 chars
 Mediator->>+AddHandler: Handle(command)
 AddHandler->>MatRepo: Looks up Material by Id
 MatRepo-->>AddHandler: Material
 Note over AddHandler: Returns Result.NotFound if it does not exist

 AddHandler->>+Material: AddStockException(quantity, reason, userId)
 Material->>StockTx: StockTransaction.CreateExceptionIn(materialId, quantity, justification, userId)
 StockTx-->>Material: StockTransaction
 Material->>Material: StockQuantity += quantity
 Material-->>-AddHandler: (void)
 AddHandler->>+UoW: CompleteAsync()
 UoW-->>-AddHandler: OK
 AddHandler-->>-Mediator: Result.Success()
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 204 No Content
```

**Highlighted business rule:** `Material.AddStockException` is a `void` method — it never fails and does NOT check the minimum stock threshold, unlike `RemoveStockException` (Diagram 7), since a stock inflow never triggers `LowStockDomainEvent`. Because of this, this flow has no domain event to publish in `CompleteAsync()`, unlike the "remove" counterpart of this same exception operation.

---

## 7. Remove Stock by Exception

Sources: `src/Modules/Inventory/Presentation/Materials/MaterialStockEndpoints.cs`, `src/Modules/Inventory/Application/Materials/Commands/RemoveStockException/{RemoveStockMaterialExceptionCommand,RemoveStockMaterialExceptionHandler,RemoveStockMaterialExceptionValidator}.cs`, `src/Modules/Inventory/Domain/Materials/{Material,StockTransaction,LowStockDomainEvent}.cs`, `src/Modules/Inventory/Application/Materials/EventHandlers/LowStockEventHandler.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as MaterialStockEndpoints
 participant Mediator as IMediator
 participant RemoveHandler as RemoveStockMaterialExceptionHandler
 participant MatRepo as IMaterialRepository
 participant Material as Material (Domain)
 participant StockTx as StockTransaction (Domain)
 participant UoW as IInventoryUnitOfWork
 participant Observers as Domain Event Handlers

 Admin->>Endpoint: POST /api/inventory/materials/{id}/write-off (without ProjectId)
 Endpoint->>Endpoint: hasProjectAssociated = false -> RemoveStockMaterialExceptionCommand
 Endpoint->>Mediator: Send(RemoveStockMaterialExceptionCommand)
 Note over Mediator: Runs the Validation Pipeline (FluentValidation) — Quantity > 0, Reason >= 10 chars
 Mediator->>+RemoveHandler: Handle(command)
 RemoveHandler->>MatRepo: Looks up Material by Id
 MatRepo-->>RemoveHandler: Material
 Note over RemoveHandler: Returns Result.NotFound if it does not exist

 RemoveHandler->>+Material: RemoveStockException(quantity, justification, userId)
 alt quantity > StockQuantity
 Material-->>RemoveHandler: Result.BusinessRule("Insufficient quantity to perform this write-off.")
 else Sufficient quantity
 Material->>StockTx: StockTransaction.CreateExceptionOut(materialId, quantity, justification, userId)
 StockTx-->>Material: StockTransaction
 Material->>Material: StockQuantity -= quantity
 Material->>Material: CheckMinStockThreshold()
 alt StockQuantity <= MinStock
 Material->>Material: AddEvent(LowStockDomainEvent(Id, Name, StockQuantity))
 end
 Material-->>-RemoveHandler: Result.Success()
 end
 alt Result.IsFailure
 RemoveHandler-->>Mediator: Result (error) — CompleteAsync is NOT called
 else Result.IsSuccess
 RemoveHandler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(LowStockDomainEvent) [if triggered]
 Note over Observers: LowStockEventHandler triggers an internal notification (ISendNotification) — the same handler used in the project consumption flow (Diagram 1)
 UoW-->>-RemoveHandler: OK
 RemoveHandler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 204 No Content
```

**Highlighted business rule:** a clear asymmetry compared to the add operation (Diagram 6) — `RemoveStockException` returns a `Result` (it can fail due to insufficient quantity) and, on success, calls `CheckMinStockThreshold()`, potentially triggering the SAME `LowStockDomainEvent` and the SAME `LowStockEventHandler` already used in the project consumption flow (Diagram 1).
