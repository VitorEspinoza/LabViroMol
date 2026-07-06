# Use Case Diagram — Inventory Module

**English** · [Português](./use-case-diagram.pt-BR.md)

This document extracts the section specific to the **Inventory** module. It covers stock management use cases, grouped into 3 operational
capabilities (catalog and kit management, purchase order management, stock
movement) plus the project eligibility validation business rule, included when
consuming material for a project. The only actor interacting with this module is **Admin**.

```mermaid
flowchart LR
 Admin(["Admin"])

 subgraph Inventory["Inventory Module (LabViroMol)"]
 direction TB
 
 UC_Catalogo(["Manage Catalog and Kits\n(Materials, Types and Kits)"])
 UC_Pedidos(["Manage Purchase Orders\n(Create, Process, Receive, Cancel)"])
 UC_Movimentacao(["Move Stock\n(Manual Adjustments and Consumption)"])
 
 UC_ValidarProjeto(["Validate Project Eligibility"])
 end

 Admin --> UC_Catalogo
 Admin --> UC_Pedidos
 Admin --> UC_Movimentacao

 UC_Movimentacao -. "<< include >>\nWhen consuming for a project".-> UC_ValidarProjeto

 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000
 classDef ruleUc fill:#f5f5f5,stroke:#666,stroke-dasharray: 5 5,color:#333

 class Admin actor
 class UC_Catalogo,UC_Pedidos,UC_Movimentacao adminUc
 class UC_ValidarProjeto ruleUc
```

**Cross-module relations:**
- `Manage Catalog and Kits` depends on `Identity.Log In / Log Out` (authentication)
 — see the Context Map (`context-map.md`) for the integration mechanism.
- `Manage Purchase Orders` (upon receiving an order) triggers
 `Notify.Process Domain Events` — see the Context Map (`context-map.md`) for the
 integration mechanism.
