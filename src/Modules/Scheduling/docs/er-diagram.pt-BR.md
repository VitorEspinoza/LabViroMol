# Diagrama Entidade-Relacionamento — Schema `scheduling`

[English](./er-diagram.md) · **Português**

Este documento apresenta o bloco do schema `scheduling`. Modela a camada de persistência (tabelas físicas reais) do agregado `Schedule`.

DbContext: `SchedulingDbContext`. `Schedule` implementa apenas `IModificationAuditable`
(sem `CreatedAt`/`CreatedBy` — único agregado do sistema sem auditoria de criação).

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
 text Status "enum ScheduleStatus como string"
 uuid ApprovedBy
 uuid RefusedBy
 text TermUrl
 text RefuseJustification
 timestamptz UpdatedAt
 uuid UpdatedBy
 }

 ScheduleEquipments {
 uuid Id PK
 uuid EquipmentId "cross-module, sem FK, ver legenda"
 text EquipmentName "cópia desnormalizada do nome no momento do agendamento"
 uuid ScheduleId FK
 }

 Schedules ||--o{ ScheduleEquipments: "1:N (FK_ScheduleEquipments_Schedules_ScheduleId, cascade)"
```

> Nota: `Schedules` não possui colunas de criação (`CreatedAt`/`CreatedBy`) nem soft
> delete — confirmado na migration (`Schedule: IModificationAuditable` apenas, sem
> `ICreationAuditable`/`IDeletionAuditable`). `ScheduleEquipments.EquipmentId` referencia
> `assets.Equipments` (outro schema/módulo) sem FK de banco — apenas Guid + nome copiado.
