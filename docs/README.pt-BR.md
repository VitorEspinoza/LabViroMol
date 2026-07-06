# Documentação — LabViroMol

[English](./README.md) · **Português**

Ponto de partida para entender o projeto antes de contribuir. A ordem abaixo constrói o contexto de baixo para cima; se for a sua primeira vez no repositório, siga-a de cima para baixo.

## Por onde começar

**[`architecture.md`](./architecture.pt-BR.md)** — a visão estrutural: como os módulos se organizam, a divisão em camadas (Domain / Application / Infrastructure / Presentation) e as decisões centrais (CQRS via Mediator, banco multi-schema, domain events, Minimal APIs). Vale ler antes de tudo, porque define o vocabulário que o resto da documentação assume.

**[`modules.md`](./modules.pt-BR.md)** — o que o sistema faz. Descreve os 6 módulos de negócio (Assets, Identity, Inventory, Notify, Research, Scheduling) e o módulo transversal Shared, com responsabilidade, entidades principais, features e surface de API de cada um. É o lugar para descobrir a qual módulo uma funcionalidade pertence.

**[`patterns.md`](./patterns.pt-BR.md)** — como o código é escrito. Cataloga os padrões recorrentes: Mediator, Repository, Aggregate Root, Strong IDs, SmartEnum, Unit of Work, Soft Delete, paginação, endpoints Minimal API e autorização por permissão. Código novo deve seguir o que está aqui.

