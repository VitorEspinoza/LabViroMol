# Use Case Diagram — Identity Module

**English** · [Português](./use-case-diagram.pt-BR.md)

This document extracts the section specific to the **Identity** module. It covers authentication and user/profile/
permission management use cases, grouped into 5 high-level capabilities: public authentication (login/logout),
password recovery/reset, user management (CRUD, activate/deactivate), role and permission
management, and self-account updates. The actors interacting with this module are
**Admin** (full Identity management) and **User / Visitor** (access to the public
authentication area).

```mermaid
flowchart LR
 Admin(["Admin"])
 Visitante(["User / Visitor"])

 subgraph Auth["Public Area (No Authentication)"]
 direction TB
 UC_Login(["Log In / Log Out"])
 UC_Recuperar(["Recover / Reset Password"])
 end

 subgraph Gestao["Identity Management (Requires Authentication)"]
 direction TB
 UC_GerirUsuarios(["Manage Users (CRUD, Activate/Deactivate)"])
 UC_GerirPerfis(["Manage Roles and Permissions"])
 UC_GerirPropriaConta(["Update Own Profile and Password"])
 end

 Visitante --> Auth
 Admin --> Auth
 Admin --> Gestao
 
 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef publicUc fill:#e2f0d9,stroke:#548235,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000

 class Admin,Visitante actor
 class UC_Login,UC_Recuperar publicUc
 class UC_GerirUsuarios,UC_GerirPerfis,UC_GerirPropriaConta adminUc
```

**Cross-module relations originating in other modules that depend on Identity** (not
drawn here since they belong to their source diagram, listed for reference):
`Inventory.Manage Catalog and Kits`, `Assets.Manage Equipment`,
`Research.Administer Projects` and `Scheduling.Analyze Request Queue` depend on
authentication (`Identity.Log In / Log Out`) — see the notes in those modules'
sections.
