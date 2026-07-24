# Runbook — Cutover de estado: droplet manual (atual) → droplet Terraform (nova)

[English](./droplet-cutover.md) · **Português**

**Audiência:** qualquer humano com acesso SSH às duas droplets e ao painel
DigitalOcean, mesmo sem contexto prévio do projeto. Execução sequencial,
de cima para baixo.

**Quando usar:** ao adotar a infraestrutura imutável
(`infra/terraform/`) pela primeira vez — ou seja, quando já existe uma
droplet de produção `labviromol-prod` criada manualmente (sem Terraform),
com Postgres, uploads e certificados TLS reais, e o objetivo é substituí-la
por uma droplet nova provisionada via `terraform apply`, **sem perder
dados**.

**Não é um flip de DNS.** O DNS já aponta para o **reserved IP** da
DigitalOcean (gerenciado fora do Terraform hoje, ou via `dns.tf` se
`manage_dns = true`). O cutover real é a **reatribuição do reserved IP**
entre droplets — o domínio nunca muda de valor. O trabalho pesado deste
runbook é mover banco, uploads e certificados, não DNS.

## 0. Vocabulário usado neste runbook

| Termo | Significado aqui |
|---|---|
| **droplet velha** | A droplet manual atual em produção, hoje respondendo em `lab.vitorespinoza.com`. |
| **droplet nova** | A droplet criada por `terraform apply` a partir de `infra/terraform/` (plano 17), recurso `digitalocean_droplet.app`. |
| **reserved IP** | `digitalocean_reserved_ip.app` (Terraform) — IP estável que o DNS aponta. Hoje, na droplet velha, presumivelmente já é um reserved IP gerenciado manualmente no painel; o `terraform apply` **adota o conceito**, mas cria um **reserved IP novo e distinto** (Terraform não importa o IP antigo automaticamente — ver §1.4). |
| **volume de dados** | `digitalocean_volume.postgres_data` (Terraform) — volume em bloco anexado à droplet nova, ponto de montagem `/mnt/labviromol_data/postgres` (preparado pelo `cloud-init.yaml`, ver §0.1 abaixo sobre desvio). |

### 0.1 Desvio conhecido entre Terraform e `docker-compose.yaml` — ler antes de continuar

O `cloud-init.yaml` da droplet nova cria e prepara o diretório
`/mnt/labviromol_data/postgres` no volume em bloco anexado
(`digitalocean_volume.postgres_data`), com a intenção de que o Postgres
grave ali (dados persistentes sobrevivendo a rebuild da droplet).

**Porém**, o `docker-compose.yaml` deste repo (raiz) declara o volume do
serviço `postgres` como um **volume Docker nomeado** (`postgres_data:`,
gerenciado pelo Docker em `/var/lib/docker/volumes/...`), **não** como um
bind mount para `/mnt/labviromol_data/postgres`:

```yaml
services:
  postgres:
    volumes:
      - postgres_data:/var/lib/postgresql/data
volumes:
  postgres_data:
```

Ou seja, **hoje o volume em bloco do Terraform não está de fato conectado
ao Postgres do compose** — o Postgres grava no disco raiz da droplet
(dentro do volume Docker padrão), não no volume em bloco anexado. Isso
significa que, se a droplet for destruída sem cuidado, os dados do
Postgres **não sobrevivem automaticamente** via o volume em bloco, ao
contrário do que a infra imutável pretende.

**Implicação prática para este runbook:** a restauração do banco (passo 3)
é feita via `pg_dump`/`pg_restore` para dentro do volume Docker nomeado da
droplet nova — funciona independente desse desvio, porque não dependemos
do bind mount para mover os dados. Mas **atenção**: para
que o volume em bloco cumpra seu propósito (dados sobrevivendo a rebuilds
futuros sem precisar repetir este runbook todo), o `docker-compose.yaml`
de produção precisa ser ajustado para montar
`/mnt/labviromol_data/postgres:/var/lib/postgresql/data` como bind mount,
em vez do volume nomeado `postgres_data`. Essa correção ainda não foi
aplicada.

---

## 1. Pré-checks (antes de qualquer coisa)

### 1.1 Snapshot da droplet velha (rede de segurança)

No painel DigitalOcean (ou via `doctl`), criar um snapshot da droplet
velha **antes** de iniciar qualquer passo de migração:

```bash
doctl compute droplet-action snapshot <ID_DROPLET_VELHA> \
  --snapshot-name "labviromol-prod-pre-cutover-$(date +%Y%m%d)"
```

