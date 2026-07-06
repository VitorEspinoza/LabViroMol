# Runbook — passo a passo da campanha de testes de carga

[English](./RUNBOOK.md) · **Português**

Este arquivo é o **checklist de execução**, na ordem em que você deve seguir. Ele não explica o "por quê"
de cada peça (isso está no [README.md](README.pt-BR.md)) nem o detalhe de como montar tabela/escrever o
capítulo (isso está no [REPORTING.md](REPORTING.pt-BR.md)) — aqui é só "faça isso, depois isso, depois isso",
pra você não perder passo nem esquecer de salvar evidência no meio do caminho.

Marque cada item conforme for executando. Sempre que um passo gerar relatório, o passo seguinte é **copiar
a evidência antes de rodar o próximo comando** — não deixe acumular pra copiar tudo no final.

## Topologia: onde cada comando roda

Isto **não é um teste local**. Hoje você só tem um servidor (o de produção, ainda sem cliente exposto) —
ele faz o papel de staging. Os comandos abaixo se dividem em dois lugares diferentes:

| Comando                                        | Onde roda                                                | Por quê                                                                                                                                                                      |
| ---------------------------------------------- | -------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `docker compose ... up -d`                     | **No servidor**, via SSH                                 | É lá que a API/Postgres/LibreTranslate/nginx precisam estar — você está testando a infraestrutura real                                                                       |
| `--command=seed` / `--command=reset`           | **No seu notebook**, via túnel SSH pro Postgres do servidor | Falam **direto com o Postgres** (não passam pela API/nginx); o servidor não tem o SDK do .NET pra rodar `dotnet run`, e o catálogo de seed precisa ficar no mesmo lugar de onde o `--scenario=...` é executado (ver seção "Túnel SSH" abaixo) |
| `--scenario=write` / `--scenario=mixed` / `mixed-public-admin` | **No seu notebook**, **com o túnel SSH e a connection string também** | Cenários com escrita administrativa podem esgotar o pool de agendamentos pendentes pré-semeados e reabastecem direto no Postgres em tempo real (`Seeder.AppendPendingSchedulesAsync`). Sem a connection string, isso lança exceção e infla a taxa de erro do teste |
| `--scenario=read-simple` / `read-complex` / `public-read` / `resilience` | **No seu notebook**, só com `--baseUrl` (sem túnel) | São 100% HTTP, nunca tocam o Postgres diretamente |
| `--scenario=... --profile=...` (a carga em si) | **No seu notebook** (ou outra máquina, fora do servidor) | É o "agente de carga externo" do README seção 6 — gerar carga _de fora_ evita _noisy neighbor_ (o gerador roubar CPU do alvo) e mede o custo real do TLS/rede até o servidor |

Pra rodar do notebook contra o servidor, toda execução de `--scenario=...` precisa do `--baseUrl` apontando
pra fora. **Use sempre `lab.vitorespinoza.com`, nunca o domínio raiz `vitorespinoza.com`** — o certificado
TLS do servidor só é válido para o subdomínio `lab.` (ver seção 8 do README, achado do `ERR_CERT_COMMON_NAME_INVALID`):

```bash
--baseUrl=https://lab.vitorespinoza.com
```

Todos os comandos deste runbook **já vêm com esse `--baseUrl` incluído** — não precisa adicionar
manualmente. Se um dia mudar de servidor/domínio, é só trocar esse valor em todos os blocos.

Pasta de evidências sugerida (crie antes de começar, no seu notebook — é aqui que as evidências ficam):

```bash
mkdir -p evidencias-tcc/capitulo-testes/{00-smoke,01-load-nominal,02-stress,03-soak,04-resiliencia,05-ablation-libretranslate,06-campanha-b}
```

---

## SSH passo a passo (do zero)

Se você nunca usou SSH manualmente, é isto: um jeito de abrir um "terminal dentro do servidor", como se
você tivesse digitando direto nele, mesmo estando no seu notebook.

### Abrindo uma sessão (pra rodar vários comandos em sequência)

