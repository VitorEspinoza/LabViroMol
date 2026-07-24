# Use Case Diagram — Scheduling Module

**English** · [Português](./use-case-diagram.pt-BR.md)

This document presents the use case diagram specific to the **Scheduling** module. It covers lab-usage scheduling, grouped into 3 capabilities: public request
(limited to 5 requests/hour), triage and management by the Admin (approve, refuse,
cancel, attach terms) and the systemic notification engine triggered on every status
change. The actors interacting with this module are **Admin**, **External Student / Visitor**
and **System**.

```mermaid
flowchart LR
 Admin(["Admin"])
 Visitante(["External Student / Visitor"])
 Sys(["System (Events/Notifications)"])

 subgraph Agendamento["Scheduling Module (LabViroMol)"]
 direction TB
 
 subgraph Publico["Institutional Portal (Public Access)"]
 UC_Solicitar(["Request Lab Usage\n(Limited to 5 requests/hour)"])
 end

 subgraph Gestao["Triage and Management (Restricted Access)"]
 UC_Analisar(["Review Request Queue\n(Approve, Refuse, Cancel and Attach Terms)"])
 end

 subgraph Automacao["Systemic Processes (Background)"]
 UC_Notificar(["Notification Engine\n(Trigger Emails and In-App Alerts on the Panel)"])
 end
 end

 Visitante --> UC_Solicitar
 Admin --> UC_Analisar
 Sys --> UC_Notificar

 %% Business rules / Triggers
 UC_Solicitar -. "<< trigger >>\nOn new request creation".-> UC_Notificar
 UC_Analisar -. "<< trigger >>\nOn status change".-> UC_Notificar

 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef publicUc fill:#d9f7d9,stroke:#2e7d32,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000
 classDef systemUc fill:#f0f0f0,stroke:#777,stroke-dasharray: 3 3,color:#000

 class Admin,Visitante,Sys actor
 class UC_Solicitar publicUc
 class UC_Analisar adminUc
 class UC_Notificar systemUc
```

**Cross-module relations:**
- `Review Request Queue` depends on `Identity.Log In / Log Out`
 (authentication) — see the Context Map (`context-map.md`) for the integration mechanism.
- `Notification Engine` depends on `Notify.Process Domain Events` — see the Context
 Map (`context-map.md`) for the integration mechanism.
