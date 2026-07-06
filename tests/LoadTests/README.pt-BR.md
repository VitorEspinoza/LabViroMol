# Testes de Performance & Resiliência — Guia para quem está chegando

[English](./README.md) · **Português**

Este documento explica **o que** é cada peça desta suíte, **por que** ela foi feita assim e
**qual o objetivo geral**. Foi escrito para alguém que nunca mexeu com teste de carga. Leia do começo:
os conceitos da seção 2 são usados no resto inteiro.

---

## 1. Objetivo geral

Queremos responder, com números e não com "achismo", três perguntas sobre a API LabViroMol:

1. **Ela aguenta o uso esperado do dia a dia?** (carga nominal — atende ao SLO?)
2. **Quando ela quebra, quebra com quanto?** (ponto de saturação — qual o RPS de ruptura?)
3. **Ela degrada sozinha com o tempo?** (memory leak / GC ao longo de 1h — soak)

E, principalmente: **quando quebrar, queremos saber QUEM quebrou primeiro** — CPU? banco? memória?
Por isso a suíte foi desenhada junto com a observabilidade (OpenTelemetry → New Relic) que já existe no
projeto. O teste de carga é o "lado de fora" (quantas requisições por segundo, qual a latência); a
telemetria é o "lado de dentro" (CPU, pool de conexões, GC). Cruzando os dois, a gente acha o gargalo.

Ferramenta escolhida: **NBomber** (biblioteca .NET). Motivo: é a mesma stack do projeto, então
reaproveitamos os DTOs, os contratos dos endpoints e os geradores de dados (Bogus) que já usamos nos
testes.

---

## 2. Conceitos que você precisa conhecer

| Termo | O que é | Por que importa aqui |
|---|---|---|
| **SLO** | *Service Level Objective* — a meta de qualidade ("p95 ≤ 50 ms") | É o critério de aprovação/reprovação |
| **Threshold (T)** | O limite de latência aceitável de uma operação | Escrita = 200 ms, leitura simples = 50 ms, etc. |
| **4T** | Quatro vezes o T — a "zona tolerável" | Acima disso o usuário fica frustrado |
| **Percentil (p95/p99)** | "95% das requisições foram mais rápidas que X" | Média esconde os outliers; percentil não. p95 é o SLO; p99 mede *jitter* (instabilidade) |
| **Throughput / RPS** | Requisições por segundo sustentadas | Mede capacidade |
| **Apdex** | Índice 0–1 que resume satisfação: (satisfeitos + tolerados/2) / total | Um número só para dizer "está bom?" (≥ 0,90 nominal) |
| **Warm-up** | Período inicial descartado | O .NET compila o código sob demanda (JIT) e enche o pool de conexões nos primeiros segundos; medir isso sujaria o resultado |
| **Modelo fechado** | N "usuários virtuais" que esperam a resposta antes de mandar a próxima | Bom para simular uso real estável |
| **Modelo aberto** | Injeta X req/s **independente** de a API estar lenta | **Obrigatório no stress**: se a API trava, continuamos batendo — é assim que se acha o ponto de ruptura |
| **Coordinated omission** | Erro clássico: no modelo fechado, se a API trava, você manda menos requisições e "não vê" a lentidão | É o motivo de usarmos modelo aberto no stress |

### Os tipos de teste (perfis)

- **smoke** — 30 s, carga mínima. Só confirma "está de pé e respondendo 2xx?" antes de gastar tempo.
- **load** — rampa até a carga nominal (~10 min). Valida o SLO do dia a dia.
- **soak** — carga nominal constante por **60 min**. Caça *memory leak* e degradação do Garbage Collector.
- **stress** — injeta cada vez mais (modelo aberto) até degradar. Acha o RPS de ruptura.
- **spike** — salto abrupto de carga. Testa resiliência a pico súbito.
- **breakpoint** — rampa contínua até quebrar. Acha o teto por operação.

