# Diagrama Entidade-Relacionamento — Módulo Notify

[English](./er-diagram.md) · **Português**

Este documento apresenta o bloco ER do schema `notify`. DbContext:
`NotifyDbContext`. `Notification` implementa apenas `ICreationAuditable` (sem
modificação nem soft delete).

```mermaid
erDiagram
 Notifications {
 uuid Id PK
 text Title
 text Message
 text ReferenceId "referência genérica em string a qualquer entidade de qualquer módulo"
 text ReferenceModule "nome do módulo de origem, ex: Scheduling, Assets"
 text Type
 text TargetPermission
 timestamptz ExpiresOn
 timestamptz CreatedAt
 uuid CreatedBy
 }

 NotificationDismissals {
 uuid NotificationId PK,FK
 int Id PK "identity, parte da PK composta"
 uuid UserId "cross-module (Identity), sem FK, ver legenda"
 timestamptz DismissedOn
 }

 Notifications ||--o{ NotificationDismissals: "1:N (FK_NotificationDismissals_Notifications_NotificationId, cascade)"
```

> Nota: `Notifications` não tem `UpdatedAt`/`UpdatedBy` nem soft delete (`Notification`
> implementa só `ICreationAuditable`). `NotificationDismissals.UserId` referencia
> `identity.Users`/`IdentityUsers` sem FK de banco (cross-module).
