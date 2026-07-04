# Diagrama de Casos de Uso — Módulo Scheduling

[English](./use-case-diagram.md) · **Português**

Este documento apresenta o diagrama de casos de uso específico do módulo **Scheduling**. Cobre o agendamento de uso do laboratório, agrupado em 3 capacidades: solicitação
pública (limitada a 5 requisições/hora), triagem e gestão pelo Admin (aprovar, recusar,
cancelar, anexar termos) e o motor de notificações sistêmico disparado a cada mudança de
status. Interagem com este módulo os atores **Admin**, **Estudante Externo / Visitante** e
**Sistema**.

```mermaid
flowchart LR
 Admin(["Admin"])
 Visitante(["Estudante Externo / Visitante"])
 Sys(["Sistema (Eventos/Notificações)"])

 subgraph Agendamento["Módulo de Agendamento (LabViroMol)"]
 direction TB
 
 subgraph Publico["Portal Institucional (Acesso Público)"]
 UC_Solicitar(["Solicitar Uso do Laboratório\n(Limitado a 5 requisições/hora)"])
 end

 subgraph Gestao["Triagem e Gestão (Acesso Restrito)"]
 UC_Analisar(["Analisar Fila de Solicitações\n(Aprovar, Recusar, Cancelar e Anexar Termos)"])
 end

 subgraph Automacao["Processos Sistêmicos (Background)"]
 UC_Notificar(["Motor de Notificações\n(Disparar E-mails e Alertas In-App no Painel)"])
 end
 end

 Visitante --> UC_Solicitar
 Admin --> UC_Analisar
 Sys --> UC_Notificar

 %% Regras de negócio / Triggers
 UC_Solicitar -. "<< trigger >>\nAo criar nova solicitação".-> UC_Notificar
 UC_Analisar -. "<< trigger >>\nAo alterar status".-> UC_Notificar

 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef publicUc fill:#d9f7d9,stroke:#2e7d32,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000
 classDef systemUc fill:#f0f0f0,stroke:#777,stroke-dasharray: 3 3,color:#000

 class Admin,Visitante,Sys actor
 class UC_Solicitar publicUc
 class UC_Analisar adminUc
 class UC_Notificar systemUc
```

**Relações cross-módulo:**
- `Analisar Fila de Solicitações` depende de `Identity.Realizar Login / Logout`
 (autenticação) — ver Mapa de Contexto (`context-map.md`) para o mecanismo de integração.
- `Motor de Notificações` depende de `Notify.Processar Eventos de Domínio` — ver Mapa de
 Contexto (`context-map.md`) para o mecanismo de integração.
