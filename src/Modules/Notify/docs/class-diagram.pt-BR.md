# Diagrama de Classes — Módulo Notify

[English](./class-diagram.md) · **Português**

Este documento apresenta o diagrama de classes do domínio do módulo **Notify**. Cobre
o aggregate root `Notification` e sua entidade filha `NotificationDismissal`.

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

 note for Notification "ReferenceId / ReferenceModule\nsão referências genéricas em string a\nqualquer entidade de qualquer módulo\n(ex: Schedule, MaintenanceRequest, Order)."
```
