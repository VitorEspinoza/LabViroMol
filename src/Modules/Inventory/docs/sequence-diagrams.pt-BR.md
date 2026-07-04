# Diagramas de Sequência — Módulo Inventory

[English](./sequence-diagrams.md) · **Português**

Este documento reúne os 7 diagramas de sequência do módulo **Inventory**: **Consumir Material para
Projeto**, **Criar Pedido de Compra**, **Processar Pedido**, **Receber Pedido**,
**Cancelar Pedido**, **Adicionar Estoque por Exceção** e **Remover Estoque por
Exceção**. Cobrem o ciclo de vida completo do agregado central `Order` e as operações de
movimentação de estoque do agregado `Material`, por serem os módulos core do sistema.
Seguem as mesmas convenções (`autonumber`, setas
sólidas/tracejadas para chamadas/retornos, blocos `alt`/`else` para regras de negócio
condicionais, blocos `loop` para publicação de domain events em
`BaseUnitOfWork.CompleteAsync()`, `Note over` apenas para fronteiras de módulo e regras de
negócio que se manifestam como ramificação de fluxo).

---

## 1. Consumir Material para Projeto

Fontes: `src/Modules/Inventory/Presentation/Materials/MaterialStockEndpoints.cs`, `src/Modules/Inventory/Application/Materials/Commands/ConsumeForProject/{ConsumeMaterialForProjectCommand,ConsumeMaterialForProjectCommandHandler,ConsumeMaterialForProjectValidator}.cs`, `src/Modules/Research/Contracts/IProjectChecker.cs`, `src/Modules/Inventory/Domain/Materials/{Material,StockTransaction,LowStockDomainEvent}.cs`, `src/Modules/Inventory/Application/Materials/EventHandlers/LowStockEventHandler.cs`.

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

 Admin->>Endpoint: POST /api/inventory/materials/{id}/write-off (ProjectId informado)
 Endpoint->>Endpoint: hasProjectAssociated = true -> ConsumeMaterialForProjectCommand
 Endpoint->>Mediator: Send(ConsumeMaterialForProjectCommand)
 Note over Mediator: Executa Pipeline de Validação (FluentValidation) — MaterialId/ProjectId não vazios, Quantity > 0
 Mediator->>+Handler: Handle(command)

 Handler->>MatRepo: Busca Material por Id
 MatRepo-->>Handler: Material
 Note over Handler: Retorna Result.NotFound se não existir

 Note over Handler,ProjectChecker: Fronteira Inventory -> Research (via Contracts) — chamada cross-module SÍNCRONA, não é domain event
 Handler->>ProjectChecker: IsEligibleForConsumptionAsync(ProjectId, currentUser.Id)
 ProjectChecker-->>Handler: Result
 alt Projeto não elegível para consumo
 Handler-->>Mediator: Result (erro do checker)
 else Projeto elegível
 Handler->>+Material: ConsumeForProject(ProjectId, Quantity, userId)
 alt Quantity > StockQuantity
 Material-->>Handler: Result.BusinessRule("Quantidade insuficiente para o consumo do projeto.")
 else Quantidade suficiente
 Material->>StockTx: StockTransaction.CreateProjectConsumption(materialId, projectId, quantity, userId)
 StockTx-->>Material: StockTransaction
 Material->>Material: StockQuantity -= quantity
 Material->>Material: CheckMinStockThreshold()
 alt StockQuantity <= MinStock
 Material->>Material: AddEvent(LowStockDomainEvent(Id, Name, StockQuantity))
 end
 Material-->>-Handler: Result.Success()
 end

 alt Result.IsFailure (estoque insuficiente)
 Handler-->>Mediator: Result (erro) — CompleteAsync NÃO é chamado
 else Result.IsSuccess
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(LowStockDomainEvent) [se disparado]
 Note over Observers: LowStockEventHandler dispara notificação interna (ISendNotification) somente se o evento foi adicionado ao buffer
 UoW->>UoW: Auditoria + SaveChangesAsync()
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 204 No Content
```

**Regra de negócio em destaque:** a checagem de elegibilidade do projeto via `IProjectChecker.IsEligibleForConsumptionAsync` é uma chamada cross-module SÍNCRONA (Inventory consome um contrato implementado por Research.Infrastructure via DI), diferente do padrão assíncrono via domain events usado entre agregados do mesmo módulo. O disparo de `LowStockDomainEvent` é condicional: só ocorre se, após o consumo, `StockQuantity <= MinStock`.

---

## 2. Criar Pedido de Compra

Fontes: `src/Modules/Inventory/Presentation/Orders/OrderEndpoints.cs`, `src/Modules/Inventory/Application/Orders/Commands/Create/{CreateOrderCommand,CreateOrderHandler,CreateOrderValidator}.cs`, `src/Modules/Inventory/Domain/Orders/{Order,OrderStatus}.cs`, `src/Modules/Research/Contracts/IProjectChecker.cs`, `src/Modules/Inventory/Application/Orders/Commands/FixDetails/{FixOrderDetailsCommand,FixOrderDetailsHandler}.cs` (nota).

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
 Note over Mediator: Executa Pipeline de Validação (FluentValidation) — MaterialId/ProjectId obrigatórios, Quantity > 0, description até 500 chars
 Mediator->>+Handler: Handle(command)

 Handler->>MatRepo: Busca Material por Id
 MatRepo-->>Handler: Material
 Note over Handler: Retorna Result.NotFound se não existir

 Note over Handler,ProjectChecker: Fronteira Inventory -> Research (via Contracts)
 Handler->>ProjectChecker: IsEligibleForOrdersAsync(ProjectId)
 ProjectChecker-->>Handler: Result
 alt Projeto não elegível para pedidos
 Handler-->>Mediator: Result (erro do checker)
 else Projeto elegível
 Handler->>+Order: Order.Create(MaterialId, ProjectId, Quantity, description)
 Order-->>-Handler: Order (Status inicial = Pending)
 Handler->>OrderRepo: AddAsync(order)
 Handler->>+UoW: CompleteAsync()
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 201 Created
```