**[`api.md`](./api.pt-BR.md)** — os contratos HTTP. Endpoints por módulo (método, path, descrição, requisito de autenticação), o formato padrão de erros (ProblemDetails) e os status codes usados. É o que o repositório Angular consome; ver [Fronteira com o frontend](#fronteira-com-o-frontend).

**[`getting-started.md`](./getting-started.pt-BR.md)** — rodar o projeto. Pré-requisitos, configuração de secrets, subir o Docker (LibreTranslate, Postgres), aplicar as migrations de cada módulo e iniciar a API.

**[`testing.md`](./testing.pt-BR.md)** — a estratégia de testes: estrutura das suites, bibliotecas, padrões para testes unitários (domínio) e de integração (endpoints HTTP) e como rodar com cobertura.

**[`ci-cd.md`](./ci-cd.pt-BR.md)** — o pipeline de ponta a ponta. Cobre os 12 workflows do GitHub Actions: os gates de PR (build/testes, migration guard, contrato OpenAPI, CodeQL, SCA, gitleaks, Trivy, SBOM, DAST/perf smoke em stack efêmera), o CD (scan-gate → imagens assinadas no GHCR → deploy migrate-first na droplet → release gate via New Relic), os jobs agendados (perf-load, DORA, crons de segurança) e um glossário dos padrões usados. Consulte antes de mexer em qualquer workflow, Dockerfile, compose de CI ou script de deploy.

## Documentação visual

Além dos textos acima, a arquitetura está documentada em diagramas — parte cruzando módulos (em `architecture/`), parte específica de cada módulo (em `src/Modules/<Modulo>/docs/`).

### Cross-module (`docs/architecture/`)

Segue o [C4 Model](https://c4model.com/) para a visão estrutural e os padrões estratégicos de [Domain-Driven Design](https://martinfowler.com/bliki/BoundedContext.html) para o relacionamento entre bounded contexts. São três notações: a maioria em Mermaid (renderiza direto no GitHub); o C4 Model em Structurizr DSL (um modelo único, navegável entre os níveis); o Mapa de Contexto em D2 ([d2lang.com](https://d2lang.com)), com os marcadores de papel (OHS/PL/ACL/CF/SK/U/D) nas pontas das relações, seguindo o "Implementing Domain-Driven Design" do Vaughn Vernon.

Ordem sugerida, do mais amplo ao mais detalhado:

1. **Visão estratégica (DDD)** — [`architecture/context-map/context-map.md`](./architecture/context-map/context-map.pt-BR.md): como os 7 bounded contexts se relacionam, em D2 ([`context-map.d2`](./architecture/context-map/context-map.d2)).
2. **C4 Nível 1 — Contexto** — [`architecture/c4-model/c4-context.md`](./architecture/c4-model/c4-context.pt-BR.md): o sistema, seus usuários e as integrações externas reais (só Gmail SMTP — o LibreTranslate é self-hosted e só aparece a partir do Nível 2). Fonte: [`workspace.dsl`](./architecture/c4-model/workspace.dsl).
3. **C4 Nível 2 — Containers** — [`architecture/c4-model/c4-container.md`](./architecture/c4-model/c4-container.pt-BR.md): as unidades deployáveis (API, frontends, banco, gateway, LibreTranslate).
4. **C4 Nível 3 — Componentes** — [`architecture/c4-model/c4-component.md`](./architecture/c4-model/c4-component.pt-BR.md): os módulos e as peças internas da API.
5. **Visão cross-module** — [`architecture/cross-module-overview.md`](./architecture/cross-module-overview.pt-BR.md): os 4 atores, as 14 Aggregate Roots com as relações entre módulos e as referências de dados sem FK de banco. O detalhe por módulo (casos de uso, classes, ER) fica em `src/Modules/<Modulo>/docs/`.
6. **Implantação** — [`architecture/deployment/deployment.md`](./architecture/deployment/deployment.pt-BR.md): a topologia física de containers no droplet de produção.
7. **Observabilidade** — [`architecture/observability-overview.md`](./architecture/observability-overview.pt-BR.md): o fluxo de telemetria da requisição HTTP até o New Relic, passando pelo Mediator pipeline (`ValidationBehavior`/`LoggingBehavior`), o logging OTel-nativo (`ILogger`), `Activity`/span OTel, OTLP Exporter, além dos caminhos paralelos de Outbox, tradução e e-mail. A configuração e a validação local estão em [`getting-started.md`](./getting-started.pt-BR.md#observabilidade--new-relic).

### Por módulo (`src/Modules/<Modulo>/docs/`)

Cada módulo tem sua pasta `docs/` com os diagramas específicos dele — apenas os tipos que fazem sentido para o módulo (nem todos têm sequência ou estado próprios):

| Módulo         | Pasta                                                                      | Conteúdo                                                                                                            |
| -------------- | -------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| **Identity**   | [`src/Modules/Identity/docs/`](../src/Modules/Identity/docs/README.pt-BR.md)     | Casos de uso, classes, sequência (Login, Refresh Token), ER (`identity`)                                           |
| **Inventory**  | [`src/Modules/Inventory/docs/`](../src/Modules/Inventory/docs/README.pt-BR.md)   | Casos de uso, classes, sequência (7 fluxos, incl. Consumir Material p/ Projeto), estado (`Order`), ER (`inventory`) |
| **Assets**     | [`src/Modules/Assets/docs/`](../src/Modules/Assets/docs/README.pt-BR.md)         | Casos de uso, classes, estado (`MaintenanceRequest`), ER (`assets`)                                                |
| **Research**   | [`src/Modules/Research/docs/`](../src/Modules/Research/docs/README.pt-BR.md)     | Casos de uso, classes e ER divididos em 3 sub-blocos (Projeto / Pesquisador / Publicação)                          |
| **Scheduling** | [`src/Modules/Scheduling/docs/`](../src/Modules/Scheduling/docs/README.pt-BR.md) | Casos de uso, classes, sequência (4 fluxos de agendamento), estado (`Schedule`), ER (`scheduling`)                 |
| **Notify**     | [`src/Modules/Notify/docs/`](../src/Modules/Notify/docs/README.pt-BR.md)         | Casos de uso, classes, ER (`notify`)                                                                               |
| **Shared**     | [`src/Modules/Shared/docs/`](../src/Modules/Shared/docs/README.pt-BR.md)         | Classes do Shared Kernel (`BaseEntity`, `AggregateRoot`, `SmartEnum`, `IStrongId` etc.)                            |

## Resumo rápido

```
Projeto:   LabViroMol — API REST para gestão de laboratório de virologia
Stack:     .NET 10 · ASP.NET Core · EF Core 10 (PostgreSQL/Npgsql) · Mediator · FluentValidation · JWT
Frontend:  Angular — repositório separado, com seus próprios docs/
Módulos:   Assets · Identity · Inventory · Notify · Research · Scheduling · Shared
Padrão:    Clean Architecture por módulo + CQRS via Mediator + Domain Events
Auth:      JWT Bearer + permissões string ("Module.Resource.Action")
Testes:    xUnit + Bogus + NSubstitute · unitários (domínio) + integração (endpoints)
```

## Fronteira com o frontend

O frontend Angular é desenvolvido no próprio repositório dele, que tem seus `docs/`. Este repositório não contém nem coordena o frontend.

O único ponto de integração mantido aqui é o contrato de API em [`api.md`](./api.pt-BR.md). Sempre que um endpoint é criado, alterado ou removido, o `api.md` precisa acompanhar — é ele que o lado Angular usa para manter a integração em dia.
