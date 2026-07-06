# Runbook — State cutover: manual droplet (current) → Terraform droplet (new)

**English** · [Português](./droplet-cutover.pt-BR.md)

**Audience:** any human with SSH access to both droplets and to the
DigitalOcean dashboard, even without prior context on the project.
Sequential execution, top to bottom.

**When to use:** when adopting the immutable infrastructure
(`infra/terraform/`) for the first time — i.e., when a `labviromol-prod`
production droplet already exists, created manually (without Terraform),
with a real Postgres, real uploads and real TLS certificates, and the
goal is to replace it with a new droplet provisioned via `terraform
apply`, **without losing data**.

**This is not a DNS flip.** DNS already points to DigitalOcean's
**reserved IP** (managed outside of Terraform today, or via `dns.tf` if
`manage_dns = true`). The actual cutover is the **reassignment of the
reserved IP** between droplets — the domain's value never changes. The
heavy lifting in this runbook is moving the database, uploads and
certificates, not DNS.

## 0. Vocabulary used in this runbook

| Term | Meaning here |
|---|---|
| **old droplet** | The current manual production droplet, today serving `lab.vitorespinoza.com`. |
| **new droplet** | The droplet created by `terraform apply` from `infra/terraform/` (plan 17), resource `digitalocean_droplet.app`. |
| **reserved IP** | `digitalocean_reserved_ip.app` (Terraform) — the stable IP that DNS points to. Today, on the old droplet, it's presumably already a reserved IP managed manually in the dashboard; `terraform apply` **adopts the concept**, but creates a **new, distinct reserved IP** (Terraform doesn't automatically import the old IP — see §1.4). |
| **data volume** | `digitalocean_volume.postgres_data` (Terraform) — a block volume attached to the new droplet, mount point `/mnt/labviromol_data/postgres` (prepared by `cloud-init.yaml`, see §0.1 below about the mismatch). |

### 0.1 Known mismatch between Terraform and `docker-compose.yaml` — read before continuing

The new droplet's `cloud-init.yaml` creates and prepares the
`/mnt/labviromol_data/postgres` directory on the attached block volume
(`digitalocean_volume.postgres_data`), with the intent that Postgres
writes there (persistent data surviving a droplet rebuild).

**However**, this repo's `docker-compose.yaml` (root) declares the
`postgres` service's volume as a **named Docker volume** (`postgres_data:`,
managed by Docker under `/var/lib/docker/volumes/...`), **not** as a bind
mount to `/mnt/labviromol_data/postgres`:

```yaml
services:
  postgres:
    volumes:
      - postgres_data:/var/lib/postgresql/data
volumes:
  postgres_data:
```

In other words, **today Terraform's block volume isn't actually connected
to the compose's Postgres** — Postgres writes to the droplet's root disk
(inside the default Docker volume), not to the attached block volume.
This means that if the droplet is destroyed carelessly, Postgres data
**does not automatically survive** via the block volume, contrary to what
the immutable infrastructure intends.

**Practical implication for this runbook:** the database restore (step 3)
is done via `pg_dump`/`pg_restore` into the new droplet's named Docker
volume — it works regardless of this mismatch, because we don't depend on
the bind mount to move the data. But **note**: for the block volume to
serve its purpose (data surviving future rebuilds without repeating this
entire runbook), production's `docker-compose.yaml` needs to be adjusted
to mount `/mnt/labviromol_data/postgres:/var/lib/postgresql/data` as a
bind mount, instead of the named volume `postgres_data`. That fix has not
been applied yet.

---

## 1. Pre-checks (before anything else)

### 1.1 Snapshot of the old droplet (safety net)

In the DigitalOcean dashboard (or via `doctl`), create a snapshot of the
old droplet **before** starting any migration step:

```bash
doctl compute droplet-action snapshot <OLD_DROPLET_ID> \
  --snapshot-name "labviromol-prod-pre-cutover-$(date +%Y%m%d)"
```

