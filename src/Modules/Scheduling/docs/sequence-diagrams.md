# Sequence Diagrams — Scheduling Module

**English** · [Português](./sequence-diagrams.pt-BR.md)

This document gathers the 4 sequence diagrams specific to the **Scheduling** module: Request Schedule, Approve Schedule, Refuse Schedule and Cancel Schedule. They cover the full lifecycle of the `Schedule` aggregate.

Conventions common to the diagrams (inherited from the source document):
- `autonumber` to reference steps during review.
- Solid arrows (`->>`) for synchronous calls, dashed (`-->>`) for returns.
- Lifeline activation via `+`/`-`.
- `alt`/`else` blocks for conditional business rules and state transitions.
- `loop` blocks for the domain event publication loop in `BaseUnitOfWork.CompleteAsync()`.
- `Note over` only for module boundaries and business rules that manifest as flow branching.
- The Domain Entity never interacts directly with the DbContext — only the Repository/UnitOfWork persists.
- The `Validator` (FluentValidation) does not appear as its own participant/lifeline: validation runs inside the `Mediator` pipeline (Behavior).
- Looking up an aggregate by Id followed by an existence check is simplified to a single lookup arrow + a `Returns Result.NotFound if it does not exist` note.

---

## 1. Request Schedule

Sources: `src/Modules/Scheduling/Presentation/Schedules/ScheduleEndpoints.cs` (`MapInstitutionalScheduleEndpoints`), `src/Modules/Scheduling/Presentation/SchedulingModule.cs` (composes the `/api/scheduling/public/schedules` route), `src/Modules/Scheduling/Application/Schedules/Commands/Create/{CreateScheduleCommand,CreateScheduleHandler,CreateScheduleValidator}.cs`, `src/Modules/Scheduling/Domain/Schedules/{Schedule,Scheduling}.cs`, `src/Modules/Scheduling/Domain/Schedules/Policies/BusinessTimePolicies.cs`, `src/Modules/Scheduling/Domain/Schedules/Events/NewScheduleDomainEvent.cs`, `src/Modules/Scheduling/Application/Schedules/EventHandlers/{NewScheduleNotificationEventHandler,NewScheduleEmailEventHandler}.cs`, `src/Modules/Notify/Contracts/ISendNotification.cs`, `src/Modules/Notify/Domain/Notifications/Notification.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Visitante as Visitor (institutional Next.js)
 participant Endpoint as ScheduleEndpoints
 participant Mediator as IMediator
 participant Handler as CreateScheduleHandler
 participant Repo as IScheduleRepository
 participant SchedulingVO as Scheduling (Domain VO)
 participant ScheduleAgg as Schedule (Domain Aggregate)
 participant UoW as ISchedulingUnitOfWork
 participant Observers as Domain Event Handlers

 Note over Visitante,Endpoint: RequireRateLimiting("SchedulingPolicy") — 5 requests/hour per IP
 Visitante->>Endpoint: POST /api/scheduling/public/schedules
 Endpoint->>Mediator: Send(CreateScheduleCommand)
 Note over Mediator: Runs the Validation Pipeline (FluentValidation)
 Mediator->>+Handler: Handle(command)

 Handler->>Repo: GetSchedulesConflictAsync(start, end, equipmentIds)
 Repo-->>Handler: List~Schedule~ conflicts

 alt Conflicts.Count > 0
 Handler-->>Mediator: Result.BusinessRule("...time conflicting with other confirmed schedules")
 else No conflicts
 Handler->>+SchedulingVO: Scheduling.Create(date, start, end)
 SchedulingVO->>SchedulingVO: BusinessTimePolicies.Validate(date, start, end)
 alt Past date / outside business day or business hours
 SchedulingVO-->>Handler: Result.Failure (Validation/BusinessRule)
 Handler-->>Mediator: Result (error)
 else Validation OK
 SchedulingVO-->>-Handler: Result.Success(Scheduling)
 Handler->>+ScheduleAgg: Schedule.Create(scheduler, scheduling, acceptTerm, advisorProfessor, projectTitle, description, equipments)
 ScheduleAgg-->>-Handler: Result.Success(Schedule)
 Handler->>Repo: AddAsync(schedule)
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(NewScheduleDomainEvent)
 Note over Observers: Triggers an internal notification (ISendNotification) + an e-mail to Scheduler.Email — independent handlers reacting to the same event (NewScheduleNotificationEventHandler, NewScheduleEmailEventHandler)
 UoW->>UoW: Applies auditing (CreatedAt/CreatedBy) and SaveChangesAsync()
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Visitante: 201 Created
```

**Highlighted business rule:** the private `Schedule.Create` constructor already sets `Status=PENDING` and calls `AddEvent(NewScheduleDomainEvent)`, but the actual publication of the event via Mediator only happens inside the `BaseUnitOfWork.CompleteAsync()` loop — there is, therefore, a gap between "event created" (in-memory buffer) and "event published". The time-conflict check (`GetSchedulesConflictAsync`) happens BEFORE any domain object is created, and the public endpoint is protected by rate limiting (5 requests/hour per IP).

---

