# Entity-Relationship Diagram — `assets` Schema

**English** · [Português](./er-diagram.pt-BR.md)

This document presents the `assets` schema block. It models the persistence layer (real physical tables) of the `Equipment` and `MaintenanceRequest` aggregates.

DbContext: `AssetsDbContext`. Both tables are `AggregateRoot` + `IFullAuditable`
(full creation, modification and soft delete tracking).

```mermaid
erDiagram
 Equipments {
 uuid Id PK
 varchar Name
 varchar Brand
 varchar Model
 varchar Code
 varchar Description
 text ImageUrl
 varchar Location
 text Translations "Serialized JSON (Dictionary of EquipmentTranslation)"
 timestamptz CreatedAt
 uuid CreatedBy
 boolean IsDeleted
 timestamptz RemovedAt
 uuid RemovedBy
 timestamptz UpdatedAt
 uuid UpdatedBy
 }

 MaintenanceRequests {
 uuid Id PK
 text Status "MaintenanceRequestStatus enum as string"
 text Description
 text ProblemDescription
 uuid EquipmentId FK
 timestamptz CreatedAt
 uuid CreatedBy
 boolean IsDeleted
 timestamptz RemovedAt
 uuid RemovedBy
 timestamptz UpdatedAt
 uuid UpdatedBy
 }

 Equipments ||--o{ MaintenanceRequests: "1:N (FK_MaintenanceRequests_Equipments_EquipmentId, restrict)"
```

> Note: `MaintenanceRequest.EquipmentId` now has a real database FK constraint —
> `FK_MaintenanceRequests_Equipments_EquipmentId` (`ON DELETE RESTRICT`), added via a migration
> (not yet applied to any environment). It used to be a plain required
> `uuid` column with no `HasOne`/`HasForeignKey` in `MaintenanceRequestConfiguration`
> nor a constraint in the `Assets_InitialSetup` migration.
