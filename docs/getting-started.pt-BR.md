# Como começar

[English](./getting-started.md) · **Português**

## Pré-requisitos

- .NET 10 SDK
- Docker (para PostgreSQL e LibreTranslate)

## Configuração

### 1. Clone o repositório

```bash
git clone https://github.com/VitorEspinoza/LabViroMol
cd LabViroMol
```

### 2. Suba o banco de dados e o serviço de tradução

```bash
docker compose up -d
```

Isso inicia o PostgreSQL 17 na porta 5432 e o LibreTranslate na porta 5000.

### 3. Configure os secrets

A API usa User Secrets do ASP.NET Core. Defina os valores necessários:

```bash
cd src/LabViroMol.Api
dotnet user-secrets set "ConnectionStrings:LabViroMol" "Host=localhost;Port=5432;Database=LabViroMol;Username=labviromol;Password=labviromol_dev"
dotnet user-secrets set "Jwt:Key" "<your-secret>"
dotnet user-secrets set "Jwt:Issuer" "<issuer>"
dotnet user-secrets set "Jwt:Audience" "<audience>"
dotnet user-secrets set "Email:Host" "<smtp-host>"
dotnet user-secrets set "Email:Port" "587"
dotnet user-secrets set "Email:Username" "<email>"
dotnet user-secrets set "Email:Password" "<password>"
```

### 4. Aplique as migrations

Cada módulo tem seu próprio `DbContext`. Rode as migrations de cada um:

```bash
dotnet ef database update --project src/Modules/Identity/Infrastructure --startup-project src/LabViroMol.Api --context LabViroMolIdentityDbContext
dotnet ef database update --project src/Modules/Inventory/Infrastructure --startup-project src/LabViroMol.Api --context InventoryDbContext
dotnet ef database update --project src/Modules/Assets/Infrastructure --startup-project src/LabViroMol.Api --context AssetsDbContext
dotnet ef database update --project src/Modules/Research/Infrastructure --startup-project src/LabViroMol.Api --context ResearchDbContext
dotnet ef database update --project src/Modules/Scheduling/Infrastructure --startup-project src/LabViroMol.Api --context SchedulingDbContext
dotnet ef database update --project src/Modules/Notify/Infrastructure --startup-project src/LabViroMol.Api --context NotifyDbContext
```

### 5. Rode a API

```bash
dotnet run --project src/LabViroMol.Api
```

A API sobe em `https://localhost:7xxx` (porta exibida no console). O explorador interativo de API (Scalar) fica disponível em `/scalar/v1` no ambiente de desenvolvimento.

## Frontend

O frontend Angular é um repositório separado e é esperado que rode em `http://localhost:4200`. O CORS já está pré-configurado para essa origem.

## Rodando a stack completa localmente (Docker Compose)

Os passos acima sobem só a API, contra um Postgres/LibreTranslate no Docker. Para rodar tudo do jeito que roda em produção — API + os dois frontends (painel admin Angular, site institucional Next.js) + um gateway nginx na frente dos três — mas inteiramente na sua máquina, a partir do seu próprio código-fonte local, use o `docker-compose.override.yml`. Ele já está versionado neste repositório, e o Docker Compose o mescla automaticamente junto com o `docker-compose.yaml`; não tem nada para habilitar.

### 1. Clone os três repositórios lado a lado

O arquivo de override builda os dois frontends a partir do código-fonte, referenciando-os por caminho relativo — então os três repositórios precisam ficar lado a lado, com exatamente estes nomes de pasta:

```
alguma-pasta/
├── LabViroMol/                   # este repositório
├── LabViroMol-Admin-Panel/       # painel admin Angular
└── labviromol-institucional/     # site institucional Next.js
```

```bash
git clone https://github.com/VitorEspinoza/LabViroMol
git clone https://github.com/VitorEspinoza/LabViroMol-Admin-Panel
git clone https://github.com/VitorEspinoza/labviromol-institucional
```

### 2. Configure o `.env`

```bash
cd LabViroMol
cp .env.example .env
```

Todo valor que os containers precisam vem deste único arquivo — não existe um banco de dados externo para provisionar antes, nem uma connection string para ir buscar em algum lugar. É o próprio `docker-compose.yaml` que cria o container `postgres`, usando o que você colocar em `POSTGRES_USER`/`POSTGRES_PASSWORD`/`POSTGRES_DB` no `.env`; o `DB_CONNECTION_STRING` da API, nesse mesmo arquivo, só precisa referenciar esses três mesmos valores, mais `Host=postgres` (o nome do container na rede do Docker — não `localhost`, já que containers se alcançam pelo nome do serviço, não pelo endereço de loopback da máquina hospedeira). O `.env.example` já mantém isso consistente por padrão:

