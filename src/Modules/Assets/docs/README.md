# Architecture Documentation — Assets Module

**English** · [Português](./README.pt-BR.md)

This directory gathers the architecture diagrams specific to the **Assets** module.

- [`use-case-diagram.md`](./use-case-diagram.md) — use cases for equipment and maintenance management (internal CRUD and public catalog).
- [`class-diagram.md`](./class-diagram.md) — domain class diagram of the `Equipment` and `MaintenanceRequest` aggregates.
- [`state-diagram.md`](./state-diagram.md) — state diagram of the `MaintenanceRequest` aggregate.
- [`er-diagram.md`](./er-diagram.md) — entity-relationship model of the `assets` schema (`Equipments`, `MaintenanceRequests` tables).

Assets has no `sequence-diagrams.md` of its own: none of the documented sequence flows belong exclusively to this module.