Wait for the snapshot to complete (DO dashboard → Images → Snapshots)
before proceeding. This snapshot is the last-resort rollback: if
something goes irrecoverably wrong, it allows recreating the old droplet
from scratch, from the state prior to the cutover.

### 1.2 Confirm the new droplet is provisioned and healthy

The new droplet should already exist, provisioned by plan 17/18
(`terraform apply` — via `infra.yml`, `apply` job, or manually):

```bash
terraform output reserved_ip      # new reserved IP (not yet the production one)
terraform output droplet_public_ip
terraform output postgres_volume_id
```

Bring up the application stack on the new droplet (empty database, not
yet production) and confirm it responds:

```bash
ssh deploy@<new_droplet_public_ip> "curl -fsS http://localhost:8080/health/ready"
# expected: HTTP 200
```

Also confirm that the migrations ran (empty database, but with the
schema created) and that cloud-init's `docker login ghcr.io` worked
(`docker compose pull` succeeds).

### 1.3 Announce the maintenance window

Communicate the planned downtime window to users (e-mail, banner on the
frontend, or notice on the institutional site). Expected duration:
minutes (dump/restore of a lab database, not terabytes), but treat it as
total application unavailability during this runbook's execution.

Optional: put the old droplet in read/maintenance mode (e.g., scale down
the `api` service in the compose file, leaving only a static placeholder)
to guarantee no new writes happen to the database while the dump is being
taken — this avoids an inconsistency window between the dump and the
traffic cutover.

### 1.4 Reserved IP — watch the distinction between the current IP and Terraform's

If the current production reserved IP (the one DNS points to today) was
**not** originally created by Terraform, plan 17's
`digitalocean_reserved_ip.app` is a **new resource, with a different IP
address** from the current production reserved IP. Confirm this before
step 6:

```bash
# Current production IP (what DNS resolves today)
dig +short lab.vitorespinoza.com

# Reserved IP created by Terraform (new)
terraform output reserved_ip
```

If the two differ, the traffic cutover (§6) necessarily needs to change
the DNS A record to the new reserved IP (reassignment alone isn't
enough — the IP itself changed). If the user prefers to keep the same
historical IP, the alternative is to import the existing reserved IP into
Terraform's state (`terraform import digitalocean_reserved_ip.app
<existing-ip-id>`) before this runbook — an infrastructure decision
outside this document's scope, but critical to confirm here so as not to
assume "the reserved IP doesn't change" when in practice it does.

---

## 2. Freeze writes on the old droplet

Before the dump, make sure no new writes occur during the window:

```bash
# On the old droplet
ssh deploy@<old_droplet_ip>
cd ~/app   # or wherever the production docker-compose.yaml is
docker compose stop api migrate admin institucional
# Postgres stays up (we need it up for pg_dump),
# only the application stops accepting traffic/writes.
```

---

## 3. Postgres migration

### 3.1 Dump on the old droplet (custom format)

```bash
# Still on the old droplet
docker compose exec -T postgres pg_dump \
  -U "${POSTGRES_USER:-labviromol}" \
  -d "${POSTGRES_DB:-LabViroMol}" \
  -Fc -f /tmp/labviromol-cutover.dump

docker compose cp postgres:/tmp/labviromol-cutover.dump /tmp/labviromol-cutover.dump
```

Confirm the file size (`ls -lh /tmp/labviromol-cutover.dump`) — an empty
dump (a few KB) indicates a silent auth/connection failure; do not
proceed in that case.

### 3.2 Transfer the dump to the new droplet

```bash
scp /tmp/labviromol-cutover.dump deploy@<new_droplet_ip>:/tmp/labviromol-cutover.dump
```

### 3.3 Restore on the new droplet

The new droplet's database already exists (created empty by the
migrations in step 1.2). `pg_restore` into it:

```bash
# On the new droplet
ssh deploy@<new_droplet_ip>
cd ~/app