---

## 3. Como as peças se encaixam (fluxo)

```
Program.cs
  │  lê argumentos da linha de comando (--scenario, --profile, --campaign, ...)
  │  lê appsettings.json  →  LoadTestConfig
  ├─ comando "seed"?   →  Seeder      (popula o banco)
  ├─ comando "reset"?  →  Reset       (limpa o banco via Respawn)
  │
  ├─ HttpClientFactory →  cria UMA HttpClient compartilhada
  ├─ AuthClient        →  faz login e guarda um POOL de tokens
  ├─ LoadTestRuntime   →  carrega o catálogo de seed + tokens; guarda estado e métricas
  ├─ ScenarioCatalog   →  escolhe os cenários conforme --scenario
  │
  └─ NBomberRunner.Run()  →  executa os cenários
         └─ ResultExporter →  grava reports/<campanha>/<perfil>/<cenário>/summary.json
```

---

## 4. As peças, uma por uma

### `LoadTestConfig.cs` + `appsettings.json`

Centraliza toda a configuração: URL da API, tuning de rede, dados de login, volume de seed e os
**perfis** (smoke/load/stress/...). Cada perfil define `WarmUpSeconds`, `DurationSeconds`,
`ClosedCopies` (nº de usuários no modelo fechado) e `OpenRate` (req/s no modelo aberto).

> **Por que em config e não no código?** Para ajustar a intensidade do teste sem recompilar — você troca
> o `OpenRate` do stress no JSON e roda de novo.

### `CommandLineOptions.cs`

Lê argumentos no formato `--chave=valor`. Os principais:
`--command` (run/seed/reset), `--scenario`, `--profile`, `--campaign`, `--keepAlive`,
`--resetBeforeRun`, `--baseUrl`. É o que permite uma única build rodar qualquer combinação.

### `HttpClientFactory.cs` — a parte mais sutil

Cria **uma única** `HttpClient` sobre um `SocketsHttpHandler`. Decisões importantes:

- **`UseCookies = false`**: não queremos um "pote de cookies" compartilhado. Mandamos a autenticação
  no cabeçalho `Authorization: Bearer` (ver `AuthClient`).
- **Uma instância só, reutilizada**: este é o ponto que evita o **esgotamento de portas efêmeras**. Se
  criássemos uma `HttpClient` por requisição (erro clássico), a milhares de req/s o sistema operacional
  ficaria sem portas TCP (cada conexão fechada fica ~60 s em `TIME_WAIT`) e o **agente de carga**
  quebraria com `SocketException` **antes** da API — mediríamos o limite da máquina errada.
- **`MaxConnectionsPerServer = 1024`**: dimensiona o pool de conexões reutilizáveis.
- **Toggle keep-alive** (`--keepAlive=false`): por padrão keep-alive fica **ligado** (reusa conexão). Há
  a opção de desligar **só** para medir o custo do *handshake* TLS (cada requisição abre conexão nova).
  Nessa variação o esgotamento de portas volta a importar — por isso o ajuste de SO fica na campanha
  (ver seção 6).

### `AuthClient.cs`

Faz login uma vez por usuário de teste em `POST /api/identity/users/login` e extrai o token do cookie
`X-Access-Token` da resposta. Depois, todas as requisições usam esse token via header **Bearer**.

> **Por que Bearer e não o cookie que a API devolve?** A API aceita os dois — o cookie é só *fallback*
> (o `OnMessageReceived` no `Identity/Infrastructure/InfrastructureModule.cs` só lê o cookie se o header
> Bearer estiver ausente). Usar Bearer nos deixa ter **vários usuários** com **uma `HttpClient` só**
> (resolve auth + reuso de conexão ao mesmo tempo). O token dura 2 h, então cobre até o soak de 60 min
> sem precisar renovar.

### `Seeder.cs` + `SeedCatalog.cs`

