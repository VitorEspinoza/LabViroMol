# Arquitetura

[English](./architecture.md) · **Português**

LabViroMol é uma API REST modular em ASP.NET Core, construída para gestão de laboratório de virologia. O sistema segue os princípios de Clean Architecture com um design inspirado em CQRS.

## Estrutura de alto nível

```
LabViroMol/
├── src/
│   ├── LabViroMol.Api/           # Ponto de entrada, composição do programa
│   └── Modules/
│       ├── Assets/               # Equipamentos e manutenção
│       ├── Identity/              # Usuários, roles, permissões
│       ├── Inventory/            # Materiais, kits, pedidos, estoque
│       ├── Notify/               # Notificações e e-mails
│       ├── Research/             # Projetos, publicações, pesquisadores
│       ├── Scheduling/           # Gestão de agendamentos
│       └── Shared/               # Kernel, classes base de infraestrutura
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
└── docker-compose.yaml           # Serviço LibreTranslate
```

## Estrutura interna de módulo

Todo módulo segue o mesmo layout de quatro camadas:

```
Module/
├── Domain/           # Entidades, agregados, value objects, interfaces de repositório
├── Application/      # Commands, queries, handlers, validators, view models
├── Infrastructure/   # DbContext, implementações de repositório, serviços externos
└── Presentation/     # Definições de endpoints Minimal API
```

A camada opcional `Contracts/` é usada quando um módulo precisa expor tipos para comunicação cross-module.

## Decisões arquiteturais centrais

### Mediator / CQRS
Todas as requisições passam por um pipeline de mediator. Commands alteram estado; queries fazem leitura. Um step de pipeline `ValidationBehavior` roda os validadores do FluentValidation antes da execução dos handlers.

### Banco de dados multi-schema
Cada módulo tem seu próprio `DbContext` e schema de banco de dados separado, viabilizando a futura extração para serviços independentes sem mudanças estruturais.

### Minimal APIs
Sem controllers MVC — os endpoints são definidos com métodos de extensão `MapGroup()` por módulo e registrados em `Program.cs`.

### JWT + Autorização baseada em permissões
O ASP.NET Core Identity emite os JWTs. A autorização é aplicada por endpoint via constantes de permissão granulares (ex.: `Permissions.Inventory.MaterialsManage`).

### Domain Events
Os agregados disparam domain events via `AddEvent()`. O `BaseUnitOfWork` os publica em `CompleteAsync()`, permitindo acoplamento fraco entre módulos (ex.: estoque baixo dispara uma notificação).

## Integração com o frontend

O frontend Angular roda separadamente em `http://localhost:4200`. A API está configurada com CORS para aceitar requisições dessa origem. Imagens enviadas são servidas em `/images/*`.
