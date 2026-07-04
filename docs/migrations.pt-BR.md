# Migrações de banco de dados — fluxo de produção (EF Core migrations bundle)

[English](./migrations.md) · **Português**

## Visão geral

O LabViroMol tem **6 DbContexts** (um por módulo de negócio), cada um com seu próprio histórico de migrações EF Core, em schemas Postgres separados:

| Módulo | DbContext | Projeto |
|---|---|---|
| Identity | `LabViroMolIdentityDbContext` | `src/Modules/Identity/Infrastructure` |
| Inventory | `InventoryDbContext` | `src/Modules/Inventory/Infrastructure` |
| Assets | `AssetsDbContext` | `src/Modules/Assets/Infrastructure` |
| Research | `ResearchDbContext` | `src/Modules/Research/Infrastructure` |
| Scheduling | `SchedulingDbContext` | `src/Modules/Scheduling/Infrastructure` |
| Notify | `NotifyDbContext` | `src/Modules/Notify/Infrastructure` |

Em vez de instalar o SDK do .NET + a ferramenta `dotnet-ef` no container que roda em produção (abordagem antiga, pesada e ambígua com múltiplos DbContexts), usamos **EF Core migrations bundle**: um executável *self-contained* por DbContext, gerado em build-time, que aplica as migrações daquele módulo sem precisar do SDK no runtime.

## Como gerar os bundles localmente

Pré-requisitos: .NET SDK 10 e a ferramenta `dotnet-ef` instalada (`dotnet tool install --global dotnet-ef`).

```bash
bash scripts/ci/build-migration-bundles.sh [output_dir] [runtime_identifier]
# padrão: output_dir=./artifacts/migrate, runtime_identifier=linux-x64
```

Gera 6 executáveis self-contained (`efbundle-identity`, `efbundle-inventory`, `efbundle-assets`, `efbundle-research`, `efbundle-scheduling`, `efbundle-notify`) em `<output_dir>`.

## Imagem Docker (`Dockerfile.migrate`)

Multi-stage:
- **Stage `build`** (`mcr.microsoft.com/dotnet/sdk:10.0`): restaura a solução, instala `dotnet-ef`, roda `build-migration-bundles.sh`.
- **Stage `final`** (`mcr.microsoft.com/dotnet/runtime-deps:10.0`): copia só os 6 executáveis + o script de execução. **Sem SDK, sem código-fonte, sem `dotnet-ef`** na imagem final.

```bash
docker build -f Dockerfile.migrate -t labviromol-migrate:local .
docker run --rm -e DB_CONNECTION_STRING="Host=...;Port=5432;Database=...;Username=...;Password=..." labviromol-migrate:local
```

## Ordem de execução

Os bundles são executados **em sequência**, sempre nesta ordem:

```
identity → inventory → assets → research → scheduling → notify
```

Identity primeiro porque os demais módulos referenciam usuários/papéis por convenção de integração (mesmo sem FK física entre schemas). A ordem é codificada em dois lugares que precisam ficar sincronizados:
- `scripts/ci/build-migration-bundles.sh` (array `MODULE_ORDER`, usado só pra geração — a ordem de geração não importa tecnicamente, mas é mantida igual à de execução por clareza)
- `scripts/ci/run-migration-bundles.sh` (variável `BUNDLES`, usado na execução real — **esta ordem importa**)

## Contrato de variável de ambiente

O entrypoint (`run-migration-bundles.sh`) espera a connection string Npgsql completa na variável de ambiente **`DB_CONNECTION_STRING`** (valor cru, passado como `--connection` para cada bundle — **não** é o formato `ConnectionStrings__LabViroMol` usado pela API via `IConfiguration`, já que o bundle não passa pelo binding de configuração do ASP.NET, é uma chamada de CLI direta).

Qualquer serviço de compose que rode esta imagem (`migrate`) precisa setar `DB_CONNECTION_STRING` diretamente — **não** `ConnectionStrings__LabViroMol`.

## Gate de exit code (crítico para o deploy)

`run-migration-bundles.sh` **para no primeiro bundle que falhar** e propaga o exit code dele para o processo pai. Isso é o que permite ao pipeline de deploy detectar falha de migração e abortar o rollout da API **antes** de trocar a versão em produção — a API antiga continua servindo se a migração falhar.

## Regra de migrações: backward-compatible (expand/contract)

Como não há rollback automático de imagem quando uma migração já alterou o schema, toda migração nova deve ser **aditiva/backward-compatible**: a versão *anterior* da API precisa continuar funcionando enquanto a migração nova já foi aplicada (janela de deploy). Praticamente:

- Adicionar coluna: sempre nullable ou com default, nunca `NOT NULL` sem default numa coluna existente.
- Remover coluna/tabela: só depois de um deploy anterior já ter parado de usá-la (padrão *expand* primeiro, *contract* depois, em deploys separados).
- Renomear: trate como "adicionar nova + parar de usar a antiga + remover a antiga depois" — nunca um rename atômico que quebre a versão anterior.

Se uma migração violar isso e a API nova falhar no health check pós-deploy, a política é **fix-forward** (corrigir e fazer novo deploy), não rollback automático.
