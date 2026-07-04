# Architecture Documentation — Shared Kernel

**English** · [Português](./README.pt-BR.md)

Catalog of the architecture documentation specific to the **Shared Kernel**.

1. [Class Diagram](./class-diagram.md) — base classes and interfaces
 (`BaseEntity`, `AggregateRoot`, auditing interfaces, `IStrongId`,
 etc.) from which the domain of all 6 modules inherits or implements.

> The Shared Kernel has no use cases, database schema, or sequence/state
> diagrams of its own — that is why there is no `use-case-diagram.md`, `er-diagram.md`,
> `sequence-diagrams.md`, or `state-diagram.md` in this folder.