# Stop the API to guarantee no concurrent connection interferes with the restore
docker compose stop api migrate admin institucional

# Copy the dump into the Postgres container
docker compose cp /tmp/labviromol-cutover.dump postgres:/tmp/labviromol-cutover.dump

# Restore. --clean removes the empty schema's objects (created by the
# migrations) before recreating them from the dump — needed because the
# target database isn't really empty (it has a schema, no data).
docker compose exec -T postgres pg_restore \
  -U "${POSTGRES_USER:-labviromol}" \
  -d "${POSTGRES_DB:-LabViroMol}" \
  --clean --if-exists --no-owner --no-privileges \
  /tmp/labviromol-cutover.dump
```

`--no-owner --no-privileges`: avoids `pg_restore` failures due to a
different role/owner name between the old and new droplets (the Postgres
user may have the same name by convention, but don't rely on that).

### 3.4 Validate row counts per schema

Before moving on, confirm that the row counts of the main tables in each
schema of the (old) dump match the restored new droplet. The 6 module
schemas (`assets`/`identity`/`inventory`/`notify`/`research`/`scheduling`):

```bash
# Run the same script on both droplets (old, before any new write; new,
# after the restore) and compare the output line by line.
docker compose exec -T postgres psql -U "${POSTGRES_USER:-labviromol}" -d "${POSTGRES_DB:-LabViroMol}" -c "
SELECT schemaname, relname, n_live_tup
FROM pg_stat_user_tables
WHERE schemaname IN ('assets','identity','inventory','notify','research','scheduling')
ORDER BY schemaname, relname;
"
```

`n_live_tup` is a planner estimate (fast, non-blocking) — enough to
detect gross divergences (an empty table when it shouldn't be, differing
orders of magnitude). If something looks suspicious in a specific table,
confirm with an exact `SELECT count(*)` on just that table before
proceeding.

**Acceptance criterion for this step:** every table with `n_live_tup > 0`
on the old droplet has an equal (or compatible, considering there may
have been some residual write before `docker compose stop` in step 2)
count on the new droplet. Any entirely zeroed-out schema indicates a
restore failure — do not proceed to the traffic cutover in that case.

---

## 4. Uploads migration

The API's image uploads (`Storage__RootFolder=/app/Upload/Images`, named
Docker volume `uploads_images` in `docker-compose.yaml`) need to be
copied via `rsync`, container to container (both hosts already have
`rsync` installed by `cloud-init.yaml`, package `rsync`):

```bash
# On the old droplet: find the Docker volume's real path on the host
ssh deploy@<old_droplet_ip> \
  "docker volume inspect app_uploads_images --format '{{ .Mountpoint }}'"
# (the Docker volume name prefix follows the compose directory's name,
#  e.g. "app_uploads_images" if the compose runs in ~/app — confirm with
#  `docker volume ls` if the name doesn't match)

# rsync directly between the two hosts over SSH (run from a machine with
# access to both droplets, or from one to the other):
rsync -avz --progress \
  -e ssh \
  deploy@<old_droplet_ip>:/var/lib/docker/volumes/app_uploads_images/_data/ \
  deploy@<new_droplet_ip>:/var/lib/docker/volumes/app_uploads_images/_data/
```

If the named Docker volume names diverge between the two droplets (e.g.,
the compose directory has a different folder name on each — not the
expected case if both use `~/app`, but confirm with `docker volume ls`
before assuming), adjust `rsync`'s destination to match the actual
mountpoint reported by the new droplet.

**Validation:** compare file count and total size on both ends:

```bash
ssh deploy@<old_droplet_ip> "find /var/lib/docker/volumes/app_uploads_images/_data -type f | wc -l"
ssh deploy@<new_droplet_ip>  "find /var/lib/docker/volumes/app_uploads_images/_data -type f | wc -l"
```

The two numbers should match.

---

## 5. TLS certificates

Two options — pick one:

### Option A — copy the existing certificates (faster, no rate limit)

```bash
# certbot_certs is a named Docker volume, mounted at /etc/letsencrypt
# on the gateway/certbot service of docker-compose.yaml
ssh deploy@<old_droplet_ip> \
  "docker run --rm -v app_certbot_certs:/certs -v /tmp:/backup alpine \
   tar czf /backup/letsencrypt-backup.tar.gz -C /certs ."