1. Abra um terminal no seu notebook (Git Bash, PowerShell, ou o terminal do VS Code).
2. Digite:

```bash
ssh root@142.93.14.97
```

3. **Na primeira vez**, vai aparecer algo como:

```
The authenticity of host '142.93.14.97' can't be established.
...
Are you sure you want to continue connecting (yes/no)?
```

Digite `yes` e Enter. Isso só aparece uma vez (depois disso o servidor fica "conhecido" no seu
`~/.ssh/known_hosts`).

4. Se a chave SSH já está configurada (era o caso aqui — não pediu senha), você cai direto no prompt do
   servidor. Ele muda de `usuario@seu-notebook` para algo como `root@TCC-VITOR:~#` — **esse é o sinal de
   que você está "dentro" do servidor agora**, não mais no seu notebook.
5. A partir daqui, todo comando que você digitar roda **no servidor**. Por exemplo:

```bash
cd ~/labviromol-deploy
docker compose ps
```

6. Pra **sair** e voltar pro seu notebook: digite `exit` e Enter (ou `Ctrl+D`). O prompt volta a mostrar o
   nome do seu notebook — confirma que você saiu.

### Rodando um comando único sem "entrar" (jeito usado na maioria deste runbook)

Em vez de entrar na sessão e digitar comando por comando, dá pra mandar o comando direto, entre aspas,
junto com o próprio `ssh`. O servidor executa, mostra o resultado, e você já volta pro seu notebook
automaticamente — não precisa digitar `exit`:

```bash
ssh root@142.93.14.97 "docker compose -f labviromol-deploy/docker-compose.yaml ps"
```

Isso é o que significa todo bloco deste runbook que já vem com `ssh root@142.93.14.97 "..."` na frente —
você **não** precisa abrir uma sessão antes; é só colar o bloco inteiro (com `ssh` e tudo) no seu terminal
local e dar Enter. Já volta pra você sozinho.

Pra rodar **mais de um comando** nesse formato de uma linha só, separe com `&&` (só roda o próximo se o
anterior não der erro) ou `;` (roda o próximo de qualquer jeito):

```bash
ssh root@142.93.14.97 "cd ~/labviromol-deploy && docker compose ps && docker logs labviromol-api --tail 20"
```

### Copiando arquivo do/pro servidor (`scp`) — mesma lógica do `ssh`

```bash
# do seu notebook PRO servidor:
scp docker-compose.yaml root@142.93.14.97:~/labviromol-deploy/

# do servidor PRO seu notebook:
scp root@142.93.14.97:~/labviromol-deploy/.env .env
```

> **Regra fixa deste runbook:** todo `docker compose` no servidor assume que você está dentro de
> `~/labviromol-deploy` — é lá que o `docker-compose.yaml` e os overlays (`loadtest.A.yaml`, etc.) vivem.
> Se você abrir uma sessão SSH manual (não usar o formato `ssh host "comando"` de uma linha), **o primeiro
> comando sempre é `cd ~/labviromol-deploy`** antes de qualquer `docker compose`. Rodar de fora dessa pasta
> dá `open /root/docker-compose.yaml: no such file or directory`.

### Túnel SSH (pra alcançar o Postgres do servidor sem instalar nada lá)

O servidor **não tem o SDK do .NET instalado** — só Docker. Isso significa que `dotnet run` não funciona lá.
Por isso `--command=seed` e `--command=reset` (que falam direto com o Postgres, sem passar pela API) também
rodam **do seu notebook**, não do servidor — e o Postgres só escuta em `127.0.0.1:5432` *dentro* do
servidor (não exposto pra internet), então você abre um túnel SSH que "finge" que o Postgres do servidor
está em `localhost` no seu notebook:

```bash
ssh -fN -L 15432:localhost:5432 root@142.93.14.97
```

- `-L 15432:localhost:5432`: porta `15432` no seu notebook ↔ `localhost:5432` visto **de dentro do
  servidor** (que é onde o Postgres realmente escuta).