Aguardar o snapshot completar (painel DO → Images → Snapshots) antes de
prosseguir. Este snapshot é o rollback de última instância: se algo der
errado de forma irrecuperável, ele permite recriar a droplet velha do
zero a partir do estado anterior ao cutover.

### 1.2 Confirmar a droplet nova provisionada e saudável

A droplet nova deve já existir, provisionada pelo plano 17/18
(`terraform apply` — via `infra.yml`, job `apply`, ou manualmente):

```bash
terraform output reserved_ip      # novo reserved IP (ainda não é o de produção)
terraform output droplet_public_ip
terraform output postgres_volume_id
```

Subir a stack de aplicação na droplet nova (banco vazio, ainda não é
produção) e confirmar que ela responde:

```bash
ssh deploy@<droplet_public_ip_nova> "curl -fsS http://localhost:8080/health/ready"
# esperado: HTTP 200
```

Confirmar também que as migrations rodaram (banco vazio, mas com schema
criado) e que o `docker login ghcr.io` do cloud-init funcionou (consegue
`docker compose pull`).

### 1.3 Anunciar a janela de manutenção

Comunicar aos usuários (e-mail, banner no front, ou aviso no
institucional) a janela de downtime planejada. Duração esperada: minutos
(dump/restore de um banco de laboratório, não terabytes), mas tratar como
indisponibilidade total da aplicação durante a execução deste runbook.

Opcional: colocar a droplet velha em modo leitura/manutenção (ex.: scale
down do serviço `api` no compose, deixando só um placeholder estático) para
garantir que nenhuma escrita nova aconteça no banco enquanto o dump está
sendo tirado — evita janela de inconsistência entre o dump e o cutover de
tráfego.

### 1.4 Reserved IP — atenção à distinção entre o IP atual e o do Terraform

Se o reserved IP de produção atual (apontado pelo DNS, hoje) **não** foi
criado originalmente pelo Terraform, o `digitalocean_reserved_ip.app` do
plano 17 é um **recurso novo, com um endereço IP diferente** do reserved
IP de produção atual. Confirmar isso antes do passo 6:

```bash
# IP de produção atual (o que o DNS resolve hoje)
dig +short lab.vitorespinoza.com

# IP reservado criado pelo Terraform (novo)
terraform output reserved_ip
```

Se os dois forem diferentes, o cutover de tráfego (§6) precisa
necessariamente trocar o A record de DNS para o novo reserved IP (não
basta reatribuir — o IP em si mudou). Se o usuário preferir manter o
mesmo IP histórico, a alternativa é importar o reserved IP existente para
o state do Terraform (`terraform import digitalocean_reserved_ip.app
<id-do-ip-existente>`) antes deste runbook — decisão de infraestrutura
fora do escopo deste documento, mas crítica de confirmar aqui para não
assumir "o reserved IP não muda" quando na prática muda.

---

## 2. Congelar escrita na droplet velha

Antes do dump, garantir que nenhuma escrita nova ocorra durante a janela:

```bash
# Na droplet velha
ssh deploy@<droplet_velha_ip>
cd ~/app   # ou onde estiver o docker-compose.yaml de produção
docker compose stop api migrate admin institucional
# Postgres continua de pé (precisamos dele de pé para o pg_dump),
# só a aplicação para de aceitar tráfego/escritas.
```

---

## 3. Migração do Postgres

### 3.1 Dump na droplet velha (formato custom)

```bash
# Ainda na droplet velha
docker compose exec -T postgres pg_dump \
  -U "${POSTGRES_USER:-labviromol}" \
  -d "${POSTGRES_DB:-LabViroMol}" \
  -Fc -f /tmp/labviromol-cutover.dump

docker compose cp postgres:/tmp/labviromol-cutover.dump /tmp/labviromol-cutover.dump
```

Confirmar o tamanho do arquivo (`ls -lh /tmp/labviromol-cutover.dump`) —
um dump vazio (poucos KB) indica falha silenciosa de autenticação/conexão,
não prosseguir nesse caso.

### 3.2 Transferir o dump para a droplet nova

```bash
scp /tmp/labviromol-cutover.dump deploy@<droplet_nova_ip>:/tmp/labviromol-cutover.dump
```

### 3.3 Restaurar na droplet nova

O banco da droplet nova já existe (criado vazio pelas migrations no
passo 1.2). `pg_restore` para dentro dele:

