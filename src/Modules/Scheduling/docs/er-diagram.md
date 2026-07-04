# Entity-Relationship Diagram — `scheduling` Schema

**English** · [Português](./er-diagram.pt-BR.md)

This document presents the block for the `scheduling` schema. It models the persistence layer (real physical tables) of the `Schedule` aggregate.

DbContext: `SchedulingDbContext`. `Schedule` implements only `IModificationAuditable`
(no `CreatedAt`/`CreatedBy` — the only aggregate in the system without creation auditing).

```mermaid
erDiagram
 Schedules {
 uuid Id PK
 text SchedulerName
 text SchedulerCourse
 text SchedulerEmail
 date SchedulingDate
 timestamptz SchedulingStartHour
 timestamptz SchedulingEndHour
 boolean AcceptTerm
 text AdvisorProfessor
 text ProjectTitle
 text Description
 text Status "ScheduleStatus enum as string"
 uuid ApprovedBy
 uuid RefusedBy
 text TermUrl
 text RefuseJustification
 timestamptz UpdatedAt
 uuid UpdatedBy
 }

 ScheduleEquipments {
 uuid Id PK
 uuid EquipmentId "cross-module, no FK, see legend"
 text EquipmentName "denormalized copy of the name at scheduling time"
 uuid ScheduleId FK
 }

 Schedules ||--o{ ScheduleEquipments: "1:N (FK_ScheduleEquipments_Schedules_ScheduleId, cascade)"
```

> Note: `Schedules` has no creation columns (`CreatedAt`/`CreatedBy`) nor soft
> delete — confirmed in the migration (`Schedule: IModificationAuditable` only, without
> `ICreationAuditable`/`IDeletionAuditable`). `ScheduleEquipments.EquipmentId` references
> `assets.Equipments` (another schema/module) with no database FK — just a copied Guid + name.
