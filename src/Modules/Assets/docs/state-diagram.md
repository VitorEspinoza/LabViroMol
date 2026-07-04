# State Diagram — MaintenanceRequest (Assets Module)

**English** · [Português](./state-diagram.pt-BR.md)

This document presents the state diagram of the `MaintenanceRequest` aggregate.

Sources: `src/Modules/Assets/Domain/MaintenanceRequests/MaintenanceRequest.cs`, `src/Modules/Assets/Domain/MaintenanceRequests/MaintenanceRequestStatus.cs`, handlers in `src/Modules/Assets/Application/MaintenanceRequests/Commands/{Create,Start,Done,Cancel}/`, `src/Modules/Assets/Application/Equipments/EventHandlers/EquipmentDeletedDomainEventHandler.cs`.

`MaintenanceRequestStatus` has 4 states: `Requested`, `InProgress`, `Done`, `Cancelled`. `Done` and `Cancelled` are terminal states.

```mermaid
stateDiagram-v2
 [*] --> Requested: MaintenanceRequest.Create()
 note right of Requested
 Guard in the Application layer (CreateMaintenanceHandler):
 blocks creation if an active request
 already exists for the same Equipment
 ("Maintenance for this equipment has already
 been requested or is in progress.").
 end note

 Requested --> InProgress: Start()
 note right of InProgress
 Guard: Status == Requested, otherwise
 "Cannot change status to
 'In Progress'."
 end note

 InProgress --> Done: Done()
 note right of Done
 Guard: Status == InProgress, otherwise
 "Cannot change status to
 'Done'."
 end note

 Requested --> Cancelled: Cancel()
 InProgress --> Cancelled: Cancel()
 note right of Cancelled
 Guard (by exclusion): blocks only
 if Status == Done
 ("Cannot cancel requests
 that are already done."). Allows cancelling
 both Requested and InProgress.
 end note

 note left of Requested
 Deleting the associated Equipment triggers
 EquipmentDeletedDomainEvent, handled by
 EquipmentDeletedDomainEventHandler, which
 removes (hard delete) every
 MaintenanceRequest for that equipment,
 regardless of status. This is not a
 state transition, it is record
 deletion — shown here only as
 an observation.
 end note

 Done --> [*]
 Cancelled --> [*]
```

**Reading guide**: every maintenance request is created as `Requested` (blocked at creation if another active request already exists for the same equipment) and follows the linear flow `Requested → InProgress → Done`. Cancellation (`Cancel()`) is allowed at any point before completion (`Requested` or `InProgress`), but never after (`Done` is terminal and protected). Deleting the associated `Equipment` is not a state transition — it is a direct record removal performed by `EquipmentDeletedDomainEventHandler`, outside the aggregate's status control.
