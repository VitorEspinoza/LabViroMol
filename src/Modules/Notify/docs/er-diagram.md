# Entity-Relationship Diagram — Notify Module

**English** · [Português](./er-diagram.pt-BR.md)

This document presents the ER block for the `notify` schema. DbContext:
`NotifyDbContext`. `Notification` implements only `ICreationAuditable` (no
modification or soft delete).

```mermaid
erDiagram
 Notifications {
 uuid Id PK
 text Title
 text Message
 text ReferenceId "generic string reference to any entity of any module"
 text ReferenceModule "name of the originating module, e.g. Scheduling, Assets"
 text Type
 text TargetPermission
 timestamptz ExpiresOn
 timestamptz CreatedAt
 uuid CreatedBy
 }

 NotificationDismissals {
 uuid NotificationId PK,FK
 int Id PK "identity, part of the composite PK"
 uuid UserId "cross-module (Identity), no FK, see legend"
 timestamptz DismissedOn
 }

 Notifications ||--o{ NotificationDismissals: "1:N (FK_NotificationDismissals_Notifications_NotificationId, cascade)"
```

> Note: `Notifications` has no `UpdatedAt`/`UpdatedBy` nor soft delete (`Notification`
> implements only `ICreationAuditable`). `NotificationDismissals.UserId` references
> `identity.Users`/`IdentityUsers` without a database FK (cross-module).