- `-f -N`: roda em background, sem abrir um terminal interativo (só faz o túnel e volta o prompt pra você).
- Pra fechar o túnel depois: ache o processo (`ps aux | grep 15432`) e mate com `kill <PID>`, ou simplesmente
  feche o terminal/reinicie o notebook.

Com o túnel aberto, toda vez que for rodar `--command=seed`/`--command=reset`, **ou `--scenario=write`/
`--scenario=mixed`** (esses dois reabastecem agendamentos pendentes direto no Postgres quando o pool
pré-semeado esgota sob carga — ver tabela de topologia acima), prefixe o comando com a connection string
apontando pro túnel, usando a senha real do `POSTGRES_PASSWORD` do seu `.env` local (puxado do servidor
antes):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --command=seed
```

> Isso também resolve um problema que passaria batido: o `seed` grava um arquivo de catálogo
> (`.artifacts/seed-catalog.json`, com os IDs que os cenários de carga usam) na pasta de build **de onde o
> comando rodou**. Se o `seed` rodasse no servidor e o `--scenario=...` rodasse no notebook, esse arquivo
> nunca seria encontrado pelo segundo — rodando os dois do mesmo lugar (seu notebook), o problema nem existe.

> **Esqueceu a connection string num `--scenario=write`/`mixed`?** O erro aparece só quando o pool de
> agendamentos pendentes esgota (não no início) — vai aparecer como `InvalidOperationException:
> ConnectionStrings:LabViroMol não configurada` no log/console no meio da execução, e a taxa de erro do
> cenário `approve_schedule_write` sobe artificialmente. Se isso acontecer, mate a execução, abra o túnel,
> e rode de novo com o prefixo de connection string.

---

## 0. Pré-requisitos (uma vez)

- [ ] `dotnet build tests/LoadTests/LabViroMol.LoadTests.csproj -c Release` compila sem erro.
- [ ] Suíte de unit/integration tests está verde (`dotnet test`) — não comece carga com bug funcional conhecido.
- [ ] Fez o smoke funcional manual (passo a passo detalhado abaixo).
- [ ] Anotou a especificação da máquina de carga (CPU/RAM/rede) **e** do servidor — vai pro capítulo de
      metodologia.
- [ ] Confirmou (do notebook) que o servidor responde via HTTPS: `curl -I https://lab.vitorespinoza.com`
      (deve responder algo como `HTTP/2 200`) — sem isso, nenhum `--scenario=...` vai funcionar do notebook.
- [ ] **Enviou os overlays de load test pro servidor** (só precisa fazer uma vez, ou de novo se editar esses
      arquivos localmente — eles não sobem automaticamente, diferente do `docker-compose.yaml` principal):

```bash
scp docker-compose.loadtest.A.yaml docker-compose.loadtest.B.yaml docker-compose.loadtest.noop-translate.yaml \
    root@142.93.14.97:~/labviromol-deploy/
```

- [ ] No servidor (via SSH): `cd ~/labviromol-deploy && docker compose -f docker-compose.yaml -f
      docker-compose.loadtest.A.yaml config` não dá erro (valida que os dois arquivos combinam certo antes
      de subir).

### Smoke funcional manual — passo a passo

O objetivo aqui é clicar nos mesmos fluxos que os cenários do NBomber vão bater (seção 3 do README), **com
olho humano**, pra pegar qualquer bug óbvio antes de jogar carga em cima dele. Faça pelo navegador, no
Admin Panel:

1. Abra `https://lab.vitorespinoza.com/gestao-lab-ufpr/` no navegador.
2. Faça login com seu usuário admin real (não precisa ser usuário de teste — é só pra confirmar que o
   fluxo funciona).
   - [ ] Login completou sem erro, te levou pro dashboard.
3. Vá em **Agendamentos** → ache um agendamento pendente → aprove.
   - [ ] Aprovação completou, o status mudou pra "aprovado" na lista.
4. Vá em **Projetos** → abra um projeto existente → adicione um membro (qualquer pesquisador da lista) com
   papel "Colaborador".
   - [ ] Membro apareceu na lista do projeto sem erro.