**Regra de negócio em destaque:** a ordem exata de validação é busca de `Material` (se não existir, `NotFound`) → `IsEligibleForOrdersAsync(ProjectId)` (se falha, retorna erro do checker) → `Order.Create(...)`. Diferente de `Schedule.Create`, `Order.Create` não dispara nenhum domain event — não há `CompleteAsync` publicando eventos neste fluxo, o pipeline executa direto auditoria + `SaveChangesAsync()`; o ciclo de vida do pedido só gera eventos na transição `Receive` (ver Diagrama 4). *Nota satélite:* `FixOrderDetailsCommand` (`PUT /{id}/fix-details`) segue um desenho semelhante (busca `Order`, `IsEligibleForOrdersAsync` para o novo `ProjectId`, `order.FixDetails`), mas só é permitido se `Order.Status == Pending`, senão retorna `BusinessRule` de detalhes não corrigidos — não há lógica suficiente para justificar diagrama próprio.

---

## 3. Processar Pedido

Fontes: `src/Modules/Inventory/Presentation/Orders/OrderEndpoints.cs`, `src/Modules/Inventory/Application/Orders/Commands/Process/{ProcessOrderCommand,ProcessOrderCommandHandler,ProcessOrderCommandValidator}.cs`, `src/Modules/Inventory/Domain/Orders/{Order,OrderProcessing}.cs`.

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
 Note over Mediator: Executa Pipeline de Validação (FluentValidation)
 Mediator->>+Handler: Handle(command)
 Handler->>OrderRepo: Busca Order por Id
 OrderRepo-->>Handler: Order
 Note over Handler: Retorna Result.NotFound se não existir

 Handler->>+Order: Process(processedBy, processedByName, notes)
 alt Status != Pending
 Order-->>Handler: Result.BusinessRule("Apenas pedidos pendentes podem ser processados.")
 else Status == Pending
 Order->>Order: Status = Processing
 Order->>Order: Processing = new OrderProcessing(processedBy, processedByName, UtcNow, notes)
 Order-->>-Handler: Result.Success()
 end
 alt Result.IsFailure
 Handler-->>Mediator: Result (erro) — CompleteAsync NÃO é chamado
 else Result.IsSuccess
 Handler->>UoW: CompleteAsync()
 Handler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 204 No Content
