# Diagrama de Classes — Módulo Inventory

[English](./class-diagram.md) · **Português**

Este documento extrai a seção específica do módulo **Inventory**, cobrindo exclusivamente a camada Domain: os aggregate roots `MaterialType`,
`Material`, `Kit` e `Order`, a entidade filha `StockTransaction`, os value objects
(`KitItem`, `OrderProcessing`, `OrderReceipt`, `ProjectId`) e os enums (`TransactionType`,
`Unit`, `OrderStatus`).

```mermaid
classDiagram
 class MaterialType {
 +Name: string
 +Active: bool
 +DeactivatedBy: UserId
 +DeactivatedAt: DateTimeOffset
 +Create(name) MaterialType
 +Deactivate(userId) void
 +Activate(userId) void
 }
 MaterialType --|> AggregateRoot~MaterialTypeId~
 MaterialType..|> ICreationAuditable
 MaterialType..|> IModificationAuditable

 class Material {
 +Name: string
 +StockQuantity: Quantity
 +Unit: Unit
 +MinStock: Quantity
 +Location: string
 +TypeId: MaterialTypeId
 +Create(name, location, minStock, stockQuantity, unit, type) Result~Material~
 +ReceiveFromOrder(orderId, quantity, userId) Result
 +ConsumeForProject(projectId, quantity, userId) Result
 +AddStockException(quantity, justification, userId) void
 +RemoveStockException(quantity, justification, userId) Result
 }
 Material --|> AggregateRoot~MaterialId~
 Material..|> ICreationAuditable
 Material..|> IModificationAuditable
 Material "1" --> "1" MaterialType: TypeId

 class StockTransaction {
 +MaterialId: MaterialId
 +OrderId: OrderId
 +ProjectId: ProjectId
 +Quantity: Quantity
 +Type: TransactionType
 +TransactedAt: DateTime
 +TransactedByUserId: UserId
 +Justification: string
 }
 StockTransaction --|> BaseEntity~StockTransactionId~
 Material "1" *-- "many" StockTransaction: transactions
 StockTransaction "0..1" --> "0..1" Order: OrderId (opcional)
 StockTransaction..> ProjectId: ProjectId (referencia Research, cross-module)

 class TransactionType {
 <<enumeration>>
 OrderReceipt
 ProjectConsumption
 ExceptionIn
 ExceptionOut
 }
 StockTransaction --> TransactionType: Type

 class Unit {
 <<enumeration>>
 Gram
 Milliliter
 Piece
 }
 Material --> Unit: Unit

 class Kit {
 +Name: string
 +Description: string
 +Create(name, description, initialItems) Kit
 +UpdateMetadata(name, description) void
 +DefineMaterials(newItems) void
 }
 Kit --|> AggregateRoot~KitId~
 Kit..|> ICreationAuditable
 Kit..|> IModificationAuditable

 class KitItem {
 <<value object>>
 +MaterialId: MaterialId
 +Quantity: Quantity
 }
 Kit "1" *-- "many" KitItem: materials
 KitItem "many" --> "1" Material: MaterialId

 class Order {
 +MaterialId: MaterialId
 +ProjectId: ProjectId
 +Status: OrderStatus
 +RequestedQuantity: Quantity
 +Processing: OrderProcessing
 +Receipt: OrderReceipt
 +Description: string
 +Create(materialId, projectId, quantity, description) Order
 +FixDetails(newProjectId, newRequestedQuantity, description) Result
 +Process(processedBy, processedByName, processingNotes) Result
 +Receive(receivedBy, receivedByName, quantityReceived, receiptNotes) Result
 +Cancel() Result
 }
 Order --|> AggregateRoot~OrderId~
 Order..|> ICreationAuditable
 Order..|> IModificationAuditable
 Order "1" --> "1" Material: MaterialId
 Order..> ProjectId: ProjectId (referencia Research, cross-module)

 class OrderStatus {
 <<enumeration>>
 Pending
 Processing
 Completed
 Canceled
 }
 Order --> OrderStatus: Status

 class OrderProcessing {
 <<value object>>
 +ProcessedBy: UserId
 +ProcessedByName: string
 +ProcessedAt: DateTimeOffset
 +Notes: string
 }
 Order "1" --> "0..1" OrderProcessing: Processing

 class OrderReceipt {
 <<value object>>
 +ReceivedBy: UserId
 +ReceivedByName: string
 +Notes: string
 +Quantity: Quantity
 +ReceivedAt: DateTimeOffset
 }
 Order "1" --> "0..1" OrderReceipt: Receipt

 class ProjectId {
 <<value object / Inventory.References>>
 +Value: Guid
 }
 note for ProjectId "Strong Id local ao módulo Inventory.\nDistinto de Research.Domain.Projects.ProjectId.\nFormaliza a referência fraca cross-module."
```