5. Vá em **Materiais** → crie um material novo (nome, localização, estoque mínimo, quantidade, unidade,
   tipo — qualquer valor válido).
   - [ ] Material apareceu na listagem depois de criado.
6. Se algum desses passos der erro: **pare aqui**, não comece a carga. Abra o DevTools do navegador (F12 →
   aba Network) pra ver qual request falhou, ou olhe os logs da API (`ssh root@142.93.14.97 "docker logs
labviromol-api --tail 50"`) pra achar o stacktrace.

**Alternativa rápida por terminal** (só confirma que login funciona, sem abrir navegador):

```bash
curl -i -X POST https://lab.vitorespinoza.com/api/identity/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"SEU_EMAIL_ADMIN","password":"SUA_SENHA"}'
```

- [ ] Resposta `200` com um `Set-Cookie: X-Access-Token=...` no cabeçalho = login funcionando. Qualquer
      `4xx`/`5xx` aqui já é motivo pra investigar antes de seguir.

---

## 1. Campanha A — tradução real (configuração padrão)

**No servidor (via SSH, dentro de `~/labviromol-deploy`):**

```bash
cd ~/labviromol-deploy
docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml up -d
```

- [ ] Subiu sem erro; `docker compose ps` mostra `api`, `postgres`, `libretranslate`, `gateway` saudáveis.

### Aplicar migrations (só se este for um banco novo/diferente do de produção)

Se você está reutilizando o mesmo Postgres de produção que já recebeu deploy (caso normal, já tratado),
**pule esta parte** — as migrations já foram aplicadas. Se for um banco realmente novo (ex.: você trocou
de servidor), o jeito mais simples é reaproveitar a imagem `migrate` (já existe um `Dockerfile.migrate` no
repo pra isso):

```bash
# 1. No seu notebook: builda e sobe a imagem de migration pro GHCR (uma vez)
docker build -f Dockerfile.migrate -t ghcr.io/vitorespinoza/labviromol-migrate:latest .
docker push ghcr.io/vitorespinoza/labviromol-migrate:latest

# 2. No servidor: puxa a imagem e roda pra cada um dos 6 DbContexts (o projeto tem 6 módulos,
#    cada um com seu proprio DbContext/schema — precisa rodar uma vez por contexto)
ssh root@142.93.14.97 'docker pull ghcr.io/vitorespinoza/labviromol-migrate:latest'

ssh root@142.93.14.97 'DB_CS=$(grep "^DB_CONNECTION_STRING=" ~/labviromol-deploy/.env | cut -d= -f2-); \
for ctx in LabViroMolIdentityDbContext InventoryDbContext AssetsDbContext ResearchDbContext SchedulingDbContext NotifyDbContext; do \
  echo "=== $ctx ==="; \
  docker run --rm --network labviromol-deploy_default -e ConnectionStrings__LabViroMol="$DB_CS" \
    ghcr.io/vitorespinoza/labviromol-migrate:latest --context $ctx; \
done'
```

- [ ] Cada um dos 6 contextos terminou com `Done.` no output (sem erro).
- [ ] Reiniciar a API pra garantir que ela não ficou com conexão antiga: `ssh root@142.93.14.97
    "docker compose -f labviromol-deploy/docker-compose.yaml restart api"`.

**No seu notebook**, com o túnel SSH aberto (ver seção "Túnel SSH" no topo deste arquivo):

```bash
ssh -fN -L 15432:localhost:5432 root@142.93.14.97

ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --command=seed
```

- [ ] Seed terminou sem exceção; `.artifacts/seed-catalog.json` foi criado **na pasta de build do seu
      notebook** (mesma pasta de onde o `--scenario=...` vai rodar depois).

**A partir daqui, todo `--scenario=...` roda no seu notebook** (agente externo), com `--baseUrl=https://lab.vitorespinoza.com` já incluído em cada comando abaixo.

### 1.1 Smoke (sanidade antes de qualquer carga real)

