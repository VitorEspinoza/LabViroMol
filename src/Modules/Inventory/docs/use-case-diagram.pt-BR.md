# Diagrama de Casos de Uso — Módulo Inventory

[English](./use-case-diagram.md) · **Português**

Este documento extrai a seção específica do módulo **Inventory**. Cobre os casos de uso de gestão de estoque, agrupados em 3 capacidades
operacionais (gestão de catálogo e kits, gestão de pedidos de compra, movimentação de
estoque) mais a regra de negócio de validação de elegibilidade do projeto, incluída ao
consumir material para um projeto. Interage com este módulo apenas o ator **Admin**.

```mermaid
flowchart LR
 Admin(["Admin"])

 subgraph Inventory["Módulo de Estoque (LabViroMol)"]
 direction TB
 
 UC_Catalogo(["Gerenciar Catálogo e Kits\n(Materiais, Tipos e Kits)"])
 UC_Pedidos(["Gerenciar Pedidos de Compra\n(Criar, Processar, Receber, Cancelar)"])
 UC_Movimentacao(["Movimentar Estoque\n(Ajustes Manuais e Consumo)"])
 
 UC_ValidarProjeto(["Validar Elegibilidade do Projeto"])
 end

 Admin --> UC_Catalogo
 Admin --> UC_Pedidos
 Admin --> UC_Movimentacao

 UC_Movimentacao -. "<< include >>\nAo consumir para projeto".-> UC_ValidarProjeto

 classDef actor fill:#fff3cd,stroke:#a67c00,stroke-width:1px,color:#000
 classDef adminUc fill:#d6e8ff,stroke:#1f5fa8,color:#000
 classDef ruleUc fill:#f5f5f5,stroke:#666,stroke-dasharray: 5 5,color:#333

 class Admin actor
 class UC_Catalogo,UC_Pedidos,UC_Movimentacao adminUc
 class UC_ValidarProjeto ruleUc
```

**Relações cross-módulo:**
- `Gerenciar Catálogo e Kits` depende de `Identity.Realizar Login / Logout` (autenticação)
 — ver Mapa de Contexto (`context-map.md`) para o mecanismo de integração.
- `Gerenciar Pedidos de Compra` (ao receber um pedido) dispara
 `Notify.Processar Eventos de Domínio` — ver Mapa de Contexto (`context-map.md`) para o
 mecanismo de integração.
