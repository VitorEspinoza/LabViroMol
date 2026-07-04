# Diagrama de Classes — Shared Kernel

[English](./class-diagram.md) · **Português**

Este documento apresenta o diagrama de classes do **Shared Kernel**. Cobre as
classes e interfaces base de onde todo o domínio dos 6 módulos do LabViroMol herda ou
implementa.

```mermaid
classDiagram
 class BaseEntity~TId~ {
 +Id: TId
 }

 class AggregateRoot~TId~ {
 +Events: IReadOnlyCollection~IEvent~
 +ClearEvents() void
 +ClearDomainEvents() void
 #AddEvent(event: IEvent) void
 }
 AggregateRoot~TId~ --|> BaseEntity~TId~
 AggregateRoot~TId~..|> IHasEvents
 AggregateRoot~TId~..|> IConcurrencySafe

 class IHasEvents {
 <<interface>>
 +Events: IReadOnlyCollection~IEvent~
 +ClearDomainEvents() void
 +ClearEvents() void
 }

 class IConcurrencySafe {
 <<interface>>
 }

 class ICreationAuditable {
 <<interface>>
 }
 class IModificationAuditable {
 <<interface>>
 }
 class IDeletionAuditable {
 <<interface>>
 }
 class IFullAuditable {
 <<interface>>
 }
 IFullAuditable --|> ICreationAuditable
 IFullAuditable --|> IModificationAuditable
 IFullAuditable --|> IDeletionAuditable

 class ITranslatable~TTranslation~ {
 <<interface>>
 +Translations: Dictionary~string, TTranslation~
 }

 class IStrongId~TSelf~ {
 <<interface>>
 +Value: Guid
 +From(value: Guid) TSelf
 }

 class IEntityId {
 <<interface>>
 +Value: Guid
 }

 class Quantity {
 <<value object>>
 +Value: decimal
 }

 class UserId {
 <<value object>>
 +Value: Guid
 }
 UserId..|> IStrongId~UserId~
```
