# Runbook — Deploy de produção (pull-based)

[English](./deploy.md) · **Português**

> Substitui completamente o antigo `deploy.sh` (aposentado no plano
> `22-prod-compose-and-config-sync`). Não existe mais build local +
> `docker save | ssh | docker load`. Todo deploy é **pull-based**: a droplet
> só puxa imagens já publicadas no GHCR.

## Visão geral do fluxo

1. **CI** (`.github/workflows/cd.yml`, plano 15) builda, escaneia
   (`container-scan`), assina (cosign keyless) e publica no GHCR as imagens
   `labviromol-api` e `labviromol-migrate`, com tags `latest` + `<sha>`.
2. **CD** (job `deploy` em `cd.yml`, plano 16, gate de aprovação manual no
   GitHub Environment `production`) conecta via SSH na droplet e:
   1. Roda `scripts/deploy/sync-config.sh` para atualizar
      `docker-compose.prod.yaml` + `nginx/gateway.conf` (+
      `certbot/cloudflare.ini`, se presente) na droplet — sem tocar no
      `.env` do servidor.
   2. Decripta o `.env` via SOPS (plano 23) a partir de
      `secrets/prod.enc.env`.
   3. Executa a migração (`docker compose -f docker-compose.prod.yaml run
      --rm migrate`) **antes** de tocar na API — gate de exit code.
   4. Se a migração for bem-sucedida, faz `docker compose -f
      docker-compose.prod.yaml pull api && ... up -d api` e valida
      `/health/ready`.
   5. Se a migração falhar, **aborta** — a API antiga continua servindo
      (política fix-forward, sem rollback automático de imagem).
3. **Fronts** (`admin`, `institucional`) são deployados pelos próprios
   repos (plano 11 de cada um), atualizando **apenas o seu serviço** contra
   este mesmo `docker-compose.prod.yaml` na droplet. O backend não builda
   nem dispara o deploy dos fronts — só é dono do manifesto de orquestração.

## Arquivos envolvidos

| Arquivo | Dono | Papel |
|---|---|---|
| `docker-compose.prod.yaml` | backend (este repo) | manifesto pull-based de produção — único compose que roda na droplet |
| `docker-compose.yaml` + `docker-compose.override.yml` | backend (este repo) | **dev local apenas** — têm `build:` apontando para as pastas-fonte irmãs dos fronts; nunca vão para a droplet |
| `nginx/gateway.conf` | backend (este repo) | roteamento do gateway nginx em produção |
| `scripts/deploy/sync-config.sh` | backend (este repo) | envia compose+nginx+certbot config para a droplet, idempotente, sem tocar no `.env` |
| `secrets/prod.enc.env` | backend (este repo, plano 23) | `.env` de produção criptografado via SOPS/age — decriptado só na droplet |

## Autenticação no GHCR (login na droplet)

A droplet precisa estar autenticada no GHCR para `docker compose pull`
funcionar contra imagens privadas. Duas opções (recomendação: a primeira):

### Opção recomendada — `docker login` com PAT de leitura

1. Criar um **Personal Access Token (classic)** dedicado, com escopo
   **apenas** `read:packages` (não usar um token com escopos mais amplos).
2. Executar uma vez na droplet (idealmente já no bootstrap via cloud-init,
   plano 17, ou como primeiro passo do job `deploy` antes do primeiro
   `pull`):
   ```bash
   echo "$GHCR_READ_PAT" | docker login ghcr.io -u <usuario-github> --password-stdin
   ```
3. O `docker login` persiste a credencial em `~/.docker/config.json` no
   host (ou no `$HOME` do usuário usado pelo SSH) — sobrevive a reboots da
   droplet; não precisa repetir a cada deploy, só se o token expirar/for
   rotacionado.
4. Armazenar o PAT como secret do GitHub
   (`GHCR_READ_PAT`/`secrets.GHCR_READ_PAT`) se o login for feito como
   parte do job de deploy em vez do cloud-init; nunca commitar o valor.

### Alternativa — pacotes públicos no GHCR

Tornar os pacotes `labviromol-api`, `labviromol-migrate`,
`labviromol-admin` e `labviromol-institucional` **públicos** no GHCR
elimina a necessidade de login para `pull`. Trade-off: qualquer pessoa
pode baixar as imagens (não há segredo de aplicação dentro delas — config
sensível vem só do `.env` em runtime — mas é uma decisão de superfície de
exposição que deve ser avaliada antes de ativar).

## Comandos manuais úteis (operação/diagnóstico)

Executados **na droplet**, dentro da pasta de deploy (`~/labviromol-deploy`
por padrão, configurável no `sync-config.sh`):

```bash
# Ver status de todos os serviços
docker compose -f docker-compose.prod.yaml ps

# Puxar e subir só a API (sem afetar migrate/fronts)
docker compose -f docker-compose.prod.yaml pull api
docker compose -f docker-compose.prod.yaml up -d api

# Rodar a migração manualmente (fora do fluxo automatizado do CD)
docker compose -f docker-compose.prod.yaml run --rm migrate

# Conferir saúde da API
curl -fsS http://127.0.0.1:8080/health/ready   # de dentro da droplet
# ou, de fora, via gateway:
curl -fsS https://lab.vitorespinoza.com/api/health/ready

# Logs
docker compose -f docker-compose.prod.yaml logs -f api
```

## O que precisa de aprovação humana antes de produção real

- **GitHub Environment `production`** com required reviewers configurado
  (gate de aprovação manual do job `deploy`, plano 16) — sem isso, o deploy
  roda sem revisão humana.
- **Decisão sobre autenticação no GHCR**: login com PAT dedicado
  (recomendado) vs. tornar os pacotes públicos — decisão de superfície de
  exposição — avaliar antes de decidir.
- **Secrets de infraestrutura** no GitHub (`DROPLET_HOST`, `DROPLET_USER`,
  `DROPLET_SSH_KEY`, e o PAT do GHCR se optar pelo login via CI em vez de
  cloud-init).
- **Primeira execução real do `sync-config.sh`** contra a droplet de
  produção (até aqui só validado: `docker compose -f
  docker-compose.prod.yaml config`, sintaxe do script, e o caminho de erro
  de uso incorreto — nenhuma transferência SSH real foi feita).
- **Bug conhecido e bloqueante**: a imagem
  `labviromol-migrate` não builda hoje (`MSB3552` em `dotnet ef migrations
  bundle`). O serviço `migrate` deste compose
  está pronto para uso, mas a imagem que ele referencia não existe
  publicada no GHCR até esse bug ser corrigido — **não tentar deploy real
  do backend em produção antes disso**, ou a etapa migrate-first
  vai falhar já no `pull`.