```

**Regra de negócio em destaque:** `Order.Process` só permite a transição quando `Status == Pending`, mudando para `Processing` e registrando `OrderProcessing` (quem processou, quando, notas). Essa transição NÃO dispara nenhum domain event — `CompleteAsync()` aqui apenas persiste a auditoria padrão (`CreatedAt`/`CreatedBy`) via `SaveChangesAsync()`, sem publicar eventos de domínio.

---

## 4. Receber Pedido

Fontes: `src/Modules/Inventory/Presentation/Orders/OrderEndpoints.cs`, `src/Modules/Inventory/Application/Orders/Commands/Receive/{ReceiveOrderCommand,ReceiveOrderCommandHandler,ReceiveOrderCommandValidator}.cs`, `src/Modules/Inventory/Domain/Orders/{Order,OrderReceipt,OrderReceivedDomainEvent}.cs`, `src/Modules/Inventory/Application/Materials/EventHandlers/OrderReceivedEventHandler.cs`, `src/Modules/Inventory/Domain/Materials/{Material,StockTransaction}.cs`.

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
 Note over Mediator: Executa Pipeline de Validação (FluentValidation) — QuantityReceived > 0
 Mediator->>+Handler: Handle(command)
 Handler->>OrderRepo: Busca Order por Id
 OrderRepo-->>Handler: Order
 Note over Handler: Retorna Result.NotFound se não existir

 Handler->>+Order: Receive(receivedBy, receivedByName, quantityReceived, notes)
 alt Status != Processing
 Order-->>Handler: Result.BusinessRule("Apenas pedidos em processamento podem ser recebidos.")
 else Status == Processing
 Order->>Order: Status = Completed
 Order->>Order: Receipt = new OrderReceipt(receivedBy, receivedByName, notes, quantityReceived, UtcNow)
 Order->>Order: AddEvent(OrderReceivedDomainEvent(Id, MaterialId, quantityReceived, receivedBy))
 Order-->>-Handler: Result.Success()
 end
 alt Result.IsFailure
 Handler-->>Mediator: Result (erro) — CompleteAsync NÃO é chamado
 else Result.IsSuccess
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(OrderReceivedDomainEvent)
 activate Observers
 Note over Observers: Efeito cascata INTRA-módulo (Inventory) via domain event, no MESMO CompleteAsync — OrderReceivedEventHandler atualiza outro agregado (Material)
 Observers->>MatRepo: GetByIdAsync(MaterialId) — segunda busca de Material, dentro do event handler
 MatRepo-->>Observers: Material?
 Observers->>+Material: ReceiveFromOrder(OrderId, QuantityReceived, ReceivedBy)
 Material->>StockTx: StockTransaction.CreateReceipt(Id, orderId, quantity, userId)
 StockTx-->>Material: StockTransaction
 Material->>Material: StockQuantity += quantity
 Material-->>-Observers: Result.Success()
 deactivate Observers
 UoW->>UoW: Auditoria + SaveChangesAsync()
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 204 No Content
```

**Regra de negócio em destaque:** só `Receive` (diferente de `Process`, ver Diagrama 3) dispara `OrderReceivedDomainEvent`, que é consumido pelo `OrderReceivedEventHandler` dentro do MESMO `CompleteAsync` da fase de recebimento, atualizando o agregado `Material` (efeito cascata intra-módulo via evento, não chamada direta). `Material.ReceiveFromOrder` NUNCA chama `CheckMinStockThreshold()` — recebimento de pedido nunca dispara `LowStockDomainEvent`, pois é entrada de estoque; somente saídas (consumo por projeto, baixa por exceção) verificam o limiar mínimo.

---

## 5. Cancelar Pedido