```bash
# Na droplet nova
ssh deploy@<droplet_nova_ip>
cd ~/app

# Parar a API para garantir que nenhuma conexão concorrente interfira no restore
docker compose stop api migrate admin institucional

# Copiar o dump para dentro do container do Postgres
docker compose cp /tmp/labviromol-cutover.dump postgres:/tmp/labviromol-cutover.dump

# Restaurar. --clean remove os objetos do schema vazio (criados pelas
# migrations) antes de recriá-los a partir do dump — necessário porque
# o banco de destino não está realmente vazio (tem schema, sem dados).
docker compose exec -T postgres pg_restore \
  -U "${POSTGRES_USER:-labviromol}" \
  -d "${POSTGRES_DB:-LabViroMol}" \
  --clean --if-exists --no-owner --no-privileges \
  /tmp/labviromol-cutover.dump
```

`--no-owner --no-privileges`: evita falhas de `pg_restore` por diferença
de nome de role/owner entre a droplet velha e a nova (o usuário do
Postgres pode ter um nome igual por convenção, mas não depender disso).

### 3.4 Validar contagens de linhas por schema

Antes de seguir, confirmar que a contagem de linhas das tabelas
principais de cada schema do dump (velha) bate com a da droplet nova
restaurada. Os 6 schemas dos módulos (ver `assets/identity/inventory/
notify/research/scheduling`):

```bash
# Rodar o mesmo script nas duas droplets (velha, antes de qualquer escrita
# nova; nova, depois do restore) e comparar a saída linha a linha.
docker compose exec -T postgres psql -U "${POSTGRES_USER:-labviromol}" -d "${POSTGRES_DB:-LabViroMol}" -c "
SELECT schemaname, relname, n_live_tup
FROM pg_stat_user_tables
WHERE schemaname IN ('assets','identity','inventory','notify','research','scheduling')
ORDER BY schemaname, relname;
"
```

`n_live_tup` é uma estimativa do planner (rápida, não bloqueia) —
suficiente para detectar divergências grosseiras (tabela vazia quando não
deveria estar, ordens de grandeza diferentes). Se algo parecer suspeito
numa tabela específica, confirmar com `SELECT count(*)` exato só naquela
tabela antes de prosseguir.

**Critério de aceite deste passo:** toda tabela com `n_live_tup > 0` na
droplet velha tem contagem igual (ou compatível, considerando que pode
ter havido alguma escrita residual antes do `docker compose stop` do
passo 2) na droplet nova. Qualquer schema inteiro zerado indica falha no
restore — não prosseguir para o cutover de tráfego nesse caso.

---

## 4. Migração dos uploads

Os uploads de imagens da API (`Storage__RootFolder=/app/Upload/Images`,
volume Docker nomeado `uploads_images` no `docker-compose.yaml`) precisam
ser copiados via `rsync`, container a container (ambos os hosts já têm
`rsync` instalado pelo `cloud-init.yaml`, pacote `rsync`):

```bash
# Na droplet velha: descobrir o caminho real do volume Docker no host
ssh deploy@<droplet_velha_ip> \
  "docker volume inspect app_uploads_images --format '{{ .Mountpoint }}'"
# (o prefixo do nome do volume Docker segue o nome do diretório do compose,
#  ex. "app_uploads_images" se o compose roda em ~/app — confirmar com
#  `docker volume ls` se o nome não bater)

# rsync direto entre os dois hosts via SSH (executar de uma máquina com
# acesso às duas droplets, ou de uma delas para a outra):
rsync -avz --progress \
  -e ssh \
  deploy@<droplet_velha_ip>:/var/lib/docker/volumes/app_uploads_images/_data/ \
  deploy@<droplet_nova_ip>:/var/lib/docker/volumes/app_uploads_images/_data/
```

Se os nomes dos volumes Docker nomeados divergirem entre as duas droplets
(ex.: diretório do compose tem nome de pasta diferente em cada uma — não
é o caso esperado se ambas usam `~/app`, mas confirmar com `docker volume
ls` antes de assumir), ajustar o destino do `rsync` conforme o
mountpoint real reportado pela droplet nova.

**Validação:** comparar contagem de arquivos e tamanho total nas duas
pontas:

```bash
ssh deploy@<droplet_velha_ip> "find /var/lib/docker/volumes/app_uploads_images/_data -type f | wc -l"
ssh deploy@<droplet_nova_ip>  "find /var/lib/docker/volumes/app_uploads_images/_data -type f | wc -l"
```

Os dois números devem ser iguais.

---

## 5. Certificados TLS

Duas opções — escolher uma:

### Opção A — copiar os certificados existentes (mais rápido, sem rate limit)

```bash
# certbot_certs é volume Docker nomeado, montado em /etc/letsencrypt
# no serviço gateway/certbot do docker-compose.yaml
ssh deploy@<droplet_velha_ip> \
  "docker run --rm -v app_certbot_certs:/certs -v /tmp:/backup alpine \
   tar czf /backup/letsencrypt-backup.tar.gz -C /certs ."

scp deploy@<droplet_velha_ip>:/tmp/letsencrypt-backup.tar.gz /tmp/
scp /tmp/letsencrypt-backup.tar.gz deploy@<droplet_nova_ip>:/tmp/

ssh deploy@<droplet_nova_ip> \
  "docker run --rm -v app_certbot_certs:/certs -v /tmp:/backup alpine \
   tar xzf /backup/letsencrypt-backup.tar.gz -C /certs"
```

### Opção B — re-emitir via certbot (Cloudflare DNS-01) na droplet nova

Pré-requisito: `~/app/certbot/cloudflare.ini` precisa estar presente na
droplet nova **com permissão `chmod 600`** antes de subir o serviço
`certbot` do `docker-compose.yaml` (o serviço monta esse arquivo
read-only e o certbot recusa credenciais com permissão mais aberta que
`600`):

```bash
# Copiar o cloudflare.ini da droplet velha (ou de onde estiver guardado
# fora do repo — nunca commitado) para a droplet nova
scp deploy@<droplet_velha_ip>:~/app/certbot/cloudflare.ini /tmp/cloudflare.ini
scp /tmp/cloudflare.ini deploy@<droplet_nova_ip>:~/app/certbot/cloudflare.ini
ssh deploy@<droplet_nova_ip> "chmod 600 ~/app/certbot/cloudflare.ini"

# Emitir o certificado (executar uma vez, manualmente, antes de subir o
# loop de renovação automática do serviço certbot)
ssh deploy@<droplet_nova_ip> "cd ~/app && docker compose run --rm certbot \
  certbot certonly --dns-cloudflare \
  --dns-cloudflare-credentials /etc/cloudflare.ini \
  -d lab.vitorespinoza.com --non-interactive --agree-tos \
  -m <email-do-administrador>"
```

A Opção A evita o rate limit do Let's Encrypt (5 certificados/semana por
domínio exato) e é preferível se os certificados da droplet velha ainda
têm validade razoável. A Opção B é mais simples de raciocinar (estado
limpo) mas só deve ser usada se a Opção A falhar ou se os certificados
antigos estiverem perto do vencimento de qualquer forma.

**Validação (qualquer opção):**

```bash
ssh deploy@<droplet_nova_ip> "docker compose exec gateway nginx -t"
ssh deploy@<droplet_nova_ip> "ls -la /var/lib/docker/volumes/app_certbot_certs/_data/live/lab.vitorespinoza.com/"
# esperado: fullchain.pem, privkey.pem, cert.pem, chain.pem presentes
```

---

## 6. Cutover de tráfego

### 6.1 Subir a stack completa na droplet nova

```bash
ssh deploy@<droplet_nova_ip>
cd ~/app
docker compose up -d
docker compose ps   # confirmar todos os serviços "healthy"/"running"
curl -fsS http://localhost:8080/health/ready
```

### 6.2 Reatribuir o reserved IP

Via Terraform (preferível — mantém o state como fonte de verdade):

```bash
# infra/terraform/main.tf já declara:
#   resource "digitalocean_reserved_ip_assignment" "app" {
#     ip_address = digitalocean_reserved_ip.app.ip_address
#     droplet_id = digitalocean_droplet.app.id
#   }
# Se a droplet nova é a digitalocean_droplet.app do state atual, o
# reserved_ip_assignment já está apontando para ela desde o `apply`
# inicial (passo 1.2) — confirmar:
terraform state show digitalocean_reserved_ip_assignment.app
```

Se o reserved IP de produção real (DNS) é o mesmo do Terraform (ver
§1.4), a reatribuição já está em vigor desde o provisionamento da droplet
nova — não há ação adicional aqui, só **confirmar** via painel
DigitalOcean (Networking → Reserved IPs) que o IP aponta para a droplet
nova, não para a velha.

Se o reserved IP de produção real é **diferente** do criado pelo
Terraform (caso comum na primeira adoção, ver §1.4): atualizar o A record
de DNS (painel Cloudflare, ou `terraform apply` com `manage_dns = true`
apontando para `terraform output reserved_ip`) para o novo IP. Esse
caminho **não é instantâneo** — está sujeito ao TTL do DNS (300s, ver
`dns.tf`) e a caches de resolver intermediários.