scp deploy@<old_droplet_ip>:/tmp/letsencrypt-backup.tar.gz /tmp/
scp /tmp/letsencrypt-backup.tar.gz deploy@<new_droplet_ip>:/tmp/

ssh deploy@<new_droplet_ip> \
  "docker run --rm -v app_certbot_certs:/certs -v /tmp:/backup alpine \
   tar xzf /backup/letsencrypt-backup.tar.gz -C /certs"
```

### Option B — re-issue via certbot (Cloudflare DNS-01) on the new droplet

Prerequisite: `~/app/certbot/cloudflare.ini` needs to be present on the
new droplet **with `chmod 600` permission** before bringing up the
`certbot` service in `docker-compose.yaml` (the service mounts this file
read-only and certbot refuses credentials with permissions looser than
`600`):

```bash
# Copy cloudflare.ini from the old droplet (or from wherever it's kept
# outside the repo — never committed) to the new droplet
scp deploy@<old_droplet_ip>:~/app/certbot/cloudflare.ini /tmp/cloudflare.ini
scp /tmp/cloudflare.ini deploy@<new_droplet_ip>:~/app/certbot/cloudflare.ini
ssh deploy@<new_droplet_ip> "chmod 600 ~/app/certbot/cloudflare.ini"

# Issue the certificate (run once, manually, before bringing up the
# automatic renewal loop of the certbot service)
ssh deploy@<new_droplet_ip> "cd ~/app && docker compose run --rm certbot \
  certbot certonly --dns-cloudflare \
  --dns-cloudflare-credentials /etc/cloudflare.ini \
  -d lab.vitorespinoza.com --non-interactive --agree-tos \
  -m <admin-email>"
```

Option A avoids Let's Encrypt's rate limit (5 certificates/week per exact
domain) and is preferable if the old droplet's certificates still have
reasonable validity. Option B is simpler to reason about (clean state)
but should only be used if Option A fails or if the old certificates are
close to expiring anyway.

**Validation (either option):**

```bash
ssh deploy@<new_droplet_ip> "docker compose exec gateway nginx -t"
ssh deploy@<new_droplet_ip> "ls -la /var/lib/docker/volumes/app_certbot_certs/_data/live/lab.vitorespinoza.com/"
# expected: fullchain.pem, privkey.pem, cert.pem, chain.pem present
```

---

## 6. Traffic cutover

### 6.1 Bring up the full stack on the new droplet

```bash
ssh deploy@<new_droplet_ip>
cd ~/app
docker compose up -d
docker compose ps   # confirm all services "healthy"/"running"
curl -fsS http://localhost:8080/health/ready
```

### 6.2 Reassign the reserved IP

Via Terraform (preferable — keeps the state as the source of truth):

```bash
# infra/terraform/main.tf already declares:
#   resource "digitalocean_reserved_ip_assignment" "app" {
#     ip_address = digitalocean_reserved_ip.app.ip_address
#     droplet_id = digitalocean_droplet.app.id
#   }
# If the new droplet is the current state's digitalocean_droplet.app, the
# reserved_ip_assignment has already been pointing to it since the
# initial `apply` (step 1.2) — confirm:
terraform state show digitalocean_reserved_ip_assignment.app
```

If the real production reserved IP (DNS) is the same as Terraform's (see
§1.4), the reassignment has already been in effect since the new
droplet's provisioning — no further action here, just **confirm** via the
DigitalOcean dashboard (Networking → Reserved IPs) that the IP points to
the new droplet, not the old one.

If the real production reserved IP is **different** from the one created
by Terraform (a common case on first adoption, see §1.4): update the DNS
A record (Cloudflare dashboard, or `terraform apply` with `manage_dns =
true` pointing to `terraform output reserved_ip`) to the new IP. This
path **is not instantaneous** — it's subject to the DNS TTL (300s, see
`dns.tf`) and to intermediate resolver caches.

### 6.3 Validate the cutover

```bash
curl -fsS https://lab.vitorespinoza.com/health/ready
# expected: 200, responding from the new droplet

