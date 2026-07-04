# Class Diagram — Notify Module

**English** · [Português](./class-diagram.pt-BR.md)

This document presents the domain class diagram of the **Notify** module. It covers
the `Notification` aggregate root and its child entity `NotificationDismissal`.

```mermaid
classDiagram
 class Notification {
 +Title: string
 +Message: string
 +ReferenceId: string
 +ReferenceModule: string
 +Type: string
 +TargetPermission: string
 +ExpiresOn: DateTimeOffset
 +Create(title, message, targetPermission, referenceId, referenceModule, type) Result~Notification~
 +Dismiss(userId) void
 }
 Notification --|> AggregateRoot~NotificationId~
 Notification..|> ICreationAuditable

 class NotificationDismissal {
 +UserId: UserId
 +DismissedOn: DateTimeOffset
 }
 Notification "1" *-- "many" NotificationDismissal: NotificationDismissals

 note for Notification "ReferenceId / ReferenceModule\nare generic string references to\nany entity of any module\n(e.g.: Schedule, MaintenanceRequest, Order)."
```
