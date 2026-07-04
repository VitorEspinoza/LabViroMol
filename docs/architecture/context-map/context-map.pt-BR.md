# Mapa de Contexto — LabViroMol (DDD Strategic Design)

[English](./context-map.md) · **Português**

Um Context Map (Mapa de Contexto) é a representação estratégica de DDD que mostra os **bounded contexts** de um sistema e como eles se relacionam entre si — não em termos de classes ou fluxos de tela, mas em termos de **quem depende de quem**, **por qual mecanismo técnico** (eventos de integração, contratos/interfaces, referência fraca por id) e **sob qual padrão estratégico clássico** (Shared Kernel, Customer/Supplier, Conformist, Open Host Service + Published Language). Ele precede o C4 (e qualquer diagrama de classes/sequência) porque responde primeiro à pergunta "quais são as fronteiras de domínio do sistema e como elas se acoplam", antes de descer ao nível de containers, componentes ou código. No LabViroMol, os bounded contexts coincidem com os módulos do Clean Architecture (`src/Modules/*`): **Identity**, **Research**, **Inventory**, **Scheduling**, **Assets**, **Notify**, mais o **Shared Kernel** (`src/Modules/Shared`) que fornece primitivas comuns a todos.

O diagrama é autorado em [D2](https://d2lang.com), inspirado no Context Map clássico do livro *Implementing Domain-Driven Design* (Vaughn Vernon, 2013): cada bounded context é uma caixa (`shape: rectangle`), e os marcadores de papel do padrão estratégico (`OHS`, `PL`, `ACL`, `CF`, `SK`, `U`, `D`) ficam junto a cada extremidade da linha de relação via `source-arrowhead.label` (lado upstream/origem) e `target-arrowhead.label` (lado downstream/destino) — não como rótulo único no meio da linha.

O modelo completo está em [`context-map.d2`](./context-map.d2) — os 7 bounded contexts e as 14 relações. Recorte legível abaixo:

```d2
# Bounded Contexts
Identity: "Identity\n(Autenticação, usuários, roles e permissões)" {
  shape: rectangle
}
Research: "Research\n(Projetos, pesquisadores e publicações)" {
  shape: rectangle
}
Inventory: "Inventory\n(Materiais, estoque, kits e pedidos)" {
  shape: rectangle
  style.stroke-width: 3
  style.fill: "#E8F4FD"
}
Scheduling: "Scheduling\n(Agendamento de laboratório e equipamentos)" {
  shape: rectangle
  style.stroke-width: 3
  style.fill: "#E8F4FD"
}
Assets: "Assets\n(Equipamentos e manutenção)" {
  shape: rectangle
}
Notify: "Notify\n(Notificações in-app e e-mail)" {
  shape: rectangle
}
SharedKernel: "SharedKernel\n(AggregateRoot<TId>, UserId, SmartEnum, Permissions,\nIDomainEvent/IIntegrationEvent, BaseUnitOfWork)" {
  shape: rectangle
  style.stroke-dash: 3
  style.fill: "#F5F5F5"
}

# ===== Relações funcionais (marcador de papel em cada extremidade) =====

# Integration Events: UserRegisteredIntegrationEvent, UserUpdatedIntegrationEvent, ...
Identity -> Research: { source-arrowhead.label: "U"; target-arrowhead.label: "D" }

# Contract: ISendEmail
Identity -> Notify: { source-arrowhead.label: "U"; target-arrowhead.label: "D" }

# Contracts: IProjectChecker, IProjectCatalog, IResearcherProfileProvider
Research -> Inventory: { source-arrowhead.label: "OHS, PL"; target-arrowhead.label: "CF" }

# Contract: IResearcherProfileProvider
Research -> Identity: { source-arrowhead.label: "OHS, PL"; target-arrowhead.label: "CF" }

# LowStockDomainEvent -> ISendNotification
Inventory -> Notify: { source-arrowhead.label: "U"; target-arrowhead.label: "D" }

# Guid ref.: ProjectId (Inventory conforma-se a Research)
Research -> Inventory: { source-arrowhead.label: "U"; target-arrowhead.label: "CF" }

# NewScheduleDomainEvent, ApprovedScheduleDomainEvent, CanceledScheduleDomainEvent,
# ReprovedScheduleDomainEvent -> ISendNotification / ISendEmail
Scheduling -> Notify: { source-arrowhead.label: "U"; target-arrowhead.label: "D" }

# Guid ref.: EquipmentId (Scheduling conforma-se a Assets)
Assets -> Scheduling: { source-arrowhead.label: "U"; target-arrowhead.label: "CF" }

# ===== Shared Kernel (1 relação por módulo consumidor, conexão tracejada) =====
SharedKernel -> Identity: { style.stroke-dash: 4; source-arrowhead.label: "SK"; target-arrowhead.label: "SK" }
SharedKernel -> Research: { style.stroke-dash: 4; source-arrowhead.label: "SK"; target-arrowhead.label: "SK" }
SharedKernel -> Inventory: { style.stroke-dash: 4; source-arrowhead.label: "SK"; target-arrowhead.label: "SK" }
SharedKernel -> Scheduling: { style.stroke-dash: 4; source-arrowhead.label: "SK"; target-arrowhead.label: "SK" }
SharedKernel -> Assets: { style.stroke-dash: 4; source-arrowhead.label: "SK"; target-arrowhead.label: "SK" }
SharedKernel -> Notify: { style.stroke-dash: 4; source-arrowhead.label: "SK"; target-arrowhead.label: "SK" }
```

## Legenda dos padrões DDD

| Padrão | Significado | Notação D2 (marcador em cada extremidade da conexão) | Onde aparece neste mapa |
|---|---|---|---|
| **Shared Kernel** | Múltiplos bounded contexts compartilham deliberadamente um subconjunto de modelo/código (tipos, contratos de infraestrutura) mantido em comum, aceitando o acoplamento em troca de não duplicar conceitos centrais. | Conexão sem direção semântica (`style.stroke-dash` para diferenciar visualmente), com `source-arrowhead.label: "SK"` e `target-arrowhead.label: "SK"` — mesmo marcador nos dois lados. | `SharedKernel` → todos os 6 módulos (`AggregateRoot<TId>`, `UserId`, `SmartEnum`, `Permissions`, `IDomainEvent`/`IIntegrationEvent`, `BaseUnitOfWork`). |
| **Customer/Supplier** | Um contexto upstream (supplier) expõe uma API/evento que um contexto downstream (customer) consome; o supplier tem incentivo a não quebrar o contrato, mas a relação não é tão formalizada quanto um Open Host Service. | `source-arrowhead.label: "U"` (Upstream) no lado supplier, `target-arrowhead.label: "D"` (Downstream) no lado customer. | Identity → Research (Integration Events), Identity → Notify (`ISendEmail`), Inventory → Notify (`LowStockDomainEvent`), Scheduling → Notify (eventos de agendamento). |
| **Conformist** | O contexto downstream se conforma ao modelo do upstream sem traduzi-lo nem negociar um contrato — aqui materializado como referência fraca por `Guid`, sem acoplamento de classe/tipo de domínio. | `source-arrowhead.label: "U"` no upstream, `target-arrowhead.label: "CF"` (Conformist) no downstream. | Research → Inventory (`ProjectId`, Inventory conforma-se), Assets → Scheduling (`EquipmentId`, Scheduling conforma-se). |
| **Open Host Service + Published Language** | O contexto upstream publica um conjunto de interfaces/contratos estáveis e bem definidos (uma "linguagem publicada") para que múltiplos consumidores integrem de forma previsível, em vez de expor seu modelo interno. | `source-arrowhead.label: "OHS, PL"` no upstream, `target-arrowhead.label: "CF"` no downstream. | Research → Inventory (`IProjectChecker`, `IProjectCatalog`, `IResearcherProfileProvider`), Research → Identity (`IResearcherProfileProvider`). |

**Observação sobre Assets**: é o contexto mais isolado do sistema — não publica nem consome nenhum Contract ou Integration Event; sua única relação funcional é a de upstream/supplier em relação a Scheduling via referência fraca (`EquipmentId`, padrão Conformist — `"U"` em `Assets`, `"CF"` em `Scheduling`), além de depender do Shared Kernel como todos os demais.

**Destaque visual**: `Inventory` e `Scheduling` são os módulos de negócio "core" do sistema (estoque/pedidos e agendamento de laboratório) e recebem `style.stroke-width: 3` + `style.fill` diferenciado no `.d2`. `SharedKernel` recebe `style.stroke-dash` por não ser um módulo de negócio, e sim infraestrutura comum.

## Como visualizar o `.d2`

O D2 é uma DSL textual com várias formas de renderização gráfica:

- **VS Code**: instalar a extensão oficial **D2** (`terrastruct.d2`), que oferece preview gráfico em tempo real e validação de sintaxe ao editar [`context-map.d2`](./context-map.d2).
- **CLI**: instalar o `d2` ([instruções](https://d2lang.com/tour/install)) e rodar `d2 context-map.d2 context-map.svg` para gerar um SVG.
- **Playground online**: colar o conteúdo de [`context-map.d2`](./context-map.d2) em [play.d2lang.com](https://play.d2lang.com) para visualização imediata sem instalar nada.

O diagrama tem 7 bounded contexts e 14 relações.
