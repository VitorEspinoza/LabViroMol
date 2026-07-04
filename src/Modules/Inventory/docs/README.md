# Architecture Documentation — Inventory Module

**English** · [Português](./README.pt-BR.md)

Index of the architecture diagrams specific to the Inventory module.

- [`use-case-diagram.md`](./use-case-diagram.md) — use cases for catalog/kit management, purchase orders and stock movement.
- [`class-diagram.md`](./class-diagram.md) — class diagram of the `MaterialType`, `Material`, `Kit` and `Order` aggregates, and the `StockTransaction` entity.
- [`sequence-diagrams.md`](./sequence-diagrams.md) — sequence diagrams for the 7 core flows: Consume Material for Project, Create/Process/Receive/Cancel Order, Add/Remove Stock by Exception.
- [`state-diagram.md`](./state-diagram.md) — state diagram of the `Order` lifecycle (`OrderStatus`).
- [`er-diagram.md`](./er-diagram.md) — entity-relationship diagram of the `inventory` schema (physical Postgres tables).