## 2. Approve Schedule

Sources: `src/Modules/Scheduling/Presentation/Schedules/ScheduleEndpoints.cs`, `src/Modules/Scheduling/Application/Schedules/Commands/Approve/{ApproveScheduleCommand,ApproveScheduleCommandHandler}.cs`, `src/Modules/Scheduling/Domain/Schedules/Schedule.cs` (`EnsureCanBeApprovedOrRefused`, `Approve`), `src/Modules/Scheduling/Domain/Schedules/Events/{ApprovedScheduleDomainEvent,ReprovedScheduleDomainEvent}.cs`, `src/Modules/Scheduling/Application/Schedules/EventHandlers/{ApprovedScheduleEmailEventHandler,ReprovedScheduleEmailEventHandler}.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as ScheduleEndpoints
 participant Mediator as IMediator
 participant Handler as ApproveScheduleCommandHandler
 participant Repo as IScheduleRepository
 participant ScheduleAgg as Schedule (Domain Aggregate)
 participant Conflict as Conflicting Schedule (Domain)
 participant UoW as ISchedulingUnitOfWork
 participant Observers as Domain Event Handlers

 Admin->>Endpoint: POST /api/scheduling/schedules/{id}/approve
 Endpoint->>Mediator: Send(ApproveScheduleCommand)
 Note over Mediator: Runs the Validation Pipeline (FluentValidation) — no dedicated validator beyond the standard pipeline
 Mediator->>+Handler: Handle(command)
 Handler->>Repo: Looks up Schedule by Id
 Repo-->>Handler: Schedule
 Note over Handler: Returns Result.NotFound if it does not exist

 Handler->>+ScheduleAgg: Approve(currentUser.Id)
 ScheduleAgg->>ScheduleAgg: EnsureCanBeApprovedOrRefused()
 alt Status != PENDING
 ScheduleAgg-->>Handler: Result.BusinessRule("Schedule is not pending.")
 else Scheduling.Date earlier than today
 ScheduleAgg-->>Handler: Result.BusinessRule("Cannot change a schedule with a past date.")
 else Valid for approval
 ScheduleAgg->>ScheduleAgg: Status = SCHEDULED, ApprovedBy = userId
 ScheduleAgg->>ScheduleAgg: AddEvent(ApprovedScheduleDomainEvent(this))
 ScheduleAgg-->>-Handler: Result.Success()
 end

 alt result.IsFailure
 Handler-->>Mediator: result (error) — returns immediately, before the conflict cascade and CompleteAsync
 else result.IsSuccess
 Handler->>Repo: GetSchedulesConflictAsync(StartDateHour, EndDateHour, equipmentIds)
 Repo-->>Handler: List~Schedule~ conflicts
 loop For each conflicting PENDING schedule (Id != schedule.Id)
 Handler->>+Conflict: Refuse(currentUser.Id, "Another schedule with a conflicting time was approved.")
 Conflict->>Conflict: EnsureCanBeApprovedOrRefused() + Status=REFUSED, RefusedBy, RefuseJustification
 Conflict->>Conflict: AddEvent(ReprovedScheduleDomainEvent(this, justification))
 Conflict-->>-Handler: Result (not checked by the handler)
 end
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(ApprovedScheduleDomainEvent + N ReprovedScheduleDomainEvent)
 Note over Observers: Triggers an Approval e-mail to Scheduler.Email and N Refusal-by-Conflict e-mails to the conflicting requesters. A SINGLE CompleteAsync publishes all of them in the same loop. There is no internal-notification handler for these events, only e-mail
 UoW-->>-Handler: OK
 Handler-->>Mediator: result (Result.Success())
 end
 deactivate Handler
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 202 Accepted
```

**Highlighted business rule:** when approving a schedule, `ApproveScheduleCommandHandler` only runs the conflict-refusal cascade (`RefuseConflictingSchedules` — looks up conflicts again via `GetSchedulesConflictAsync` and refuses all conflicting `PENDING` schedules) and calls `CompleteAsync()` if `Approve()` returned success; there is a guard `if (result.IsFailure) return result;` right after the call to `Approve`, which interrupts the flow before the cascade and the persistence in case of failure. When successful, `CompleteAsync()` publishes the `ApprovedScheduleDomainEvent` of the approved aggregate and all the `ReprovedScheduleDomainEvent`s of the refused conflicting ones in the same event loop.

---

## 3. Refuse Schedule