```bash
dotnet run -c Release --project tests/LoadTests -- --scenario=read-simple --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Sem 5xx; throughput > 0.
- [ ] **Copiar evidência agora:**

```bash
cp -r tests/LoadTests/bin/Release/net10.0/reports/A/smoke/read-simple \
      evidencias-tcc/capitulo-testes/00-smoke/$(date +%F)_A_smoke_read-simple
echo "dotnet run -c Release --project tests/LoadTests -- --scenario=read-simple --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com" \
      > evidencias-tcc/capitulo-testes/00-smoke/$(date +%F)_A_smoke_read-simple/comando.txt
```

- [ ] Repetir o smoke pro `read-complex` (dashboard administrativo, sem connection string — é só leitura).
- [ ] Rodar também o smoke do institucional:

```bash
dotnet run -c Release --project tests/LoadTests -- --scenario=public-read --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com
```
- [ ] **`write`**: precisa do túnel SSH aberto e do prefixo de connection string (ver seção "Túnel SSH"
      acima), porque sob carga ele esgota o pool de agendamentos pendentes e reabastece direto no Postgres:

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=write --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] **Antes de rodar `mixed`, dê um `--command=reset` + `--command=seed`** (seção 1.6 + 1, com o mesmo
      prefixo de connection string). Sem isso, `mixed` vai tentar aprovar agendamentos e adicionar membros
      que o `write` já consumiu, e vai aparecer `422`/`409` que **não são bugs reais**, só dado velho.
- [ ] **`mixed`**: mesmo prefixo de connection string que o `write` (ele também inclui escrita):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Copiar evidência de cada um dos 3 (mesmo padrão do `read-simple` acima).

### 1.2 Load nominal (~10 min)

**Com túnel SSH aberto e connection string** (`mixed` escreve e reabastece pool sob carga):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=load --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] `summary.json`: erro < 0,1%, p95 dentro do T por operação (ver tabela do README seção 3).
- [ ] Copiar evidência (mesmo padrão do 1.1, pasta `01-load-nominal`).
- [ ] No New Relic, recortar a janela de tempo do teste e printar `cqrs.duration`, pool do Npgsql, CPU/GC.
      Salvar print na mesma pasta de evidência.

### 1.2.1 Capacidade institucional isolada (~10 min)

Estes cenários medem quantos usuários virtuais simultâneos o institucional suporta sem carga administrativa relevante.
O `summary.json` grava `Load.ClosedCopies`, `Load.ApproxRps`, `Groups` e `Operations`.

```bash
dotnet run -c Release --project tests/LoadTests -- --scenario=public-read --profile=institutional-100 --campaign=A --baseUrl=https://lab.vitorespinoza.com

dotnet run -c Release --project tests/LoadTests -- --scenario=public-read --profile=institutional-200 --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Usar o maior perfil válido para declarar a capacidade institucional isolada.

### 1.2.2 Carga conjunta institucional + administrativo (~10 min)

**Com túnel SSH aberto e connection string**, porque o fluxo administrativo inclui escrita:

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed-public-admin --profile=joint-100-20 --campaign=A --baseUrl=https://lab.vitorespinoza.com

ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed-public-admin --profile=joint-150-30 --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Conferir no `summary.json` `Load.InstitutionalClosedCopies`, `Load.AdminClosedCopies`, RPS por grupo e operações que primeiro violaram p95/p99.
- [ ] Considerar `joint-100-20` como meta principal inicial; `joint-150-30` é degrau de folga/ruptura.

### 1.3 Stress (modelo aberto, acha o ponto de ruptura)

`read-complex` não precisa de connection string; `write` precisa (túnel SSH aberto):

