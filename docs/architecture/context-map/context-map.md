# Context Map — LabViroMol (DDD Strategic Design)

**English** · [Português](./context-map.pt-BR.md)

A Context Map is the DDD strategic representation that shows a system's **bounded contexts** and how they relate to each other — not in terms of classes or screen flows, but in terms of **who depends on whom**, **through which technical mechanism** (integration events, contracts/interfaces, weak reference by id), and **under which classic strategic pattern** (Shared Kernel, Customer/Supplier, Conformist, Open Host Service + Published Language). It precedes the C4 model (and any class/sequence diagram) because it answers first the question "what are the system's domain boundaries and how are they coupled", before descending to the level of containers, components or code. In LabViroMol, the bounded contexts coincide with the Clean Architecture modules (`src/Modules/*`): **Identity**, **Research**, **Inventory**, **Scheduling**, **Assets**, **Notify**, plus the **Shared Kernel** (`src/Modules/Shared`), which provides primitives common to all of them.

The diagram is authored in [D2](https://d2lang.com), inspired by the classic Context Map from the book *Implementing Domain-Driven Design* (Vaughn Vernon, 2013): each bounded context is a box (`shape: rectangle`), and the strategic pattern role markers (`OHS`, `PL`, `ACL`, `CF`, `SK`, `U`, `D`) sit next to each end of the relationship line via `source-arrowhead.label` (upstream/origin side) and `target-arrowhead.label` (downstream/destination side) — not as a single label in the middle of the line.

The full model is in [`context-map.d2`](./context-map.d2) — the 7 bounded contexts and 14 relationships. Readable excerpt below:

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

## DDD pattern legend

| Pattern | Meaning | D2 notation (marker at each end of the connection) | Where it appears in this map |
|---|---|---|---|
| **Shared Kernel** | Multiple bounded contexts deliberately share a subset of model/code (types, infrastructure contracts) maintained in common, accepting the coupling in exchange for not duplicating core concepts. | Connection with no semantic direction (`style.stroke-dash` for visual distinction), with `source-arrowhead.label: "SK"` and `target-arrowhead.label: "SK"` — same marker on both sides. | `SharedKernel` → all 6 modules (`AggregateRoot<TId>`, `UserId`, `SmartEnum`, `Permissions`, `IDomainEvent`/`IIntegrationEvent`, `BaseUnitOfWork`). |
| **Customer/Supplier** | An upstream context (supplier) exposes an API/event that a downstream context (customer) consumes; the supplier has an incentive not to break the contract, but the relationship is not as formalized as an Open Host Service. | `source-arrowhead.label: "U"` (Upstream) on the supplier side, `target-arrowhead.label: "D"` (Downstream) on the customer side. | Identity → Research (Integration Events), Identity → Notify (`ISendEmail`), Inventory → Notify (`LowStockDomainEvent`), Scheduling → Notify (scheduling events). |
| **Conformist** | The downstream context conforms to the upstream model without translating it or negotiating a contract — here materialized as a weak `Guid` reference, with no domain class/type coupling. | `source-arrowhead.label: "U"` on the upstream side, `target-arrowhead.label: "CF"` (Conformist) on the downstream side. | Research → Inventory (`ProjectId`, Inventory conforms), Assets → Scheduling (`EquipmentId`, Scheduling conforms). |
| **Open Host Service + Published Language** | The upstream context publishes a stable, well-defined set of interfaces/contracts (a "published language") so that multiple consumers can integrate predictably, instead of exposing its internal model. | `source-arrowhead.label: "OHS, PL"` on the upstream side, `target-arrowhead.label: "CF"` on the downstream side. | Research → Inventory (`IProjectChecker`, `IProjectCatalog`, `IResearcherProfileProvider`), Research → Identity (`IResearcherProfileProvider`). |

**Note on Assets**: it is the most isolated context in the system — it neither publishes nor consumes any Contract or Integration Event; its only functional relationship is as upstream/supplier toward Scheduling via a weak reference (`EquipmentId`, Conformist pattern — `"U"` on `Assets`, `"CF"` on `Scheduling`), besides depending on the Shared Kernel like all the others.

**Visual highlight**: `Inventory` and `Scheduling` are the "core" business modules of the system (stock/orders and lab scheduling) and receive `style.stroke-width: 3` plus a distinct `style.fill` in the `.d2` file. `SharedKernel` receives `style.stroke-dash` because it is not a business module, but shared infrastructure.

## How to view the `.d2`

D2 is a textual DSL with several ways to render graphically:

- **VS Code**: install the official **D2** extension (`terrastruct.d2`), which offers live graphical preview and syntax validation when editing [`context-map.d2`](./context-map.d2).
- **CLI**: install `d2` ([instructions](https://d2lang.com/tour/install)) and run `d2 context-map.d2 context-map.svg` to generate an SVG.
- **Online playground**: paste the contents of [`context-map.d2`](./context-map.d2) into [play.d2lang.com](https://play.d2lang.com) for immediate visualization without installing anything.

The diagram has 7 bounded contexts and 14 relationships.
