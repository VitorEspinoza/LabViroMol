# LabViroMol

[English](./README.md) · **Português**

API REST modular para gestão de laboratório de virologia, construída com ASP.NET Core em .NET 10. Cuida do inventário, equipamentos, projetos de pesquisa, agendamento, usuários e notificações do laboratório, e serve tanto um painel administrativo autenticado (Angular) quanto um site institucional público.

## Visão geral

O backend é organizado em módulos de negócio independentes, cada um seguindo Clean Architecture e se comunicando através de um pipeline de mediator. Cada módulo tem seu próprio schema de banco de dados, o que mantém as fronteiras explícitas e deixa a porta aberta para extrair um módulo como serviço independente no futuro, sem remodelar o código.

- **Arquitetura:** Clean Architecture por módulo, CQRS via Mediator, domain events para reações cross-module (ex.: estoque baixo dispara uma notificação).
- **Persistência:** um `DbContext` e um schema PostgreSQL por módulo.
- **API:** Minimal APIs agrupadas por módulo, documentadas via OpenAPI (Scalar UI em `/scalar/v1` em desenvolvimento).
- **Auth:** JWT Bearer com permissões baseadas em string (`Module.Resource.Action`).
- **Observabilidade:** OpenTelemetry (logs, métricas, traces) exportado para o New Relic via OTLP.

## Stack

.NET 10 · ASP.NET Core · EF Core 10 (PostgreSQL / Npgsql) · Mediator · FluentValidation · JWT · OpenTelemetry · Docker · xUnit · NBomber

## Módulos

| Módulo | Responsabilidade |
|--------|----------------|
| **Identity** | Usuários, roles e permissões; autenticação JWT |
| **Inventory** | Materiais, kits, pedidos de compra e movimentação de estoque |
| **Assets** | Equipamentos e solicitações de manutenção |
| **Research** | Projetos, pesquisadores e publicações |
| **Scheduling** | Agendamento de uso do laboratório e seu ciclo de vida |
| **Notify** | Notificações internas e e-mails de saída |
| **Shared** | Shared Kernel: entidades base, aggregate roots, strong IDs, SmartEnum |

## Estrutura do projeto

```
src/
├── LabViroMol.Api/        # Ponto de entrada, composição do host
└── Modules/<Modulo>/
    ├── Domain/            # Entidades, agregados, value objects, interfaces de repositório
    ├── Application/       # Commands, queries, handlers, validators, view models
    ├── Infrastructure/    # DbContext, repositórios, serviços externos
    └── Presentation/      # Endpoints Minimal API
tests/
├── UnitTests/            # Domínio
├── IntegrationTests/     # Endpoints HTTP
└── LoadTests/            # Performance e resiliência (NBomber)
```

## Como começar

Pré-requisitos: .NET 10 SDK e Docker.

```bash
git clone https://github.com/VitorEspinoza/LabViroMol
cd LabViroMol
docker compose up -d                    # PostgreSQL + LibreTranslate
dotnet run --project src/LabViroMol.Api
```

Antes da primeira execução, você também precisa configurar os user secrets e aplicar as migrations de cada módulo. O passo a passo completo está em [`docs/getting-started.md`](./docs/getting-started.pt-BR.md).

## Testes

```bash
dotnet test
```

Os testes unitários cobrem o domínio; os de integração exercitam os endpoints HTTP. Veja [`docs/testing.md`](./docs/testing.pt-BR.md) para a estratégia e [`tests/LoadTests/`](./tests/LoadTests/) para a suíte de carga/resiliência.

## Documentação

Comece por [`docs/README.md`](./docs/README.pt-BR.md) para o índice completo. Links rápidos:

- [Arquitetura](./docs/architecture.pt-BR.md) · [Módulos](./docs/modules.pt-BR.md) · [Padrões](./docs/patterns.pt-BR.md)
- [Referência de API](./docs/api.pt-BR.md) · [Contrato de API](./docs/api-contract.pt-BR.md)
- [Como começar](./docs/getting-started.pt-BR.md) · [Migrations](./docs/migrations.pt-BR.md) · [Testes](./docs/testing.pt-BR.md)
- [CI/CD](./docs/ci-cd.pt-BR.md) · [Métricas DORA](./docs/dora.pt-BR.md) · [Runbooks](./docs/runbooks/)
- Diagramas de arquitetura (C4, mapa de contexto, visões cross-module) em [`docs/architecture/`](./docs/architecture/)

O painel administrativo Angular e o site institucional vivem em seus próprios repositórios; o contrato de integração entre eles e esta API é o [`docs/api.md`](./docs/api.pt-BR.md).