### 6.3 Validar o cutover

```bash
curl -fsS https://lab.vitorespinoza.com/health/ready
# esperado: 200, respondendo a partir da droplet nova

# Confirmar pela IP de origem reportada / ou comparar um identificador
# único (ex. logar timestamp de boot ou hostname num endpoint de debug,
# se existir) para ter certeza de que o tráfego está de fato saindo pela
# droplet nova, não servindo cache de CDN/proxy intermediário.
dig +short lab.vitorashospital.com  # confirmar resolução para o IP esperado
```

Validar manualmente pelo menos um fluxo crítico de cada módulo
(login, listagem de materiais, criação de agendamento, etc.) através da
URL pública, não só via `/health/ready`.

---

## 7. Pós-cutover

1. **Monitorar** (New Relic — `OTEL_EXPORTER_OTLP_*` já configurado no
   compose) por um período de carência (recomendado: pelo menos 24-48h)
   antes de qualquer ação destrutiva. Observar taxa de erro, latência e
   uso de recursos (a droplet nova tem o mesmo `s-2vcpu-4gb` da antiga,
   então não deveria haver regressão de capacidade).
2. **Confirmar fluxos críticos** com usuários reais/QA manual, não só
   smoke tests automatizados.
3. **Só então destruir a droplet velha.** Duas formas:
   - Se a droplet velha nunca foi gerenciada pelo Terraform (caso
     esperado nesta primeira adoção): destruição manual via painel
     DigitalOcean ou `doctl compute droplet delete <ID_DROPLET_VELHA>`.
   - Se em algum momento ela foi importada para o state do Terraform:
     `terraform destroy -target=<recurso_da_droplet_velha>`.

   Antes de destruir, confirmar que o **snapshot do passo 1.1** ainda
   existe e está íntegro — ele é o único artefato de recuperação depois
   que a droplet velha for removida.

---

## 8. Rollback do cutover

Se um problema crítico for descoberto **depois** do passo 6 mas **antes**
da destruição da droplet velha (período de carência do passo 7): a
droplet velha ficou intacta (parada nos serviços de app desde o passo 2,
mas não destruída), então o rollback é simplesmente desfazer o passo 6.2:

```bash
# Religar os serviços de app na droplet velha (estavam stopped desde o passo 2)
ssh deploy@<droplet_velha_ip> "cd ~/app && docker compose start api migrate admin institucional"

# Reatribuir o reserved IP de volta à droplet velha
```

Via Terraform, isso significa reverter temporariamente a
`reserved_ip_assignment.app.droplet_id` para apontar de volta ao ID da
droplet velha — só é direto se a droplet velha também estiver representada
no state do Terraform (não é o caso padrão nesta primeira adoção, ver
§1.4). No caso comum (droplet velha fora do Terraform), o rollback é
manual via painel DigitalOcean (Networking → Reserved IPs → reatribuir)
ou, se o IP de DNS mudou no passo 6.2, revertendo o A record de volta ao
IP antigo.

**Atenção a dados escritos depois do cutover:** se o rollback ocorrer
depois que a droplet nova já recebeu escritas reais (não só leitura),
essas escritas **não existem na droplet velha** — o rollback de
infraestrutura (IP) não é um rollback de dados. Se isso acontecer, será
necessário repetir o passo 3 (dump da droplet nova → restore na velha)
antes de reatribuir o IP, ou aceitar a perda das escritas feitas no
intervalo (decisão de negócio, não técnica).

---

## Recomendação de médio prazo

Migrar para **DigitalOcean Managed Postgres** + **Spaces** (para uploads)
elimina os passos 3 e 4 em futuros rebuilds — a droplet se torna
verdadeiramente stateless (só roda os containers de aplicação, sem dados
próprios), e um rebuild futuro deixa de exigir este runbook inteiro,
reduzindo-se a "apontar a nova droplet para o mesmo banco/bucket
gerenciado". Os certificados TLS (passo 5) ainda precisariam de
tratamento, mas seriam o único estado restante.

## Antes de um cutover real

Ensaiar este runbook num ambiente de teste seco (droplet descartável,
banco de teste com dados sintéticos) antes do cutover de produção real.
Cronometrar a duração real da janela de
manutenção nesse ensaio para informar o anúncio de downtime do passo 1.3.
