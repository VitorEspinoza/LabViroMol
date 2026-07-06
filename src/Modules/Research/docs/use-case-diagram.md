# Use Case Diagram — Research Module

**English** · [Português](./use-case-diagram.pt-BR.md)

This document presents the use case diagram of the **Research** module. It covers
the management of partners, positions, research projects, project members and
publications, grouped into 4 capabilities: public browsing of the institutional
collection, project conduction by researchers, project administration, and management
of publications/base records by the Admin. The actors interacting with this module are
**Admin**, **Researcher** and **External Student / Visitor**.

```mermaid
flowchart LR
 Admin(["Admin"])
 Pesq(["Researcher"])
 Visitante(["External Student / Visitor"])

 subgraph Pesquisa["Research and Extension Module (LabViroMol)"]
 direction TB
 
 subgraph Publico["Institutional Portal (Public Access)"]
 UC_AcervoPublico(["Browse Research Collection\n(Projects, Publications, Partners and Team)"])
 end

 subgraph Operacao["Scientific Execution (Researchers)"]
 UC_ConduzirProjeto(["Conduct Research Projects\n(Change Status, Manage Members and Leadership)"])
 end

 subgraph Governanca["Governance and Administration (Restricted)"]
 UC_CriarProjeto(["Administer Projects\n(Create, Edit, Delete)"])
 UC_Publicacoes(["Manage Scientific Publications\n(Registration, Authorship, Reordering and DOI)"])
 UC_CadastrosBase(["Manage Base Records\n(Partners and Positions)"])
 end
 end

 Visitante --> UC_AcervoPublico
 
 Pesq --> UC_ConduzirProjeto
 
 Admin --> UC_ConduzirProjeto
 Admin --> UC_CriarProjeto
 Admin --> UC_Publicacoes
 Admin --> UC_CadastrosBase

 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef publicUc fill:#d9f7d9,stroke:#2e7d32,color:#000
 classDef researcherUc fill:#f3e0ff,stroke:#6a1fa8,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000

 class Admin,Pesq,Visitante actor
 class UC_AcervoPublico publicUc
 class UC_ConduzirProjeto researcherUc
 class UC_CriarProjeto,UC_Publicacoes,UC_CadastrosBase adminUc
```

**Cross-module relations:**
- `Administer Projects` depends on `Identity.Perform Login / Logout` (authentication) —
 see the Context Map (`context-map.md`) for the integration mechanism.