Sources: `src/Modules/Scheduling/Presentation/Schedules/ScheduleEndpoints.cs`, `src/Modules/Scheduling/Application/Schedules/Commands/Refuse/{RefuseScheduleCommand,RefuseScheduleCommandHandler,RefuseScheduleValidator}.cs`, `src/Modules/Scheduling/Domain/Schedules/Schedule.cs` (`EnsureCanBeApprovedOrRefused`, `Refuse`), `src/Modules/Scheduling/Domain/Schedules/Events/ReprovedScheduleDomainEvent.cs`, `src/Modules/Scheduling/Application/Schedules/EventHandlers/ReprovedScheduleEmailEventHandler.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as ScheduleEndpoints
 participant Mediator as IMediator
 participant Handler as RefuseScheduleCommandHandler
 participant Repo as IScheduleRepository
 participant ScheduleAgg as Schedule (Domain Aggregate)
 participant UoW as ISchedulingUnitOfWork
 participant Observers as Domain Event Handlers

 Admin->>Endpoint: POST /api/scheduling/schedules/{id}/refuse (Justification)
 Endpoint->>Mediator: Send(RefuseScheduleCommand)
 Note over Mediator: Runs the Validation Pipeline (FluentValidation) — Justification >= 20 characters
 Mediator->>+Handler: Handle(command)
 Handler->>Repo: Looks up Schedule by Id
 Repo-->>Handler: Schedule
 Note over Handler: Returns Result.NotFound if it does not exist

 Handler->>+ScheduleAgg: Refuse(currentUser.Id, justification)
 ScheduleAgg->>ScheduleAgg: EnsureCanBeApprovedOrRefused()
 alt Status != PENDING or past date
 ScheduleAgg-->>Handler: Result.BusinessRule(message)
 else Valid for refusal
 ScheduleAgg->>ScheduleAgg: Status = REFUSED, RefusedBy = userId, RefuseJustification = justification
 ScheduleAgg->>ScheduleAgg: AddEvent(ReprovedScheduleDomainEvent(this, justification))
 ScheduleAgg-->>-Handler: Result.Success()
 end
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(ReprovedScheduleDomainEvent)
 Note over Observers: ReprovedScheduleEmailEventHandler sends an e-mail to Scheduler.Email — there is no internal-notification handler (ISendNotification) for this event
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 202 Accepted
```

**Highlighted business rule:** unlike the Approval flow (Diagram 2), a direct refusal does NOT trigger a cascade over other schedules — it affects only its own aggregate. `RefuseScheduleCommandHandler` calls `_unitOfWork.CompleteAsync()` even when `schedule.Refuse(...)` fails internally (the handler does not check the `Result` returned by `Refuse` before persisting, unlike the other handlers in this document), always returning `Result.Success()` to the Mediator.

---

## 4. Cancel Schedule

Sources: `src/Modules/Scheduling/Presentation/Schedules/ScheduleEndpoints.cs`, `src/Modules/Scheduling/Application/Schedules/Commands/Cancel/{CancelScheduleCommand,CancelScheduleHandler}.cs`, `src/Modules/Scheduling/Domain/Schedules/Schedule.cs` (`Cancel`), `src/Modules/Scheduling/Domain/Schedules/Events/CanceledScheduleDomainEvent.cs`, `src/Modules/Scheduling/Application/Schedules/EventHandlers/CanceledScheduleEmailEventHandler.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as ScheduleEndpoints
 participant Mediator as IMediator
 participant Handler as CancelScheduleHandler
 participant Repo as IScheduleRepository
 participant ScheduleAgg as Schedule (Domain Aggregate)
 participant UoW as ISchedulingUnitOfWork
 participant Observers as Domain Event Handlers

 Admin->>Endpoint: POST /api/scheduling/schedules/{id}/cancel (Justification)
 Endpoint->>Mediator: Send(CancelScheduleCommand)
 Mediator->>+Handler: Handle(command)
 Handler->>Repo: Looks up Schedule by Id
 Repo-->>Handler: Schedule
 Note over Handler: Returns Result.NotFound if it does not exist

 Handler->>+ScheduleAgg: Cancel(justification, currentUser.Id)
 alt Status == CANCELED
 ScheduleAgg-->>Handler: Result.BusinessRule("Schedule already canceled.")
 else Status == REFUSED
 ScheduleAgg-->>Handler: Result.BusinessRule("A refused schedule cannot be canceled.")
 else Status == PENDING or SCHEDULED (any other)
 ScheduleAgg->>ScheduleAgg: Status = CANCELED
 ScheduleAgg->>ScheduleAgg: RefusedBy = userId, RefuseJustification = justification
 ScheduleAgg->>ScheduleAgg: AddEvent(CanceledScheduleDomainEvent(this, justification))
 ScheduleAgg-->>-Handler: Result.Success()
 end
 alt Result.IsFailure
 Handler-->>Mediator: Result (error) — CompleteAsync is NOT called
 else Result.IsSuccess
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(CanceledScheduleDomainEvent)
 Note over Observers: CanceledScheduleEmailEventHandler sends an e-mail to Scheduler.Email — the only handler, no internal notification, same pattern as Approve/Refuse
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 202 Accepted
```

**Highlighted business rule:** `Schedule.Cancel` uses an exclusion rule DIFFERENT from `EnsureCanBeApprovedOrRefused` (used in Approve/Refuse) — it blocks only when `Status == CANCELED` or `Status == REFUSED`; any other status, including `PENDING` (not yet evaluated) and `SCHEDULED` (already approved), allows cancellation. The entity reuses the `RefusedBy`/`RefuseJustification` fields (the SAME ones used by `Refuse`, see Diagram 3) instead of having dedicated cancellation fields — there are no fields of its own for cancellation, a modeling decision that reduces the data surface but reuses semantics from another flow.
