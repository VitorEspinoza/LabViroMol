# Use Case Diagram — Notify Module

**English** · [Português](./use-case-diagram.pt-BR.md)

This document presents the use case diagram of the **Notify** module. It covers
internal notifications, grouped into 2 capabilities: the in-app inbox consumed
by the Admin (view unread and dismiss) and the systemic event engine that processes
Domain Events from other modules and feeds this inbox. The actors **Admin** and
**System** interact with this module.

```mermaid
flowchart LR
 Admin(["Admin / Internal Users"])
 Sys(["System (Background / Events)"])

 subgraph Notificacoes["Central Notifications Module (LabViroMol)"]
 direction TB
 
 subgraph Interface["Inbox (In-App)"]
 UC_CaixaEntrada(["Manage Panel Alerts\n(View Unread and Dismiss)"])
 end

 subgraph Motor["Systemic Event Engine"]
 UC_GerarNotificacao(["Process Domain Events\n(e.g.: Low Stock, New Scheduling Request, etc)"])
 end
 end

 Admin --> UC_CaixaEntrada
 Sys --> UC_GerarNotificacao

 %% The system's internal data flow
 UC_GerarNotificacao -. "<< dispatch >>\nFeeds the user interface".-> UC_CaixaEntrada

 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000
 classDef systemUc fill:#f0f0f0,stroke:#777,stroke-dasharray: 3 3,color:#000

 class Admin,Sys actor
 class UC_CaixaEntrada adminUc
 class UC_GerarNotificacao systemUc
```

**Cross-module relations received from other modules** (not drawn here since they
belong to the originating diagram, listed here for reference): `Inventory.Manage
Purchase Orders` and `Scheduling.Notification Engine` trigger
`Notify.Process Domain Events` — see the notes in those modules' sections.
