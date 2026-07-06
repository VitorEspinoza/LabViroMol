# Diagrama de Casos de Uso — Módulo Notify

[English](./use-case-diagram.md) · **Português**

Este documento apresenta o diagrama de casos de uso do módulo **Notify**. Cobre
as notificações internas, agrupadas em 2 capacidades: a caixa de entrada in-app consumida
pelo Admin (visualizar não lidas e dispensar) e o motor de eventos sistêmicos que processa
Domain Events de outros módulos e alimenta essa caixa de entrada. Interagem com este
módulo os atores **Admin** e **Sistema**.

```mermaid
flowchart LR
 Admin(["Admin / Usuários Internos"])
 Sys(["Sistema (Background / Eventos)"])

 subgraph Notificacoes["Módulo Central de Notificações (LabViroMol)"]
 direction TB
 
 subgraph Interface["Caixa de Entrada (In-App)"]
 UC_CaixaEntrada(["Gerenciar Alertas do Painel\n(Visualizar Não Lidas e Dispensar)"])
 end

 subgraph Motor["Motor de Eventos Sistêmicos"]
 UC_GerarNotificacao(["Processar Eventos de Domínio\n(Ex: Estoque Baixo, Nova Solicitação de agendamento, etc)"])
 end
 end

 Admin --> UC_CaixaEntrada
 Sys --> UC_GerarNotificacao

 %% O fluxo de dados interno do sistema
 UC_GerarNotificacao -. "<< dispatch >>\nAlimenta a interface do usuário".-> UC_CaixaEntrada

 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000
 classDef systemUc fill:#f0f0f0,stroke:#777,stroke-dasharray: 3 3,color:#000

 class Admin,Sys actor
 class UC_CaixaEntrada adminUc
 class UC_GerarNotificacao systemUc
```

**Relações cross-módulo recebidas de outros módulos** (não desenhadas aqui por
pertencerem ao diagrama de origem, listadas para referência): `Inventory.Gerenciar
Pedidos de Compra` e `Scheduling.Motor de Notificações` disparam
`Notify.Processar Eventos de Domínio` — ver as notas nas seções desses módulos.