```
POSTGRES_USER=labviromol
POSTGRES_PASSWORD=labviromol_dev
POSTGRES_DB=LabViroMol
DB_CONNECTION_STRING=Host=postgres;Port=5432;Database=LabViroMol;Username=labviromol;Password=labviromol_dev
```

Você pode deixar esses três exatamente como estão para uso local — seja qual for o valor escolhido, o Postgres se inicializa com eles na primeira vez que seu volume é criado, então, contanto que `POSTGRES_*` e `DB_CONNECTION_STRING` concordem entre si, não existe um banco "já existente" para bater com nada. O único valor que você precisa mesmo trocar é o `JWT_KEY` (qualquer string aleatória com 32+ caracteres) — a API se recusa a iniciar sem um valor real. `EMAIL_*` e `NR_LICENSE_KEY` podem ficar com os valores de exemplo; a API sobe normalmente sem um SMTP funcional ou uma conta New Relic, essas integrações simplesmente não fazem nada.

### 3. Builde e suba tudo

```bash
docker compose up -d --build
```

Isso builda a API a partir do `Dockerfile` deste repositório, o painel admin a partir de `../LabViroMol-Admin-Panel`, e o site institucional a partir de `../labviromol-institucional` (tudo declarado no `docker-compose.override.yml`), e então sobe o Postgres, o LibreTranslate, os três containers de aplicação e um gateway nginx (`nginx/gateway.local.conf`) na frente de tudo, na porta 80.

### 4. Aplique as migrations

O serviço `migrate` também sobe junto com o `docker compose up -d` (ele roda uma vez e sai sozinho), mas você pode rodá-lo explicitamente a qualquer momento:

```bash
docker compose run --rm migrate
```

### 5. Acesse a stack

| URL | O quê |
|---|---|
| `http://localhost/` | Site institucional (Next.js) |
| `http://localhost/gestao-lab-ufpr/` | Painel admin (Angular) |
| `http://localhost/api/...` | API |

Como tudo é servido a partir da mesma origem (`http://localhost`) através do gateway, `CORS_ORIGIN_ADMIN`/`CORS_ORIGIN_INSTITUCIONAL` no `.env` não entram em jogo aqui — eles só importam quando um frontend roda diretamente (`ng serve`/`next dev`), acessando a API numa porta diferente.

### Dados de seed para desenvolvimento

Na primeira vez que a API sobe em `Development` contra um banco vazio, ela automaticamente popula cerca de 20 registros válidos por módulo (usuários, materiais, equipamentos, projetos, agendamentos etc.) para já ter o que testar na tela — ver `src/LabViroMol.Api/DevSeed/DevSeeder.cs`. Roda uma única vez (pula se o banco já tiver dados) e registra as credenciais do admin no log ao concluir:

```
DevSeed: done. Log in with admin@labviromol.local / Labviromol@123
```

Desative definindo `DevSeed:Enabled` como `false` em `appsettings.Development.json`, ou via a variável de ambiente `DevSeed__Enabled=false`.

## Referência de configuração

Valores-chave em `appsettings.json`:

| Chave | Descrição |
|-----|-------------|
| `Storage:RootFolder` | Caminho raiz para os arquivos enviados (`Upload/Images`) |
| `Storage:Folders:Equipments` | Subpasta para imagens de equipamentos |
| `Storage:Folders:ScheduleTerms` | Subpasta para arquivos de termo de agendamento |
| `Translation:IntervalMinutes` | Frequência com que o job de tradução em background roda |
| `Frontend:BaseUrl` | URL do app Angular para CORS e links de e-mail |
| `OpenTelemetry:OtlpEndpoint` | Endpoint OTLP para logs/traces/métricas (fallback via appsettings; ver "Observabilidade / New Relic" abaixo) |
| `OpenTelemetry:Tracing:SamplingRatio` | Taxa de amostragem de traces (`0.0`–`1.0`, default `1.0`) |
| `Observability:SlowQueryMs` | Limite em ms acima do qual uma query EF Core/Npgsql é logada como `Warning` (default `500`) |

## Observabilidade / New Relic

