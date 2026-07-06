# Contrato de API — OpenAPI como fonte única

[English](./api-contract.md) · **Português**

## Decisão

O backend é a **fonte única do contrato de API** (schema-first). A spec OpenAPI
é gerada a partir do código (atributos de minimal API / `AddOpenApi()`) — não
escrita à mão — e versionada em `contracts/openapi.json`, na raiz do
repositório. Pact (consumer-driven contracts) foi descartado: exigiria um
broker/infra dedicada e é over-engineering para o tamanho do time.

Esse modelo cobre o contrato **estrutural** (endpoints, shapes, tipos,
status codes). O contrato **comportamental** (regras de negócio, efeitos
colaterais) continua sendo responsabilidade do E2E de cada frontend.

## Onde está a spec publicada

- **Arquivo:** [`contracts/openapi.json`](../contracts/openapi.json) — sempre
  reflete o estado da `main`.
- **Consumo pelos fronts:** buscar via URL raw do GitHub
  (`https://raw.githubusercontent.com/VitorEspinoza/LabViroMol/main/contracts/openapi.json`)
  ou via clone/submodule, conforme o gerador de client de cada repo
  (NSwag/openapi-typescript/etc. — decisão de cada plano de frontend).
- Não usar o endpoint `/openapi/v1.json` da app em produção como fonte de
  verdade para geração de client: ele só fica mapeado em `Development`
  (`app.MapOpenApi()` em `src/LabViroMol.Api/Program.cs`). A fonte de
  verdade é sempre o arquivo versionado em `contracts/openapi.json`.

## Como a spec é gerada

O pacote `Microsoft.Extensions.ApiDescription.Server` (configurado em
`src/LabViroMol.Api/LabViroMol.Api.csproj`) gera a spec em **build-time**,
via MSBuild target (`dotnet build`), sem subir a aplicação (sem `Listen`,
sem rota HTTP exposta). O processo da tool `dotnet-getdocument` precisa
construir o `IHost` (para descobrir os endpoints registrados via Mediator),
e por isso exige uma `ConnectionStrings:LabViroMol` configurada — mas nenhuma
conexão real é aberta nesse fluxo. No CI isso é suprido com uma connection
string fictícia (`ConnectionStrings__LabViroMol` no `env:` do workflow); ela
nunca é usada para conectar a um banco de fato.

O artefato gerado chama-se `contracts/LabViroMol.Api.json` (nome derivado do
projeto); o workflow renomeia para `openapi.json` antes de publicar/comparar.

Localmente:

```bash
export ConnectionStrings__LabViroMol="Host=localhost;Port=5432;Database=dummy;Username=dummy;Password=dummy"
dotnet build src/LabViroMol.Api/LabViroMol.Api.csproj -c Release
mv contracts/LabViroMol.Api.json contracts/openapi.json
```

## Gate de breaking change (`oasdiff`)

O workflow [`.github/workflows/api-contract.yml`](../.github/workflows/api-contract.yml)
roda em todo PR para `main`:

1. Gera a spec da revisão do PR (`contracts/revision.openapi.json`).
2. Busca a spec publicada na `main` (`contracts/openapi.json` via
   `git show origin/main:...`) como base de comparação. Se não existir ainda
   (primeira execução), o gate é pulado com aviso.
3. Roda `oasdiff breaking base.json revision.json` (imagem Docker
   `tufin/oasdiff`) comparando as duas specs.
4. **Se houver breaking change** (campo removido, tipo alterado, endpoint
   removido, etc.) → o job falha, **a menos que** o PR tenha a label
   `api-breaking-approved` — mesma convenção usada pelo
   [`migration-guard`](../.github/workflows/migration-guard.yml) (plano 14)
   para migrations destrutivas.
5. PRs aditivos (campo novo, endpoint novo) passam sem fricção.

Em `push` para `main`, o job `publish-spec` regenera a spec, sobrescreve
`contracts/openapi.json` e faz commit automático (`[skip ci]`) caso haja
diff — mantendo o arquivo na raiz sempre sincronizado com o código da `main`.

## Política de versionamento / breaking change

- Mudança **aditiva** (novo endpoint, novo campo opcional, novo enum value
  em campo já tratado como string aberta): não é breaking, passa direto.
- Mudança **breaking** (campo obrigatório novo, remoção/rename de
  campo/endpoint, mudança de tipo, mudança de status code default): exige
  decisão consciente — adicionar a label `api-breaking-approved` no PR e
  **avisar os times de frontend consumidores** (Admin Panel, Institucional)
  antes do merge, já que o client deles vai quebrar a próxima vez que
  regenerarem a partir do contrato atualizado.
- Recomenda-se abrir o PR breaking primeiro como rascunho/draft pra dar
  tempo do front se preparar, principalmente em mudanças de autenticação ou
  endpoints de uso amplo (`/api/identity/*`, `/api/inventory/*`).

## Cobertura de response bodies (GET)

A geração inicial da spec (plano 27) só capturava `requestBody` de comandos
de escrita — minimal APIs do ASP.NET Core só inferem o schema de resposta no
OpenAPI quando o handler declara um tipo de retorno concreto (`Results<Ok<T>,
NotFound>`) ou usa `.Produces<T>(...)` explicitamente. 43 endpoints `GET`
ficavam com `content?: never` na spec gerada (sem schema de 200), o que
impedia type-safety nos clients tipados dos fronts para os fluxos de leitura.

Todos os endpoints `GET` da spec hoje anotam o schema de resposta real via
`.Produces<TViewModel>(StatusCodes.Status200OK)` (mesmo padrão já usado pelos
endpoints de escrita) — incluindo `404`/`403` quando aplicável. Os endpoints
de relatório em PDF (`/api/inventory/reports/*.pdf`) anotam
`.Produces<FileContentHttpResult>(StatusCodes.Status200OK, "application/pdf")`
em vez de schema JSON, já que retornam binário.

Cobertura: **43 endpoints sem schema → 0** (verificado em `contracts/openapi.json`
após build; nenhum `GET` na spec final tem `content` ausente ou vazio em `200`).

## Para os times de frontend

- O client tipado deve ser gerado a partir de
  `contracts/openapi.json` da `main` (não do endpoint runtime).
- Drift estrutural entre o client gerado e o backend real vira erro de
  *typecheck* no CI do front, automaticamente, sempre que o contrato for
  regenerado a partir de uma versão mais nova do `openapi.json`.
- Caso o backend introduza um breaking change aprovado (label
  `api-breaking-approved`), o time de frontend será avisado fora de banda
  (PR description / comunicação direta) — o gate `oasdiff` não bloqueia o
  merge no backend quando a label está presente, então a comunicação manual
  é obrigatória nesse caso.
