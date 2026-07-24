# Runbook — Production deploy (pull-based)

**English** · [Português](./deploy.pt-BR.md)

> Fully replaces the old `deploy.sh` (retired in plan
> `22-prod-compose-and-config-sync`). There's no more local build +
> `docker save | ssh | docker load`. Every deploy is **pull-based**: the
> droplet only pulls images already published to GHCR.

## Flow overview

1. **CI** (`.github/workflows/cd.yml`, plan 15) builds, scans
   (`container-scan`), signs (cosign keyless) and publishes to GHCR the
   `labviromol-api` and `labviromol-migrate` images, tagged `latest` +
   `<sha>`.
2. **CD** (`deploy` job in `cd.yml`, plan 16, manual approval gate on the
   GitHub Environment `production`) connects over SSH to the droplet and:
   1. Runs `scripts/deploy/sync-config.sh` to update
      `docker-compose.prod.yaml` + `nginx/gateway.conf` (+
      `certbot/cloudflare.ini`, if present) on the droplet — without
      touching the server's `.env`.
   2. Decrypts `.env` via SOPS (plan 23) from `secrets/prod.enc.env`.
   3. Runs the migration (`docker compose -f docker-compose.prod.yaml run
      --rm migrate`) **before** touching the API — an exit-code gate.
   4. If the migration succeeds, runs `docker compose -f
      docker-compose.prod.yaml pull api && ... up -d api` and validates
      `/health/ready`.
   5. If the migration fails, it **aborts** — the old API keeps serving
      (fix-forward policy, no automatic image rollback).
3. **Frontends** (`admin`, `institucional`) are deployed by their own
   repos (plan 11 of each), updating **only their own service** against
   this same `docker-compose.prod.yaml` on the droplet. The backend
   neither builds nor triggers the frontends' deploy — it only owns the
   orchestration manifest.

## Files involved

| File | Owner | Role |
|---|---|---|
| `docker-compose.prod.yaml` | backend (this repo) | production pull-based manifest — the only compose file that runs on the droplet |
| `docker-compose.yaml` + `docker-compose.override.yml` | backend (this repo) | **local dev only** — have `build:` pointing to the frontends' sibling source folders; never go to the droplet |
| `nginx/gateway.conf` | backend (this repo) | nginx gateway routing in production |
| `scripts/deploy/sync-config.sh` | backend (this repo) | ships compose+nginx+certbot config to the droplet, idempotent, without touching `.env` |
| `secrets/prod.enc.env` | backend (this repo, plan 23) | production `.env` encrypted via SOPS/age — decrypted only on the droplet |

## GHCR authentication (login on the droplet)

The droplet needs to be authenticated to GHCR for `docker compose pull`
to work against private images. Two options (recommendation: the first):

### Recommended option — `docker login` with a read PAT

1. Create a dedicated **Personal Access Token (classic)**, scoped to
   **only** `read:packages` (don't use a token with broader scopes).
2. Run once on the droplet (ideally already in the cloud-init bootstrap,
   plan 17, or as the first step of the `deploy` job before the first
   `pull`):
   ```bash
   echo "$GHCR_READ_PAT" | docker login ghcr.io -u <github-username> --password-stdin
   ```
3. `docker login` persists the credential in `~/.docker/config.json` on
   the host (or in the `$HOME` of the SSH user) — it survives droplet
   reboots; no need to repeat it on every deploy, only if the token
   expires/is rotated.
4. Store the PAT as a GitHub secret
   (`GHCR_READ_PAT`/`secrets.GHCR_READ_PAT`) if the login happens as part
   of the deploy job instead of cloud-init; never commit the value.

### Alternative — public packages on GHCR

Making the `labviromol-api`, `labviromol-migrate`, `labviromol-admin` and
`labviromol-institucional` packages **public** on GHCR removes the need
to log in for `pull`. Trade-off: anyone can download the images (there's
no application secret inside them — sensitive config only comes from
`.env` at runtime — but it's an exposure-surface decision that should be
weighed before enabling).

## Useful manual commands (operation/diagnostics)

Run **on the droplet**, inside the deploy folder (`~/labviromol-deploy`
by default, configurable in `sync-config.sh`):

```bash
# Check status of all services
docker compose -f docker-compose.prod.yaml ps

# Pull and start only the API (without touching migrate/frontends)
docker compose -f docker-compose.prod.yaml pull api
docker compose -f docker-compose.prod.yaml up -d api

# Run the migration manually (outside the automated CD flow)
docker compose -f docker-compose.prod.yaml run --rm migrate

# Check API health
curl -fsS http://127.0.0.1:8080/health/ready   # from inside the droplet
# or, from outside, via the gateway:
curl -fsS https://lab.vitorespinoza.com/api/health/ready

# Logs
docker compose -f docker-compose.prod.yaml logs -f api
```

## What needs human approval before a real production deploy

- **GitHub Environment `production`** with required reviewers configured
  (manual approval gate on the `deploy` job, plan 16) — without this, the
  deploy runs without human review.
- **Decision on GHCR authentication**: login with a dedicated PAT
  (recommended) vs. making the packages public — an exposure-surface
  decision — weigh it before deciding.
- **Infrastructure secrets** in GitHub (`DROPLET_HOST`, `DROPLET_USER`,
  `DROPLET_SSH_KEY`, and the GHCR PAT if login is done via CI instead of
  cloud-init).
- **First real run of `sync-config.sh`** against the production droplet
  (so far only validated: `docker compose -f docker-compose.prod.yaml
  config`, the script's syntax, and the error path for incorrect usage —
  no real SSH transfer has been performed).