```bash
dotnet run -c Release --project tests/LoadTests -- --scenario=read-complex --profile=stress --campaign=A --baseUrl=https://lab.vitorespinoza.com

ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=write --profile=stress --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Anotar o RPS em que o erro começa a subir e qual recurso saturou primeiro (status code → tabela do
      README seção 8: 500 = pool, 502/504 = nginx, timeout = thread pool, restart = OOM).
- [ ] Copiar evidência + print New Relic (pasta `02-stress`).

### 1.4 Soak — 60 min (memory leak / GC)

**Com túnel SSH aberto e connection string** (mesmo motivo do 1.2):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=soak --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Olhar o gráfico de latência do NBomber ao longo do tempo: latência **não** deve crescer monotonicamente.
- [ ] Olhar heap/GC no New Relic na mesma janela.
- [ ] Copiar evidência + print (pasta `03-soak`). Se houver drift de latência, anotar como **achado**, não
      esconder.

### 1.5 Resiliência (rate limiter)

```bash
dotnet run -c Release --project tests/LoadTests -- --scenario=resilience --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Confirma 429 a partir da 6ª tentativa na mesma hora (limite = 5/hora, global).
- [ ] Copiar evidência (pasta `04-resiliencia`).

### 1.6 Reset entre rodadas de escrita (se for repetir algum perfil acima)

**No seu notebook**, com o túnel SSH aberto (mesma lógica do seed):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --command=reset
```

---

## 2. Ablation do LibreTranslate (isolar o custo da tradução)

**No servidor (dentro de `~/labviromol-deploy`):**

```bash
cd ~/labviromol-deploy
docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml -f docker-compose.loadtest.noop-translate.yaml up -d
```

- [ ] Confirmar que subiu com o overlay (variável `LoadTest__UseNoOpTranslator=true` ativa).

**No notebook** (com túnel SSH aberto e connection string, mesmo motivo do 1.2):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=load --campaign=A-noop-translate --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Mesmo perfil/cenário do passo 1.2, só muda o `--campaign` (evita sobrescrever o resultado real).
- [ ] Copiar evidência (pasta `05-ablation-libretranslate`).
- [ ] Calcular o delta: `A` (tradução real) vs `A-noop-translate` (mock) — esse número é o custo de CPU/RAM
      do LibreTranslate no host. Guardar numa tabelinha junto da evidência.
- [ ] **No servidor**, voltar pro compose sem o overlay antes de continuar (`docker compose -f
    docker-compose.yaml -f docker-compose.loadtest.A.yaml up -d`, sem o `noop-translate`).

---

## 3. Campanha B — teto da VM (sem os limites de container)

**No servidor (dentro de `~/labviromol-deploy`):**

```bash
cd ~/labviromol-deploy
docker compose -f docker-compose.yaml -f docker-compose.loadtest.B.yaml up -d
```

- [ ] Repetir **todos** os passos 1.1 a 1.5 (lembrando: `docker compose` no servidor; `seed`/`reset` e
      `--scenario=...` no notebook, via túnel SSH e `--baseUrl=https://lab.vitorespinoza.com`) trocando
      `--campaign=A` por `--campaign=B`.
- [ ] (Opcional, mesmo raciocínio do passo 2) repetir a ablation do LibreTranslate também na Campanha B,
      com `--campaign=B-noop-translate`.
- [ ] Copiar cada evidência na pasta `06-campanha-b`.

---

## 4. Consolidação (depois que todas as campanhas rodaram)

- [ ] Montar a tabela comparativa A × B × ablation (ver REPORTING.md seção 4) a partir dos `summary.json`
      copiados.
- [ ] Para cada linha da tabela, confirmar que o print do New Relic correspondente está salvo na mesma
      pasta de evidência.
- [ ] Rodar o checklist final do REPORTING.md (seção 7) pra cada resultado antes de considerá-lo "fechado".

---

## 5. Escrever o capítulo

Use a estrutura do REPORTING.md seção 6 (Metodologia → Execução → Resultados → Análise → Limitações →
Conclusão), preenchendo cada subseção com o que foi coletado nos passos 1 a 4 acima. Não escreva nenhuma
conclusão antes de ter a evidência (pasta correspondente) salva — isso evita ficar sem como sustentar um
número na defesa.