Popula o banco com **volume realista** (milhares de registros) para que os GETs paginados exercitem
índices e paginação de verdade — testar contra banco vazio não vale nada.

Cuidados embutidos:
- **Gravação em lotes** (`Batches = 500`) com `SaveChangesAsync()` + `ChangeTracker.Clear()` e
  `AutoDetectChangesEnabled = false`. **Por quê?** Se gerássemos tudo e desse um `SaveChanges` só, o EF
  Core seguraria todos os objetos rastreados na memória — fica lento e pode estourar memória (OOM) **no
  processo do seeder**.
- **Dados *stateful*** pré-criados: agendamentos no estado **pendente** (para o cenário de aprovação),
  projetos com vagas, materiais. Sem isso, o cenário de escrita não teria o que aprovar.
- **`SeedCatalog`** salva num JSON os IDs gerados (equipamentos, projetos, agendamentos pendentes...).
  Os cenários leem esse catálogo para saber **em quais IDs reais** bater, sem ter que consultar o banco
  em tempo de teste.

### `Reset.cs`

Usa o **Respawn** para truncar os schemas dos módulos (`identity`, `inventory`, `research`,
`scheduling`, `assets`, `notify`) entre execuções. Como a tabela `OutboxMessages` mora **dentro de cada
schema de módulo**, ela também é limpa — não sobram mensagens de outbox de uma rodada para outra.

> **Cuidado:** o Reset apaga **tudo** desses schemas. Nunca rode contra um banco de produção que já
> tenha dados de cliente.

### `LoadTestRuntime.cs` — o cérebro em tempo de execução

Guarda o estado compartilhado e as métricas durante o teste:

- **Pool de tokens** (`NextToken`): distribui os tokens dos usuários em *round-robin*.
- **Fila de agendamentos pendentes** (`NextPendingScheduleIdAsync`): cada aprovação consome um ID
  pendente (não dá para aprovar o mesmo duas vezes); quando a fila esvazia, o runtime **reabastece**
  criando mais 500 no banco.
- **`CreateRequest`**: monta a requisição já com o header Bearer.
- **`SendAsync`**: envia, cronometra, e **registra duas coisas**:
  - o **status code** por operação (`RecordStatus`) → vira o *breakdown* no relatório;
  - o **Apdex** por operação (`RecordApdex`) → classifica cada resposta em satisfeito (≤ T),
    tolerado (≤ 4T) ou frustrado (> 4T).
- **`CreateLoadSimulations`**: decide o **modelo de carga**. Para `stress/spike/breakpoint` usa
  **aberto** (`RampingInject` + `Inject`); para o resto usa **fechado** (`RampingConstant` +
  `KeepConstant`). É aqui que o "modelo aberto no stress" da seção 2 acontece de verdade.

### Os cenários (`Scenarios/`)

Cada cenário é um conjunto de requisições + seus **thresholds** (SLOs). Todos começam com warm-up.

| Cenário | O que faz | Threshold p95 | p99 ≤ |
|---|---|---|---|
| `ReadSimpleScenarios` | GETs paginados (materials, equipments, schedules, projects) | 50 ms | 200 ms |
| `ReadComplexScenarios` | Dashboard administrativo cross-module | 150 ms | 600 ms |
| `InstitutionalReadScenarios` | Navegação pública do institucional (equipamentos, projetos, publicações, parceiros, pesquisadores) | 800 ms | 2 s |
| `WriteScenarios` | Aprovar agendamento, add/trocar membro, criar material | 200 ms | 800 ms |
| `MixedWorkloadScenario` | Combina os de cima na proporção **~70/10/20** (leitura/leitura-pesada/escrita) | por operação | por operação |
| `MixedPublicAdminScenario` | 2 fluxos agregados no mesmo teste: institucional e administrativo, com concorrência separada | por operação | por operação |
| `ResilienceScenarios` | Burst no endpoint público de agendamento; **espera receber 429** | — | — |