A API exporta logs, métricas e traces via OpenTelemetry (OTel) para a New Relic usando o
protocolo OTLP. Logging é OTel-nativo: `Microsoft.Extensions.Logging` (`ILogger<T>`) com o
provider `Logging.AddOpenTelemetry()`, sem Serilog. O restante do pipeline
(OTel SDK, sampler, nomes de métricas, níveis de log, política de PII) segue as convenções de
instrumentação do projeto — vale conhecê-las antes de instrumentar código novo. O diagrama de
fluxo completo (request HTTP → Mediator
pipeline → ILogger/Activity → OTel SDK → OTLP → New Relic, incluindo Outbox, tradução e e-mail)
está em [`architecture/observability-overview.md`](./architecture/observability-overview.pt-BR.md).
Timing de request HTTP vive apenas no span (instrumentação automática de ASP.NET Core) — não há
log de request dedicado.

Esta seção cobre apenas o necessário para configurar e validar a telemetria localmente.

### Variáveis de ambiente

A API lê as variáveis padrão do OTel (convenção do .NET, sem necessidade de código adicional) e
uma específica da New Relic, todas já declaradas em `.env.example` e repassadas pelo
`docker-compose.yaml` ao serviço `api`:

| Variável | Descrição | Default em dev |
|---|---|---|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Endpoint OTLP (gRPC/HTTP) para onde logs/traces/métricas são exportados. Sem isso configurado, nenhum exporter/sink OTLP é registrado e a API sobe normalmente só com log de console. | vazio (telemetria desligada) |
| `OTEL_EXPORTER_OTLP_HEADERS` | Headers de autenticação do OTLP. Em produção é montado automaticamente como `api-key=${NR_LICENSE_KEY}`. | vazio |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | Protocolo OTLP (`http/protobuf` ou `grpc`). | `http/protobuf` |
| `OTEL_EXPORTER_OTLP_COMPRESSION` | Compressão do payload OTLP. | `gzip` |
| `OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE` | Temporalidade de métricas — a New Relic exige `delta` (o .NET usa `cumulative` por padrão). | `delta` |
| `NR_LICENSE_KEY` | License key de ingest da New Relic. **Segredo** — nunca commitar valor real nem logar. | vazio |

Alternativamente, o endpoint pode ser configurado via `appsettings`/User Secrets em
`OpenTelemetry:OtlpEndpoint` (lido por `ObservabilityExtensions.ResolveOtlpEndpoint`) — a env var
`OTEL_EXPORTER_OTLP_ENDPOINT` tem precedência menor e funciona como fallback caso a chave de
configuração não esteja definida.

### Obtendo a license key da New Relic

1. Crie uma conta (ou use uma existente) em [newrelic.com](https://newrelic.com).
2. No painel, vá em **API keys** (menu de perfil/organização) e copie a **license key** de ingest
   (não a *User key*, que é para a API REST da New Relic, e sim a key usada por agentes/OTLP).
3. Defina `NR_LICENSE_KEY` no `.env` local (nunca no `.env.example` ou em qualquer arquivo
   versionado).

### Validando localmente

Duas opções, da mais simples à mais fiel ao ambiente de produção:

**Opção 1 — sem exportar nada (default):** não defina `OTEL_EXPORTER_OTLP_ENDPOINT`. A API sobe
normalmente, o provider de console padrão do `Microsoft.Extensions.Logging` escreve no console, e
o OTel SDK roda sem nenhum exporter OTLP registrado (ver
`ObservabilityExtensions.AddObservabilityTelemetry` — o bloco `AddOtlpExporter` só é adicionado
quando `otlpEndpoint` não é vazio). Suficiente para validar que logs e métricas estão sendo
gerados sem precisar de nenhum backend externo.

**Opção 2 — apontar para a New Relic de verdade:** defina `OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.nr-data.net`
e `NR_LICENSE_KEY` com uma key válida, suba a API (local ou via `docker compose up -d`) e gere
tráfego (chamadas aos endpoints, um ciclo de Outbox, um job de tradução). Em poucos minutos os
dados aparecem em **APM & Services** (traces), **Logs** e **Metrics & events** no painel da New
Relic, filtrando por `service.name = labviromol-api`.

Para um collector OTel local (ex.: para inspecionar payloads antes de gastar quota de ingest da
New Relic), aponte `OTEL_EXPORTER_OTLP_ENDPOINT` para o endpoint do seu
[OpenTelemetry Collector](https://opentelemetry.io/docs/collector/) local (ex.:
`http://localhost:4317` para gRPC) e use o exporter `debug`/`logging` do Collector para imprimir
o que está sendo recebido — a API não precisa de nenhuma alteração de código para isso, é só
trocar a env var.
