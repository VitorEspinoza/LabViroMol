# Class Diagram — Shared Kernel

**English** · [Português](./class-diagram.pt-BR.md)

This document presents the class diagram of the **Shared Kernel**. It covers the
base classes and interfaces from which the domain of all 6 LabViroMol modules
inherits or implements.

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