Em cada cenário há três thresholds: **taxa de erro < 0,1%**, **p95 ≤ T** e **p99 ≤ 4T**.

> **Por que p99 ≤ 4T e não "latência máxima ≤ 4T"?** A latência *máxima* é o pior request isolado — uma
> pausa de GC ou o JIT inicial já estouram esse valor, reprovando a rodada por puro ruído. O **p99**
> representa a cauda real (1% mais lento) sem se deixar dominar por um único outlier. A "zona tolerável"
> (4T) também é medida formalmente pelo Apdex; o threshold de p99 é o complemento de SLO.

O **peso** do mix administrativo (`WithWeight(18/5/7)`) distribui a carga entre leituras simples,
dashboard e escritas. Relatórios analíticos pesados não fazem parte da suíte de carga interativa.

Para capacidade pública, `public-read` usa um fluxo agregado de navegação institucional: o valor de
`ClosedCopies` representa usuários virtuais institucionais simultâneos. Para carga conjunta,
`mixed-public-admin` registra dois fluxos no mesmo teste: `InstitutionalClosedCopies` para usuários
institucionais e `AdminClosedCopies` para usuários administrativos. Os perfis `institutional-100`,
`institutional-200`, `joint-100-20` e `joint-150-30` também definem `MinThinkTimeSeconds` e
`MaxThinkTimeSeconds`, modelando a frequência de navegação entre ações.

### `ResultExporter.cs`

Ao final, grava um `summary.json` com: campanha/perfil/cenário, carga configurada, duração, usuários
virtuais, think time, **RPS aproximado**, RPS por grupo (`Institutional`, `Admin`, `Dashboard`,
`PublicWrite`), **breakdown de status code**, **Apdex por operação** e percentis de cada cenário.
É o insumo para a análise (seção 7).

---

## 5. O lado da API: o "modo LoadTest"

O teste não muda só o cliente — a **API sobe num modo especial** quando
`ASPNETCORE_ENVIRONMENT=LoadTest` (que carrega `src/LabViroMol.Api/appsettings.LoadTest.json`). Esse modo:

| Ajuste | Onde | Por quê |
|---|---|---|
| **Email vira NoOp** | `LoadTest:UseNoOpEmail` → `NoOpEmailSender` | Não queremos disparar e-mail real (Gmail) nem esperar SMTP — isso distorceria a latência e poderia mandar e-mail de verdade |
| **Tradução continua real** | `LoadTest:UseNoOpTranslator=false` → `LibreTranslator` | LibreTranslate é self-hosted (não é dependência externa como o SMTP) e roda assíncrono via Outbox a cada poucos segundos, independente da carga HTTP. Mantê-lo real captura a disputa de CPU/RAM com a API no mesmo host, que é exatamente o que a Campanha A quer medir |
| **Pool do Postgres = 20** | `LoadTest:NpgsqlMaxPoolSize` → `ConnectionStringResolver` | Fixar um pool pequeno (abaixo do `max_connections=50` do Postgres) torna a **saturação do pool** um ponto de medição claro e correlacionável |
| **Sampling de traces = 0,1** | `OpenTelemetry:Tracing:SamplingRatio` | Sob carga, gravar 100% dos traces é caro e inunda o New Relic. Métricas continuam em 100% (são agregadas, baratas) |

> O Outbox (processamento assíncrono interno) **continua ligado** — ele faz parte do que queremos medir.
> Acompanhamos o backlog pelo gauge `outbox.pending`.

### Ablation do LibreTranslate

Como o tradutor real fica ligado por padrão, existe um overlay extra pra isolar exatamente quanto ele
custa: `docker-compose.loadtest.noop-translate.yaml`, que sobrescreve `LoadTest:UseNoOpTranslator` para
`true` via variável de ambiente. Combine com a campanha desejada:

