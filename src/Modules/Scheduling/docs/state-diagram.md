# State Diagram — Schedule (Scheduling Module)

**English** · [Português](./state-diagram.pt-BR.md)

This document presents the state diagram of the `Schedule` aggregate.

Sources: `src/Modules/Scheduling/Domain/Schedules/Schedule.cs`, `src/Modules/Scheduling/Domain/Schedules/ScheduleStatus.cs`, handlers in `src/Modules/Scheduling/Application/Schedules/Commands/{Approve,Refuse,Cancel}/`.

`ScheduleStatus` has 4 states: `PENDING`, `SCHEDULED`, `REFUSED`, `CANCELED`. `REFUSED` and `CANCELED` are terminal states. `Approve` and `Refuse` share the same guard (`EnsureCanBeApprovedOrRefused`); `Cancel` uses an independent guard, based on state exclusion.

```mermaid
stateDiagram-v2
 [*] --> PENDING: Schedule.Create()
 note right of PENDING
 Raises NewScheduleDomainEvent
 end note

 PENDING --> SCHEDULED: Approve(userId)
 note right of SCHEDULED
 Guard (EnsureCanBeApprovedOrRefused):
 Status == PENDING and Scheduling.Date
 must not be in the past, otherwise
 "Agendamento não está pendente." /
 "Não é possível alterar agendamento
 com data passada."
 Raises ApprovedScheduleDomainEvent.
 Side effect (outside this aggregate):
 RefuseConflictingSchedules cascades
 a refusal to any other PENDING
 schedule with a time slot/equipment
 conflict — not represented as a
 formal transition of another instance.
 end note

 PENDING --> REFUSED: Refuse(userId, justification)
 note right of REFUSED
 Same guard as EnsureCanBeApprovedOrRefused.
 Raises ReprovedScheduleDomainEvent.
 end note

 PENDING --> CANCELED: Cancel(justification, userId)
 SCHEDULED --> CANCELED: Cancel(justification, userId)
 note right of CANCELED
 Guard (by exclusion): blocks only
 if Status == CANCELED
 ("Agendamento já cancelado.") or
 Status == REFUSED
 ("Agendamento reprovado não pode ser
 cancelado."). Allows canceling both
 PENDING and SCHEDULED.
 Raises CanceledScheduleDomainEvent.
 Reuses the RefusedBy/
 RefuseJustification fields (the same
 ones used by Refuse), with no fields
 of its own.
 end note

 REFUSED --> [*]
 CANCELED --> [*]
```

**Reading guide**: every schedule is born `PENDING` and awaits a decision. The decision is binary and terminal in one of two directions: `Refuse` leads to `REFUSED` (terminal, no way back) or `Approve` leads to `SCHEDULED`. From either `PENDING` or `SCHEDULED`, `Cancel` is always possible and leads to `CANCELED` (terminal) — the only blocked combination is canceling something that is already `CANCELED` or `REFUSED`. The cascading effect of `Approve` on other `Schedule` instances is documented as a note because the state diagram represents a single aggregate instance, not an interaction diagram between aggregates.