# Confirm via the reported source IP / or compare a unique identifier
# (e.g. logging boot timestamp or hostname on a debug endpoint, if one
# exists) to be sure traffic is actually flowing through the new droplet,
# not serving cache from an intermediate CDN/proxy.
dig +short lab.vitorashospital.com  # confirm resolution to the expected IP
```

Manually validate at least one critical flow of each module (login,
material listing, schedule creation, etc.) through the public URL, not
just via `/health/ready`.

---

## 7. Post-cutover

1. **Monitor** (New Relic — `OTEL_EXPORTER_OTLP_*` already configured in
   the compose file) for a grace period (recommended: at least 24-48h)
   before any destructive action. Watch error rate, latency and resource
   usage (the new droplet has the same `s-2vcpu-4gb` as the old one, so
   there shouldn't be a capacity regression).
2. **Confirm critical flows** with real users/manual QA, not just
   automated smoke tests.
3. **Only then destroy the old droplet.** Two ways:
   - If the old droplet was never managed by Terraform (the expected case
     on this first adoption): manual destruction via the DigitalOcean
     dashboard or `doctl compute droplet delete <OLD_DROPLET_ID>`.
   - If at some point it was imported into Terraform's state:
     `terraform destroy -target=<old_droplet_resource>`.

   Before destroying, confirm that the **snapshot from step 1.1** still
   exists and is intact — it's the only recovery artifact once the old
   droplet is removed.

---

## 8. Rollback of the cutover

If a critical problem is discovered **after** step 6 but **before**
destroying the old droplet (the grace period from step 7): the old
droplet remained intact (its app services stopped since step 2, but not
destroyed), so the rollback is simply undoing step 6.2:

```bash
# Restart the app services on the old droplet (stopped since step 2)
ssh deploy@<old_droplet_ip> "cd ~/app && docker compose start api migrate admin institucional"

# Reassign the reserved IP back to the old droplet
```

Via Terraform, this means temporarily reverting
`reserved_ip_assignment.app.droplet_id` to point back to the old
droplet's ID — this is only straightforward if the old droplet is also
represented in Terraform's state (not the default case on this first
adoption, see §1.4). In the common case (old droplet outside Terraform),
the rollback is manual via the DigitalOcean dashboard (Networking →
Reserved IPs → reassign) or, if the DNS IP changed in step 6.2, reverting
the A record back to the old IP.

**Watch for data written after the cutover:** if the rollback happens
after the new droplet already received real writes (not just reads),
those writes **don't exist on the old droplet** — infrastructure rollback
(IP) is not a data rollback. If this happens, step 3 will need to be
repeated (dump the new droplet → restore on the old one) before
reassigning the IP, or accept the loss of writes made in the interval (a
business decision, not a technical one).

---

## Medium-term recommendation

Migrating to **DigitalOcean Managed Postgres** + **Spaces** (for uploads)
eliminates steps 3 and 4 on future rebuilds — the droplet becomes truly
stateless (only running application containers, without its own data),
and a future rebuild no longer requires this entire runbook, reducing to
"point the new droplet to the same managed database/bucket". TLS
certificates (step 5) would still need handling, but they'd be the only
remaining state.

## Before a real cutover

Rehearse this runbook in a dry-run environment (a disposable droplet, a
test database with synthetic data) before the real production cutover.
Time the actual duration of the maintenance window in that rehearsal to
inform the downtime announcement in step 1.3.