```bash
docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml -f docker-compose.loadtest.noop-translate.yaml up -d
```

Rode o mesmo perfil/cenário com e sem esse overlay (usando `--campaign` diferentes, ver seção 7) para
obter o delta de throughput/latência atribuível só à tradução.

---

## 6. As duas campanhas (A e B) — o experimento

A API de produção roda **limitada** no `docker-compose.yaml`: **0,5 vCPU / 384 MB**. Esses limites são o
que torna os resultados **portáveis** (o mesmo compose se comporta igual em qualquer host — ver seção 9).

Para entender o impacto desses limites, rodamos **duas campanhas** que diferem **apenas nos recursos**:

| Campanha | Como subir | Recursos da API |
|---|---|---|
| **A — prod real** | `docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml up -d` | 0,5 vCPU / 384 MB |
| **B — teto da VM** | `docker compose -f docker-compose.yaml -f docker-compose.loadtest.B.yaml up -d` | 2 vCPU / 4 GB |

> **Por que existe o `loadtest.A.yaml` se ele "não muda os recursos"?** Porque ele muda **só o ambiente**
> para `LoadTest`. Sem ele, a Campanha A subiria em `Production` (com SMTP/tradução reais, pool default,
> sampling cheio) e aí A e B difeririam em **várias** coisas ao mesmo tempo — o experimento perderia o
> sentido. Com os dois overrides, **a única variável entre A e B são os recursos**. Isso é o que dá
> validade científica à comparação (importante para o TCC).

**Expectativa:** na Campanha A, o primeiro teto provavelmente é a **RAM/GC dos 384 MB** (um app .NET com
6 DbContexts + OTel é apertado nesse limite). A Campanha B mostra do que a arquitetura é capaz sem
essa amarra.

O **agente de carga roda de fora** (seu notebook ou outra máquina), via HTTPS contra o nginx — assim
evitamos *noisy neighbor* (o gerador roubando CPU do alvo) e medimos o custo real do TLS. Na variação de
keep-alive desligado (custo de handshake), em Linux amplie `net.ipv4.ip_local_port_range` e
`net.ipv4.tcp_tw_reuse` no agente.

---

## 7. Como rodar

```bash
# 1. Subir a API no modo LoadTest (Campanha A, por exemplo)
docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml up -d

# 2. Popular o banco (uma vez)
dotnet run -c Release --project tests/LoadTests -- --command=seed

# 3. Smoke de sanidade (rápido) antes de qualquer campanha
dotnet run -c Release --project tests/LoadTests -- --scenario=read-simple --profile=smoke --campaign=A

# 4. Carga nominal
dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=load --campaign=A

# 5. Capacidade institucional isolada
dotnet run -c Release --project tests/LoadTests -- --scenario=public-read --profile=institutional-100 --campaign=A

# 6. Carga conjunta institucional + administrativo
dotnet run -c Release --project tests/LoadTests -- --scenario=mixed-public-admin --profile=joint-100-20 --campaign=A

# 7. Stress administrativo interativo (modelo aberto, acha o ponto de ruptura)
dotnet run -c Release --project tests/LoadTests -- --scenario=read-complex --profile=stress --campaign=A

# 8. Soak de 60 min (memory leak / GC)
dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=soak --campaign=A

# 9. Resiliência do rate limiter (espera 429)
dotnet run -c Release --project tests/LoadTests -- --scenario=resilience --profile=smoke --campaign=A

# Limpar o banco entre rodadas de escrita
dotnet run -c Release --project tests/LoadTests -- --command=reset
# (ou use --resetBeforeRun=true para limpar+semear antes de rodar)

# 8. Ablation do LibreTranslate: subir com o overlay noop-translate por cima da campanha...
docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml -f docker-compose.loadtest.noop-translate.yaml up -d
# ...e repetir o mesmo cenário/perfil com --campaign diferente, para não sobrescrever o resultado real
dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=load --campaign=A-noop-translate
```

