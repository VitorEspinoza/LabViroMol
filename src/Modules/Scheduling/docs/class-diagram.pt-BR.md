# Diagrama de Classes — Módulo Scheduling

[English](./class-diagram.md) · **Português**

Este documento apresenta o diagrama de classes de domínio específico do módulo **Scheduling**. Cobre exclusivamente a camada Domain: o aggregate root `Schedule`, os value objects `Scheduler`, `Scheduling` e `ScheduleEquipment`, e o enum `ScheduleStatus`.

```mermaid
classDiagram
 class Schedule {
 +Scheduler: Scheduler
 +Scheduling: Scheduling
 +AcceptTerm: bool
 +AdvisorProfessor: string
 +ProjectTitle: string
 +Description: string
 +Status: ScheduleStatus
 +ApprovedBy: UserId
 +RefusedBy: UserId
 +TermUrl: string
 +RefuseJustification: string
 +Create(scheduler, scheduling, acceptTerm, advisorProfessor, projectTitle, description, equipments) Result~Schedule~
 +Approve(userId) Result
 +Refuse(userId, justification) Result
 +Cancel(justification, userId) Result
 +AttachTermUrl(url) void
 }
 Schedule --|> AggregateRoot~ScheduleId~
 Schedule..|> IModificationAuditable

 class Scheduler {
 <<value object>>
 +Name: string
 +Course: string
 +Email: string
 }
 Schedule "1" --> "1" Scheduler: Scheduler

 class Scheduling {
 <<value object>>
 +Date: DateOnly
 +StartDateHour: DateTimeOffset
 +EndDateHour: DateTimeOffset
 +Create(date, start, end) Result~Scheduling~
 }
 Schedule "1" --> "1" Scheduling: Scheduling

 class ScheduleEquipment {
 <<value object>>
 +EquipmentId: Guid
 +Name: string
 }
 Schedule "1" *-- "many" ScheduleEquipment: Equipments
 ScheduleEquipment..> EquipmentRef: EquipmentId (referencia Assets.Equipment, cross-module)

 class EquipmentRef {
 <<Assets.Equipment, fora deste módulo>>
 }
 note for ScheduleEquipment "Cópia desnormalizada do nome do equipamento.\nNão é referência de classe a Assets.Equipment,\napenas Guid + string copiados no momento do agendamento."

 class ScheduleStatus {
 <<enumeration>>
 PENDING
 SCHEDULED
 REFUSED
 CANCELED
 }
 Schedule --> ScheduleStatus: Status
```
