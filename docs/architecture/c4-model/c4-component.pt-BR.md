# C4 Model — Nível 3: Componentes — LabViroMol

[English](./c4-component.md) · **Português**

Este nível abre o container `api` (ASP.NET Core 10 Minimal API) e mostra seus principais componentes internos: os 6 módulos de negócio, o Shared Kernel, o pipeline do Mediator e o componente de autenticação/autorização — confirmados em `src/LabViroMol.Api/Program.cs` pelos registros `AddIdentityModule`, `AddInventoryModule`, `AddSchedulingModule`, `AddAssetsModule`, `AddResearchModule`, `AddNotifyModule` e `AddSharedModule`. É útil para desenvolvedores que vão trabalhar diretamente no código da API e precisam entender como um módulo se conecta ao pipeline de Commands/Queries, à autenticação e aos demais módulos.

> **Fonte de verdade**: o modelo completo está definido em um único arquivo Structurizr DSL: [`workspace.dsl`](./workspace.dsl). Este documento mostra apenas o recorte relevante ao Nível 3 (Componentes) — a view `component` desse workspace, com os componentes declarados dentro do container `api`.

## Recorte do modelo (Nível 3)

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

## View correspondente

```dsl
views {
    component api "C4-Nivel-3-Componentes" {
        include *
        autoLayout
        description "Componentes internos do container API: os 6 módulos de negócio, Shared Kernel, Mediator Pipeline e Autenticação & Autorização."
    }
}
```

## Elementos e relações deste nível

- **9 Component**: Autenticação & Autorização, Mediator Pipeline, Shared Kernel (hubs) + Módulo Identity, Research, Inventory, Scheduling, Assets, Notify (módulos de negócio)
- **21 Rel**: 6 autenticação→módulos, 6 módulos→Mediator, 6 módulos→Shared Kernel, 3 cross-module específicas (Inventory→Research, Inventory→Notify, Scheduling→Notify)

## Nota sobre a migração de notação

A versão anterior deste documento usava `flowchart TB` estilizado (Mermaid) em vez de `C4Component` nativo, pela mesma razão documentada em `deployment.md`: evitar cruzamento visual das 18 relações "muitos-para-poucos" entre os 6 módulos e os 3 hubs. Na migração para Structurizr DSL, essa preocupação de layout manual deixa de ser necessária — o motor `autoLayout` do Structurizr (apoiado por ELK/dot) trata bem esse padrão de fan-out, e a notação nativa `component` permite navegação hierárquica real entre os níveis 1/2/3 a partir do mesmo modelo.

## Como renderizar

Não há ambiente de validação/renderização Structurizr disponível neste projeto — a checagem feita foi manual (balanceamento de chaves e consistência de identificadores entre `model` e `views` no workspace completo). Para gerar a visualização real:

- **Structurizr Lite** (interativo, local):
  ```
  docker run -p 8080:8080 -v ./docs/architecture/c4-model:/usr/local/structurizr structurizr/lite
  ```
  Depois abrir `http://localhost:8080`.

- **structurizr-cli** (exportar para imagem/outra notação):
  ```
  structurizr-cli export -workspace workspace.dsl -format mermaid
  structurizr-cli export -workspace workspace.dsl -format plantuml
  ```
