# Use Case Diagram — Assets Module

**English** · [Português](./use-case-diagram.pt-BR.md)

This document presents the use case diagram specific to the **Assets** module. It covers equipment and maintenance management, grouped into 2 internal capabilities
(equipment management and maintenance lifecycle) plus the public equipment catalog
lookup consumed by the institutional site. The actors interacting with this module
are **Admin** and **External Student / Visitor**.

```mermaid
flowchart LR
 Admin(["Admin"])
 Visitante(["External Student / Visitor"])

 subgraph Assets["Assets and Equipment Module (LabViroMol)"]
 direction TB
 
 subgraph Publico["Institutional Area (Public Access)"]
 UC_CatalogoPublico(["Browse Equipment Catalog\n(Listing, Details and Bookable Items)"])
 end

 subgraph Interno["Internal Management (Restricted Access)"]
 UC_GerirEquipamentos(["Manage Equipment\n(CRUD and Image Upload)"])
 UC_CicloManutencao(["Manage Maintenance Lifecycle\n(Request, Start, Complete and Cancel)"])
 end
 end

 Visitante --> UC_CatalogoPublico
 Admin --> Interno

 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000
 classDef publicUc fill:#d9f7d9,stroke:#2e7d32,color:#000

 class Admin,Visitante actor
 class UC_GerirEquipamentos,UC_CicloManutencao adminUc
 class UC_CatalogoPublico publicUc
```

**Cross-module relations:**
- `Manage Equipment` depends on `Identity.Log In / Log Out` (authentication) —
 see the Context Map (`context-map.md`) for the integration mechanism.