Fontes: `src/Modules/Inventory/Presentation/Orders/OrderEndpoints.cs`, `src/Modules/Inventory/Application/Orders/Commands/Cancel/{CancelOrderCommand,CancelOrderCommandHandler,CancelOrderCommandValidator}.cs`, `src/Modules/Inventory/Domain/Orders/Order.cs`.

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
 Note over Mediator: Executa Pipeline de Validação (FluentValidation) — OrderId não vazio
 Mediator->>+Handler: Handle(command)
 Handler->>OrderRepo: Busca Order por Id
 OrderRepo-->>Handler: Order
 Note over Handler: Retorna Result.NotFound se não existir

 Handler->>+Order: Cancel()
 alt Status != Pending
 Order-->>Handler: Result.BusinessRule("Apenas pedidos pendentes podem ser cancelados.")
 else Status == Pending
 Order->>Order: Status = Canceled
 Order-->>-Handler: Result.Success()
 end
 alt Result.IsFailure
 Handler-->>Mediator: Result (erro) — CompleteAsync NUNCA é chamado no caminho de falha
 else Result.IsSuccess
 Handler->>+UoW: CompleteAsync()
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 204 No Content
```

**Regra de negócio em destaque:** `Order.Cancel()` só permite a transição quando `Status == Pending` — pedidos em `Processing` (já processados) ou `Completed` (já recebidos) são imutáveis quanto a cancelamento e não podem mais retroceder. A transição não dispara nenhum domain event. No caminho de falha, `_unitOfWork.CompleteAsync()` nunca é invocado, então nenhuma alteração é persistida.

---

## 6. Adicionar Estoque por Exceção

Fontes: `src/Modules/Inventory/Presentation/Materials/MaterialStockEndpoints.cs`, `src/Modules/Inventory/Application/Materials/Commands/AddStockException/{AddStockMaterialExceptionCommand,AddStockMaterialExceptionHandler,AddStockMaterialExceptionValidator}.cs`, `src/Modules/Inventory/Domain/Materials/{Material,StockTransaction}.cs`.

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
 Note over Mediator: Executa Pipeline de Validação (FluentValidation) — Quantity > 0, Reason >= 10 chars
 Mediator->>+AddHandler: Handle(command)
 AddHandler->>MatRepo: Busca Material por Id
 MatRepo-->>AddHandler: Material
 Note over AddHandler: Retorna Result.NotFound se não existir

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

**Regra de negócio em destaque:** `Material.AddStockException` é um método `void` — nunca falha e NÃO verifica o limiar mínimo de estoque, ao contrário de `RemoveStockException` (Diagrama 7), pois entrada de estoque nunca dispara `LowStockDomainEvent`. Por isso este fluxo não tem nenhum domain event a publicar em `CompleteAsync()`, diferente do par "remover" desta mesma operação de exceção.

---

## 7. Remover Estoque por Exceção

Fontes: `src/Modules/Inventory/Presentation/Materials/MaterialStockEndpoints.cs`, `src/Modules/Inventory/Application/Materials/Commands/RemoveStockException/{RemoveStockMaterialExceptionCommand,RemoveStockMaterialExceptionHandler,RemoveStockMaterialExceptionValidator}.cs`, `src/Modules/Inventory/Domain/Materials/{Material,StockTransaction,LowStockDomainEvent}.cs`, `src/Modules/Inventory/Application/Materials/EventHandlers/LowStockEventHandler.cs`.

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

 Admin->>Endpoint: POST /api/inventory/materials/{id}/write-off (sem ProjectId)
 Endpoint->>Endpoint: hasProjectAssociated = false -> RemoveStockMaterialExceptionCommand
 Endpoint->>Mediator: Send(RemoveStockMaterialExceptionCommand)
 Note over Mediator: Executa Pipeline de Validação (FluentValidation) — Quantity > 0, Reason >= 10 chars
 Mediator->>+RemoveHandler: Handle(command)
 RemoveHandler->>MatRepo: Busca Material por Id
 MatRepo-->>RemoveHandler: Material
 Note over RemoveHandler: Retorna Result.NotFound se não existir

 RemoveHandler->>+Material: RemoveStockException(quantity, justification, userId)
 alt quantity > StockQuantity
 Material-->>RemoveHandler: Result.BusinessRule("Quantidade insuficiente para realizar esta baixa.")
 else Quantidade suficiente
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
 RemoveHandler-->>Mediator: Result (erro) — CompleteAsync NÃO é chamado
 else Result.IsSuccess
 RemoveHandler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(LowStockDomainEvent) [se disparado]
 Note over Observers: LowStockEventHandler dispara notificação interna (ISendNotification) — mesmo handler usado no consumo para projeto (Diagrama 1)
 UoW-->>-RemoveHandler: OK
 RemoveHandler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 204 No Content
```

**Regra de negócio em destaque:** assimetria clara em relação à operação de adição (Diagrama 6) — `RemoveStockException` retorna `Result` (pode falhar por quantidade insuficiente) e, em caso de sucesso, chama `CheckMinStockThreshold()`, podendo disparar a MESMA `LowStockDomainEvent` e o MESMO `LowStockEventHandler` já usados no consumo para projeto (Diagrama 1).