- [ ] Metodologia escrita (SLOs definidos a priori, ambiente, variáveis de controle).
- [ ] Execução escrita (comandos, datas, desvios do plano).
- [ ] Resultados escritos (tabelas por perfil + tabela comparativa).
- [ ] Análise escrita (correlação cliente↔servidor, Apdex por faixa de carga).
- [ ] Limitações escritas.
- [ ] Conclusão escrita (RPS sustentável, gargalo identificado, recomendação).

---

## Anexo A — Queries do New Relic (NRQL)

Se você nunca usou New Relic: a interface de consulta é o **"Query your data"** no menu lateral (ícone de
lupa/banco de dados). Cole a query, ajuste o período no canto superior direito (ou use `SINCE`/`UNTIL` na
própria query, como abaixo) e rode. `TIMESERIES` faz aparecer como gráfico ao longo do tempo em vez de um
número só — use isso sempre que for olhar comportamento durante um teste, não só o resultado final.

Em todas as queries abaixo, troque a janela de tempo (`SINCE ... UNTIL ...`) pelo horário exato do teste
que você anotou no passo de evidência (seção 1 a 3 deste runbook) — sem isso você mistura tráfego do teste
com tráfego de outra hora.

### A.1 — A API está respondendo? (saúde geral)

```sql
FROM Log SELECT count(*) WHERE entity.name = 'labviromol-api' SINCE 30 minutes ago TIMESERIES
```

**Por quê:** se essa contagem cair a zero durante o teste, a API parou de logar — sintoma de crash/OOM
(combina com o cenário de Campanha A explodir em 384MB, ver README seção 8).

### A.2 — Erros durante o teste, agrupados por mensagem

```sql
FROM Log SELECT count(*) WHERE entity.name = 'labviromol-api' AND severity.text = 'Error'
FACET message SINCE 30 minutes ago LIMIT 20
```

**Por quê:** é a query que te mostrou o erro do SMTP — agrupa por tipo de mensagem de erro, então você vê
rapidamente **quais** erros aconteceram e **quantas vezes**, sem ler log linha a linha. Use depois de cada
perfil (smoke/load/stress/soak) pra conferir se apareceu algo inesperado.

### A.3 — Ver o stacktrace completo de um erro específico

```sql
FROM Log SELECT timestamp, message, exception.message, exception.stacktrace
WHERE entity.name = 'labviromol-api' AND severity.text = 'Error'
SINCE 30 minutes ago LIMIT 50
```

**Por quê:** a A.2 te diz "o quê" e "quanto"; essa te dá o "onde no código" (stacktrace completo), pra
decidir se o erro é esperado (ex.: 429 do rate limiter) ou um bug real achado pelo teste de carga.

### A.4 — Latência por command/query (correlaciona com o p95/p99 do NBomber)

```sql
FROM Metric SELECT average(cqrs.duration), percentile(cqrs.duration, 95, 99)
WHERE entity.name = 'labviromol-api' FACET request SINCE 30 minutes ago TIMESERIES
```

**Por quê:** essa é a métrica que sustenta a tabela do REPORTING.md seção 5 — `request` é o nome do
command/query do Mediator (ex.: `ApproveScheduleCommand`). Compare o p95/p99 daqui com o p95/p99 que o
NBomber reportou pro mesmo cenário: se forem parecidos, a latência é da aplicação; se o do cliente for bem
maior, o tempo extra é rede/nginx/fila, não o código em si.

### A.5 — Taxa de erro por command/query

```sql
FROM Metric SELECT sum(cqrs.requests) WHERE entity.name = 'labviromol-api'
FACET request, outcome SINCE 30 minutes ago TIMESERIES
```

**Por quê:** mostra **qual** operação está falhando (não só "deu erro" — qual delas), com `outcome` =
`success`/`failure`. Cruze com o status code do `summary.json` do NBomber pra confirmar se o cliente e o
servidor enxergam a mesma falha.

### A.6 — Backlog do Outbox (achado de saturação assíncrona)

```sql
FROM Metric SELECT latest(outbox.pending) WHERE entity.name = 'labviromol-api'
SINCE 30 minutes ago TIMESERIES
```

