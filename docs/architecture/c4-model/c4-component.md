# C4 Model — Level 3: Components — LabViroMol

**English** · [Português](./c4-component.pt-BR.md)

This level opens up the `api` container (ASP.NET Core 10 Minimal API) and shows its main internal components: the 6 business modules, the Shared Kernel, the Mediator pipeline, and the authentication/authorization component — confirmed in `src/LabViroMol.Api/Program.cs` through the `AddIdentityModule`, `AddInventoryModule`, `AddSchedulingModule`, `AddAssetsModule`, `AddResearchModule`, `AddNotifyModule` and `AddSharedModule` registrations. It is useful for developers who work directly on the API codebase and need to understand how a module connects to the Commands/Queries pipeline, to authentication, and to other modules.

> **Source of truth**: the full model is defined in a single Structurizr DSL file: [`workspace.dsl`](./workspace.dsl). This document shows only the excerpt relevant to Level 3 (Components) — the `component` view of that workspace, with the components declared inside the `api` container.

## Model excerpt (Level 3)

```dsl
api = container "API" "ASP.NET Core 10 Minimal API" "Orquestra os 6 módulos de negócio via CQRS/Mediator" {
    authComponent    = component "Autenticação & Autorização" "ASP.NET Core Identity + JWT Bearer" "Emissão/validação de token, checagem de permissão por endpoint"
    mediatorPipeline = component "Mediator Pipeline" "Mediator (source-gen)" "Roteamento de Commands/Queries, ValidationBehavior (FluentValidation)"
    sharedKernel     = component "Shared Kernel" "Classes base .NET" "Primitivas comuns: AggregateRoot, StrongId, SmartEnum, Permissions"

    identityModule   = component "Módulo Identity" "C# / Clean Architecture" "Autenticação JWT, usuários, roles, permissões"
    researchModule   = component "Módulo Research" "C# / Clean Architecture" "Projetos, pesquisadores, publicações, parceiros"
    inventoryModule  = component "Módulo Inventory" "C# / Clean Architecture" "Materiais, estoque, kits, pedidos de compra"
    schedulingModule = component "Módulo Scheduling" "C# / Clean Architecture" "Agendamento de uso do laboratório"
    assetsModule     = component "Módulo Assets" "C# / Clean Architecture" "Equipamentos, manutenção"
    notifyModule     = component "Módulo Notify" "C# / Clean Architecture" "Notificações in-app e e-mail"
}

// Autenticação antes do despacho (6 relações: hub -> todos os módulos)
authComponent -> identityModule "Autentica/autoriza (antes do despacho)"
authComponent -> researchModule "Autentica/autoriza (antes do despacho)"
authComponent -> inventoryModule "Autentica/autoriza (antes do despacho)"
authComponent -> schedulingModule "Autentica/autoriza (antes do despacho)"
authComponent -> assetsModule "Autentica/autoriza (antes do despacho)"
authComponent -> notifyModule "Autentica/autoriza (antes do despacho)"

// Despacho de Commands/Queries (6 relações: todos os módulos -> Mediator)
identityModule -> mediatorPipeline "Despacha Commands/Queries"
researchModule -> mediatorPipeline "Despacha Commands/Queries"
inventoryModule -> mediatorPipeline "Despacha Commands/Queries"
schedulingModule -> mediatorPipeline "Despacha Commands/Queries"
assetsModule -> mediatorPipeline "Despacha Commands/Queries"
notifyModule -> mediatorPipeline "Despacha Commands/Queries"

// Herança de primitivas (6 relações: todos os módulos -> Shared Kernel)
identityModule -> sharedKernel "Herda primitivas"
researchModule -> sharedKernel "Herda primitivas"
inventoryModule -> sharedKernel "Herda primitivas"
schedulingModule -> sharedKernel "Herda primitivas"
assetsModule -> sharedKernel "Herda primitivas"
notifyModule -> sharedKernel "Herda primitivas"

// Relações cross-module específicas (3 relações)
inventoryModule -> researchModule "Consulta elegibilidade de projeto via Contract"
inventoryModule -> notifyModule "Dispara notificação/e-mail via Domain Event"
schedulingModule -> notifyModule "Dispara notificação/e-mail via Domain Event"
```

## Corresponding view

```dsl
views {
    component api "C4-Nivel-3-Componentes" {
        include *
        autoLayout
        description "Componentes internos do container API: os 6 módulos de negócio, Shared Kernel, Mediator Pipeline e Autenticação & Autorização."
    }
}
```

## Elements and relationships in this level

- **9 Component**: Authentication & Authorization, Mediator Pipeline, Shared Kernel (hubs) + Identity, Research, Inventory, Scheduling, Assets, Notify modules (business modules)
- **21 Rel**: 6 authentication→modules, 6 modules→Mediator, 6 modules→Shared Kernel, 3 module-specific cross-module relations (Inventory→Research, Inventory→Notify, Scheduling→Notify)

## Note on the notation migration

The earlier version of this document used a styled `flowchart TB` (Mermaid) instead of native `C4Component`, for the same reason documented in `deployment.md`: avoiding visual crossing among the 18 "many-to-few" relations between the 6 modules and the 3 hubs. In the migration to Structurizr DSL, that manual layout concern is no longer necessary — Structurizr's `autoLayout` engine (backed by ELK/dot) handles this fan-out pattern well, and the native `component` notation allows real hierarchical navigation across levels 1/2/3 from the same model.

## How to render

There is no Structurizr validation/rendering environment available in this project — the check performed was manual (brace balancing and identifier consistency between `model` and `views` in the full workspace). To generate the actual visualization:

- **Structurizr Lite** (interactive, local):
  ```
  docker run -p 8080:8080 -v ./docs/architecture/c4-model:/usr/local/structurizr structurizr/lite
  ```
  Then open `http://localhost:8080`.

- **structurizr-cli** (export to image/other notation):
  ```
  structurizr-cli export -workspace workspace.dsl -format mermaid
  structurizr-cli export -workspace workspace.dsl -format plantuml
  ```