Repita tudo trocando `--campaign=B` (e subindo o `loadtest.B.yaml`) para a comparação A×B.

> **Convenção de nomes de campanha:** `--campaign` define a pasta do relatório
> (`reports/<campanha>/<perfil>/<cenário>/`). Rodar duas vezes com o mesmo valor **sobrescreve** o
> relatório anterior. Para a ablation do LibreTranslate (overlay `noop-translate`), use um valor
> diferente, ex. `--campaign=A-noop-translate`, para manter os dois resultados lado a lado.

---

## 8. Como ler os resultados

1. **Passou no SLO?** Veja se `p95 ≤ T` e `taxa de erro < 0,1%` em cada cenário (o NBomber marca a
   rodada como falha se algum threshold estourar).
2. **Está bom de verdade?** Olhe o **Apdex** por operação no `summary.json` (≥ 0,90 = nominal saudável).
3. **Quem quebrou primeiro?** (no stress) Cruze o **breakdown de status code** do cliente com a
   telemetria do New Relic:

   | Erro no cliente | Causa provável | Sinal no servidor (New Relic) |
   |---|---|---|
   | 500 / TimeoutException | esgotou o pool do Npgsql (20) | conexões ativas no teto + espera por conexão |
   | 502 / 504 | nginx (0,2 vCPU) estourou na cripto TLS | CPU do nginx em 100% |
   | timeout / latência subindo | *thread pool starvation* no Kestrel | fila do thread pool / Kestrel active requests subindo |
   | `SocketException` **no agente** | **não é a API** — é exaustão de portas do gerador | nada anormal no servidor (ver seção 4) |
   | restart / 502 em massa (Campanha A) | **OOM/GC a 384 MB** | memória do container no teto, GC em loop |

---

## 9. Onde executar (staging com um servidor só)

"Staging" **não** é um segundo servidor ligado 24/7 — é um ambiente que **parece** produção, levantado
**quando você vai testar** e derrubado depois. Como tudo é `docker compose`, staging é um
`docker compose up` numa VM. E como os **limites do compose** definem o comportamento, o mesmo compose
roda equivalente em qualquer host (os números *absolutos* de RPS variam com a velocidade do core do
host; o **formato** — onde satura, GC, pool — se reproduz).

- **Carga pesada** (load/stress/soak) → ambiente **isolado**: hoje a própria produção *antes do
  lançamento* (sem cliente); depois, uma VM de staging levantada por algumas horas.
- **Produção ao vivo (com cliente)** → **nunca** carga pesada. Quem cuida dela é a observabilidade
  (OTel → New Relic) + um *smoke* leve. Os testes pesados rodam no staging antes de cada release.

---

## 10. Constantes do sistema (e por quê)

| Constante | Valor | Motivo |
|---|---|---|
| Limite da API (prod) | 0,5 vCPU / 384 MB | É o que está no `docker-compose.yaml` de produção |
| `max_connections` do Postgres | 50 | Limite do container do banco |
| Pool do Npgsql no teste | 20 | Pequeno de propósito, para a saturação do pool ser visível |
| Rate limit `/scheduling` público | 5/hora **global** | Não é por IP/usuário — é um balde único para toda a API; por isso só o cenário de resiliência o testa |
| Sampling de traces no teste | 0,1 | Traces são caros sob carga; métricas ficam em 100% |
| Mix realista | 70/10/20 | Aproxima o tráfego real: muita leitura, pouca leitura-pesada, alguma escrita |

---

### Resumindo em uma frase

Esta suíte **bombardeia a API de fora** com cargas controladas, **mede latência/throughput/erros** com
SLOs claros, **isola o que pode atrapalhar** (deps externas mockadas, pool fixo) e **cruza** tudo com a
telemetria interna — para dizer, com números, **até onde a aplicação aguenta e o que cede primeiro**.