**Por quê:** é o gauge citado no README seção 5 — se esse número sobe e não desce durante o stress/soak,
o worker do Outbox não está acompanhando o volume de escrita gerado pelo teste (gargalo assíncrono, mesmo
que os requests HTTP em si continuem rápidos).

### A.7 — Quanto custa processar um lote do Outbox (e quanto falha)

```sql
FROM Metric SELECT average(outbox.batch.duration), sum(outbox.messages.processed), sum(outbox.messages.failed)
WHERE entity.name = 'labviromol-api' SINCE 30 minutes ago TIMESERIES
```

**Por quê:** é a query que teria mostrado o problema do SMTP **antes** mesmo de olhar o log de erro —
`outbox.messages.failed` subindo é o sintoma agregado; a A.2/A.3 dão o motivo exato.

### A.8 — Custo da tradução (ablation do LibreTranslate)

```sql
FROM Metric SELECT average(translation.duration), sum(translation.failures)
WHERE entity.name = 'labviromol-api' FACET job SINCE 30 minutes ago TIMESERIES
```

**Por quê:** é o número que sustenta a comparação `A` vs `A-noop-translate` do passo 2 — rode essa query
nas duas janelas de tempo (uma por campanha) e compare.

### A.9 — Saúde do pool de conexão do Postgres (Npgsql)

```sql
FROM Metric SELECT count(*) FACET metricName
WHERE entity.name = 'labviromol-api' AND metricName LIKE '%npgsql%' SINCE 30 minutes ago LIMIT 20
```

**Por quê:** essa é uma query de **descoberta** — o nome exato das métricas do Npgsql varia por versão da
biblioteca, então em vez de adivinhar, essa query lista o que realmente está chegando. Depois de achar o
nome certo (algo como `db.client.connections.usage`), troque por uma query normal tipo:

```sql
FROM Metric SELECT average(`db.client.connections.usage`)
WHERE entity.name = 'labviromol-api' FACET state SINCE 30 minutes ago TIMESERIES
```

**Por quê (essa segunda):** é a métrica que confirma o gargalo do pool de 20 conexões (README seção 5) —
se `state=used` chegar perto de 20 durante o stress, é o pool saturando, não falta de CPU.

### A.10 — CPU, GC e memória do runtime .NET

```sql
FROM Metric SELECT count(*) FACET metricName
WHERE entity.name = 'labviromol-api' AND metricName LIKE '%runtime.dotnet%' SINCE 30 minutes ago LIMIT 30
```

**Por quê:** mesmo raciocínio da A.9 — descobre os nomes exatos primeiro (variam por versão do
`OpenTelemetry.Instrumentation.Runtime`). Os que mais importam pro soak (seção 1.4) são os de
`gc.heap.size` (memória) e `gc.collections.count` (frequência de coleta) — se o heap crescer sem parar
durante 60 minutos, é o memory leak que o soak deveria pegar.

### A.11 — Email: latência e falhas (o problema do SMTP, ao vivo)

```sql
FROM Metric SELECT average(email.latency), sum(email.failures)
WHERE entity.name = 'labviromol-api' SINCE 30 minutes ago TIMESERIES
```

**Por quê:** se você corrigir o bloqueio de SMTP (ver diagnóstico do dia 21/06) e quiser confirmar que voltou
a funcionar, é essa query: `email.failures` deve ir a zero e `email.latency` deve cair de "timeout" (~muitos
segundos) pra latência normal de SMTP (algumas centenas de ms).

### A.12 — Traces individuais de uma requisição lenta

No menu lateral, **Distributed tracing** (não é NRQL, é tela própria) → filtre por `labviromol-api` e pela
janela de tempo do teste → ordene por duração. Clique num trace lento pra ver o breakdown span-a-span
(quanto foi query no Postgres, quanto foi chamada HTTP pro LibreTranslate, etc.). Use isso quando a A.4
mostrar uma operação lenta e você quiser saber **a parte interna** que está demorando.
