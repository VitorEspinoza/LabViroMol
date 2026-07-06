# Class Diagram — Scheduling Module

**English** · [Português](./class-diagram.pt-BR.md)

This document presents the domain class diagram specific to the **Scheduling** module. It covers exclusively the Domain layer: the aggregate root `Schedule`, the value objects `Scheduler`, `Scheduling` and `ScheduleEquipment`, and the `ScheduleStatus` enum.

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
 ScheduleEquipment..> EquipmentRef: EquipmentId (references Assets.Equipment, cross-module)

 class EquipmentRef {
 <<Assets.Equipment, outside this module>>
 }
 note for ScheduleEquipment "Denormalized copy of the equipment name.\nNot a class reference to Assets.Equipment,\njust a Guid + string copied at scheduling time."

 class ScheduleStatus {
 <<enumeration>>
 PENDING
 SCHEDULED
 REFUSED
 CANCELED
 }
 Schedule --> ScheduleStatus: Status
```
