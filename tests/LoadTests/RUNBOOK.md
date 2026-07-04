# Runbook — step by step for the load-test campaign

**English** · [Português](./RUNBOOK.pt-BR.md)

This file is the **execution checklist**, in the order you should follow it. It doesn't explain the "why"
behind each piece (that's in [README.md](README.md)) nor the details of how to build tables/write the
chapter (that's in [REPORTING.md](REPORTING.md)) — this is just "do this, then this, then this", so you
don't skip a step or forget to save evidence along the way.

Check off each item as you go. Whenever a step generates a report, the next step is to **copy the
evidence before running the next command** — don't let it pile up to copy everything at the end.

## Topology: where each command runs

This is **not a local test**. Today you only have one server (the production one, still without a
client exposed) — it plays the role of staging. The commands below are split across two different
places:

| Command                                        | Where it runs                                                | Why                                                                                                                                                                      |
| ---------------------------------------------- | -------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `docker compose ... up -d`                     | **On the server**, via SSH                                 | That's where the API/Postgres/LibreTranslate/nginx need to be — you're testing the real infrastructure                                                                       |
| `--command=seed` / `--command=reset`           | **On your laptop**, via an SSH tunnel to the server's Postgres | They talk **directly to Postgres** (bypassing the API/nginx); the server doesn't have the .NET SDK to run `dotnet run`, and the seed catalog needs to live in the same place the `--scenario=...` is run from (see "SSH tunnel" section below) |
| `--scenario=write` / `--scenario=mixed` / `mixed-public-admin` | **On your laptop**, **with the SSH tunnel and the connection string too** | Scenarios with administrative writes can exhaust the pre-seeded pending schedule pool and replenish it directly in Postgres in real time (`Seeder.AppendPendingSchedulesAsync`). Without the connection string, this throws an exception and inflates the test's error rate |
| `--scenario=read-simple` / `read-complex` / `public-read` / `resilience` | **On your laptop**, only with `--baseUrl` (no tunnel) | They're 100% HTTP, never touch Postgres directly |
| `--scenario=... --profile=...` (the load itself) | **On your laptop** (or another machine, outside the server) | This is the "external load agent" from README section 6 — generating load *from outside* avoids the *noisy neighbor* effect (the generator stealing CPU from the target) and measures the real cost of TLS/network to the server |

To run from the laptop against the server, every `--scenario=...` execution needs the `--baseUrl` pointing
outward. **Always use `lab.vitorespinoza.com`, never the root domain `vitorespinoza.com`** — the server's
TLS certificate is only valid for the `lab.` subdomain (see README section 8, the
`ERR_CERT_COMMON_NAME_INVALID` finding):

```bash
--baseUrl=https://lab.vitorespinoza.com
```

Every command in this runbook **already includes this `--baseUrl`** — you don't need to add it manually.
If you ever change server/domain, just swap this value in every block.

Suggested evidence folder (create it before starting, on your laptop — this is where the evidence lives):

```bash
mkdir -p evidencias-tcc/capitulo-testes/{00-smoke,01-load-nominal,02-stress,03-soak,04-resiliencia,05-ablation-libretranslate,06-campanha-b}
```

---

## SSH step by step (from scratch)

If you've never used SSH manually, here's the idea: it's a way to open a "terminal inside the server", as
if you were typing directly on it, even though you're on your laptop.

### Opening a session (to run several commands in sequence)

1. Open a terminal on your laptop (Git Bash, PowerShell, or the VS Code terminal).
2. Type:

```bash
ssh root@142.93.14.97
```

3. **The first time**, you'll see something like:

```
The authenticity of host '142.93.14.97' can't be established.
...
Are you sure you want to continue connecting (yes/no)?
```

Type `yes` and Enter. This only shows up once (after that the server is "known" in your
`~/.ssh/known_hosts`).

4. If the SSH key is already configured (which was the case here — no password was asked), you'll land
   straight on the server's prompt. It changes from `user@your-laptop` to something like
   `root@TCC-VITOR:~#` — **this is the sign that you're now "inside" the server**, not on your laptop
   anymore.
5. From here on, every command you type runs **on the server**. For example:

```bash
cd ~/labviromol-deploy
docker compose ps
```

6. To **exit** and go back to your laptop: type `exit` and Enter (or `Ctrl+D`). The prompt goes back to
   showing your laptop's name — confirming you've exited.

### Running a single command without "entering" (the way used in most of this runbook)

Instead of opening a session and typing command by command, you can send the command directly, in
quotes, along with the `ssh` command itself. The server executes it, shows the result, and you're
automatically back on your laptop — no need to type `exit`:

```bash
ssh root@142.93.14.97 "docker compose -f labviromol-deploy/docker-compose.yaml ps"
```

This is what it means whenever a block in this runbook already comes with `ssh root@142.93.14.97 "..."`
in front — you **don't** need to open a session beforehand; just paste the whole block (with `ssh` and
everything) into your local terminal and hit Enter. It comes back to you on its own.

To run **more than one command** in this single-line format, separate them with `&&` (only runs the next
one if the previous one didn't fail) or `;` (runs the next one regardless):

```bash
ssh root@142.93.14.97 "cd ~/labviromol-deploy && docker compose ps && docker logs labviromol-api --tail 20"
```

### Copying a file to/from the server (`scp`) — same logic as `ssh`

```bash
# from your laptop TO the server:
scp docker-compose.yaml root@142.93.14.97:~/labviromol-deploy/

# from the server TO your laptop:
scp root@142.93.14.97:~/labviromol-deploy/.env .env
```

> **Fixed rule for this runbook:** every `docker compose` on the server assumes you're inside
> `~/labviromol-deploy` — that's where the `docker-compose.yaml` and the overlays (`loadtest.A.yaml`,
> etc.) live. If you open a manual SSH session (instead of the one-line `ssh host "command"` format),
> **the first command is always `cd ~/labviromol-deploy`** before any `docker compose`. Running from
> outside that folder gives `open /root/docker-compose.yaml: no such file or directory`.

### SSH tunnel (to reach the server's Postgres without installing anything there)

The server **doesn't have the .NET SDK installed** — only Docker. That means `dotnet run` doesn't work
there. That's why `--command=seed` and `--command=reset` (which talk directly to Postgres, bypassing the
API) also run **from your laptop**, not from the server — and Postgres only listens on `127.0.0.1:5432`
*inside* the server (not exposed to the internet), so you open an SSH tunnel that "pretends" the server's
Postgres is at `localhost` on your laptop:

```bash
ssh -fN -L 15432:localhost:5432 root@142.93.14.97
```

- `-L 15432:localhost:5432`: port `15432` on your laptop ↔ `localhost:5432` as seen **from inside the
  server** (which is where Postgres actually listens).
- `-f -N`: runs in the background, without opening an interactive terminal (just makes the tunnel and
  gives you your prompt back).
- To close the tunnel afterward: find the process (`ps aux | grep 15432`) and kill it with `kill <PID>`,
  or simply close the terminal/restart the laptop.

With the tunnel open, whenever you're about to run `--command=seed`/`--command=reset`, **or
`--scenario=write`/`--scenario=mixed`** (these two replenish pending schedules directly in Postgres when
the pre-seeded pool runs out under load — see the topology table above), prefix the command with the
connection string pointing at the tunnel, using the real `POSTGRES_PASSWORD` from your local `.env`
(pulled from the server beforehand):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --command=seed
```

> This also solves a problem that would otherwise go unnoticed: `seed` writes a catalog file
> (`.artifacts/seed-catalog.json`, with the IDs the load scenarios use) in the build folder **of wherever
> the command was run from**. If `seed` ran on the server and `--scenario=...` ran on the laptop, this
> file would never be found by the latter — running both from the same place (your laptop), the problem
> doesn't even exist.

> **Forgot the connection string on a `--scenario=write`/`mixed`?** The error only shows up once the
> pending schedule pool runs out (not at the start) — it appears as
> `InvalidOperationException: ConnectionStrings:LabViroMol not configured` in the log/console midway
> through the run, and the `approve_schedule_write` scenario's error rate rises artificially. If this
> happens, kill the run, open the tunnel, and run it again with the connection string prefix.

---

## 0. Prerequisites (once)

- [ ] `dotnet build tests/LoadTests/LabViroMol.LoadTests.csproj -c Release` compiles without errors.
- [ ] The unit/integration test suite is green (`dotnet test`) — don't start load testing with a known
      functional bug.
- [ ] You've done the manual functional smoke (detailed step by step below).
- [ ] You've noted down the load machine's spec (CPU/RAM/network) **and** the server's — this goes into
      the methodology chapter.
- [ ] You've confirmed (from the laptop) that the server responds over HTTPS: `curl -I
      https://lab.vitorespinoza.com` (should respond with something like `HTTP/2 200`) — without this, no
      `--scenario=...` will work from the laptop.
- [ ] **You've sent the load-test overlays to the server** (only needs to be done once, or again if you
      edit these files locally — they don't get uploaded automatically, unlike the main
      `docker-compose.yaml`):

```bash
scp docker-compose.loadtest.A.yaml docker-compose.loadtest.B.yaml docker-compose.loadtest.noop-translate.yaml \
    root@142.93.14.97:~/labviromol-deploy/
```

- [ ] On the server (via SSH): `cd ~/labviromol-deploy && docker compose -f docker-compose.yaml -f
      docker-compose.loadtest.A.yaml config` doesn't error out (validates that the two files combine
      correctly before bringing them up).

### Manual functional smoke — step by step

The goal here is to click through the same flows the NBomber scenarios will hit (README section 3), **by
eye**, to catch any obvious bug before throwing load at it. Do it through the browser, in the Admin
Panel:

1. Open `https://lab.vitorespinoza.com/gestao-lab-ufpr/` in the browser.
2. Log in with your real admin user (doesn't need to be a test user — it's just to confirm the flow
   works).
   - [ ] Login completed without error, took you to the dashboard.
3. Go to **Schedules** → find a pending schedule → approve it.
   - [ ] Approval completed, the status changed to "approved" in the list.
4. Go to **Projects** → open an existing project → add a member (any researcher from the list) with the
   "Collaborator" role.
   - [ ] The member appeared in the project's list without error.
5. Go to **Materials** → create a new material (name, location, minimum stock, quantity, unit, type — any
   valid values).
   - [ ] The material appeared in the listing after being created.
6. If any of these steps errors out: **stop here**, don't start the load test. Open the browser DevTools
   (F12 → Network tab) to see which request failed, or check the API logs (`ssh root@142.93.14.97
   "docker logs labviromol-api --tail 50"`) to find the stack trace.

**Quick terminal alternative** (only confirms login works, without opening a browser):

```bash
curl -i -X POST https://lab.vitorespinoza.com/api/identity/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"YOUR_ADMIN_EMAIL","password":"YOUR_PASSWORD"}'
```

- [ ] A `200` response with a `Set-Cookie: X-Access-Token=...` header = login working. Any `4xx`/`5xx`
      here is already reason to investigate before continuing.

---

## 1. Campaign A — real translation (default configuration)

**On the server (via SSH, inside `~/labviromol-deploy`):**

```bash
cd ~/labviromol-deploy
docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml up -d
```

- [ ] Came up without error; `docker compose ps` shows `api`, `postgres`, `libretranslate`, `gateway`
      healthy.

### Applying migrations (only if this is a new/different database from production)

If you're reusing the same production Postgres that has already received a deploy (the normal case,
already handled), **skip this part** — the migrations have already been applied. If it's a genuinely new
database (e.g. you switched servers), the simplest approach is to reuse the `migrate` image (there's
already a `Dockerfile.migrate` in the repo for this):

```bash
# 1. On your laptop: builds and pushes the migration image to GHCR (once)
docker build -f Dockerfile.migrate -t ghcr.io/vitorespinoza/labviromol-migrate:latest .
docker push ghcr.io/vitorespinoza/labviromol-migrate:latest

# 2. On the server: pulls the image and runs it for each of the 6 DbContexts (the project has 6 modules,
#    each with its own DbContext/schema — needs to run once per context)
ssh root@142.93.14.97 'docker pull ghcr.io/vitorespinoza/labviromol-migrate:latest'

ssh root@142.93.14.97 'DB_CS=$(grep "^DB_CONNECTION_STRING=" ~/labviromol-deploy/.env | cut -d= -f2-); \
for ctx in LabViroMolIdentityDbContext InventoryDbContext AssetsDbContext ResearchDbContext SchedulingDbContext NotifyDbContext; do \
  echo "=== $ctx ==="; \
  docker run --rm --network labviromol-deploy_default -e ConnectionStrings__LabViroMol="$DB_CS" \
    ghcr.io/vitorespinoza/labviromol-migrate:latest --context $ctx; \
done'
```

- [ ] Each of the 6 contexts finished with `Done.` in the output (no errors).
- [ ] Restart the API to make sure it doesn't hold onto a stale connection: `ssh root@142.93.14.97
    "docker compose -f labviromol-deploy/docker-compose.yaml restart api"`.

**On your laptop**, with the SSH tunnel open (see "SSH tunnel" section at the top of this file):

```bash
ssh -fN -L 15432:localhost:5432 root@142.93.14.97

ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --command=seed
```

- [ ] Seed finished without exception; `.artifacts/seed-catalog.json` was created **in your laptop's
      build folder** (the same folder `--scenario=...` will run from later).

**From here on, every `--scenario=...` runs on your laptop** (external agent), with
`--baseUrl=https://lab.vitorespinoza.com` already included in every command below.

### 1.1 Smoke (sanity check before any real load)

```bash
dotnet run -c Release --project tests/LoadTests -- --scenario=read-simple --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] No 5xx; throughput > 0.
- [ ] **Copy the evidence now:**

```bash
cp -r tests/LoadTests/bin/Release/net10.0/reports/A/smoke/read-simple \
      evidencias-tcc/capitulo-testes/00-smoke/$(date +%F)_A_smoke_read-simple
echo "dotnet run -c Release --project tests/LoadTests -- --scenario=read-simple --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com" \
      > evidencias-tcc/capitulo-testes/00-smoke/$(date +%F)_A_smoke_read-simple/comando.txt
```

- [ ] Repeat the smoke for `read-complex` (admin dashboard, no connection string needed — read-only).
- [ ] Also run the institutional smoke:

```bash
dotnet run -c Release --project tests/LoadTests -- --scenario=public-read --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com
```
- [ ] **`write`**: needs the SSH tunnel open and the connection string prefix (see "SSH tunnel" section
      above), because under load it exhausts the pending schedule pool and replenishes it directly in
      Postgres:

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=write --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] **Before running `mixed`, do a `--command=reset` + `--command=seed`** (section 1.6 + 1, with the
      same connection string prefix). Without this, `mixed` will try to approve schedules and add
      members that `write` already consumed, and you'll see `422`/`409` errors that are **not real
      bugs**, just stale data.
- [ ] **`mixed`**: same connection string prefix as `write` (it also includes writes):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Copy evidence for each of the 3 (same pattern as `read-simple` above).

### 1.2 Nominal load (~10 min)

**With the SSH tunnel open and the connection string** (`mixed` writes and replenishes the pool under
load):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=load --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] `summary.json`: error < 0.1%, p95 within T per operation (see the table in README section 3).
- [ ] Copy evidence (same pattern as 1.1, `01-load-nominal` folder).
- [ ] In New Relic, trim the test's time window and screenshot `cqrs.duration`, Npgsql pool, CPU/GC.
      Save the screenshot in the same evidence folder.

### 1.2.1 Isolated institutional capacity (~10 min)

These scenarios measure how many simultaneous institutional virtual users the site supports without
significant administrative load. `summary.json` records `Load.ClosedCopies`, `Load.ApproxRps`, `Groups`
and `Operations`.

```bash
dotnet run -c Release --project tests/LoadTests -- --scenario=public-read --profile=institutional-100 --campaign=A --baseUrl=https://lab.vitorespinoza.com

dotnet run -c Release --project tests/LoadTests -- --scenario=public-read --profile=institutional-200 --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Use the highest valid profile to declare the isolated institutional capacity.

### 1.2.2 Joint institutional + administrative load (~10 min)

**With the SSH tunnel open and the connection string**, because the administrative flow includes writes:

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed-public-admin --profile=joint-100-20 --campaign=A --baseUrl=https://lab.vitorespinoza.com

ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed-public-admin --profile=joint-150-30 --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Check `Load.InstitutionalClosedCopies`, `Load.AdminClosedCopies`, RPS per group, and which
      operations first violated p95/p99 in `summary.json`.
- [ ] Treat `joint-100-20` as the initial main target; `joint-150-30` is the headroom/breaking-point step.

### 1.3 Stress (open model, finds the breaking point)

`read-complex` doesn't need a connection string; `write` does (SSH tunnel open):

```bash
dotnet run -c Release --project tests/LoadTests -- --scenario=read-complex --profile=stress --campaign=A --baseUrl=https://lab.vitorespinoza.com

ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=write --profile=stress --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Note the RPS at which errors start rising and which resource saturated first (status code → README
      section 8 table: 500 = pool, 502/504 = nginx, timeout = thread pool, restart = OOM).
- [ ] Copy evidence + New Relic screenshot (`02-stress` folder).

### 1.4 Soak — 60 min (memory leak / GC)

**With the SSH tunnel open and the connection string** (same reason as 1.2):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=soak --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Look at the NBomber latency-over-time chart: latency **should not** grow monotonically.
- [ ] Look at heap/GC in New Relic for the same window.
- [ ] Copy evidence + screenshot (`03-soak` folder). If there's latency drift, note it as a **finding**,
      don't hide it.

### 1.5 Resilience (rate limiter)

```bash
dotnet run -c Release --project tests/LoadTests -- --scenario=resilience --profile=smoke --campaign=A --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Confirms 429 starting from the 6th attempt within the same hour (limit = 5/hour, global).
- [ ] Copy evidence (`04-resiliencia` folder).

### 1.6 Reset between write runs (if repeating any profile above)

**On your laptop**, with the SSH tunnel open (same logic as seed):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --command=reset
```

---

## 2. LibreTranslate ablation (isolating the cost of translation)

**On the server (inside `~/labviromol-deploy`):**

```bash
cd ~/labviromol-deploy
docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml -f docker-compose.loadtest.noop-translate.yaml up -d
```

- [ ] Confirm it came up with the overlay (the `LoadTest__UseNoOpTranslator=true` variable active).

**On the laptop** (with the SSH tunnel open and the connection string, same reason as 1.2):

```bash
ConnectionStrings__LabViroMol="Host=localhost;Port=15432;Database=LabViroMol;Username=labviromol;Password=$(grep '^POSTGRES_PASSWORD=' .env | cut -d= -f2-)" \
  dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=load --campaign=A-noop-translate --baseUrl=https://lab.vitorespinoza.com
```

- [ ] Same profile/scenario as step 1.2, only the `--campaign` changes (avoids overwriting the real
      result).
- [ ] Copy evidence (`05-ablation-libretranslate` folder).
- [ ] Calculate the delta: `A` (real translation) vs `A-noop-translate` (mock) — this number is the
      CPU/RAM cost of LibreTranslate on the host. Store it in a small table alongside the evidence.
- [ ] **On the server**, switch back to the compose without the overlay before continuing
      (`docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml up -d`, without
      `noop-translate`).

---

## 3. Campaign B — VM ceiling (without container limits)

**On the server (inside `~/labviromol-deploy`):**

```bash
cd ~/labviromol-deploy
docker compose -f docker-compose.yaml -f docker-compose.loadtest.B.yaml up -d
```

- [ ] Repeat **all** steps 1.1 through 1.5 (remembering: `docker compose` on the server; `seed`/`reset`
      and `--scenario=...` on the laptop, via the SSH tunnel and
      `--baseUrl=https://lab.vitorespinoza.com`) swapping `--campaign=A` for `--campaign=B`.
- [ ] (Optional, same reasoning as step 2) also repeat the LibreTranslate ablation on Campaign B, with
      `--campaign=B-noop-translate`.
- [ ] Copy each piece of evidence into the `06-campanha-b` folder.

---

## 4. Consolidation (after all campaigns have run)

- [ ] Build the A × B × ablation comparative table (see REPORTING.md section 4) from the copied
      `summary.json` files.
- [ ] For each row of the table, confirm the corresponding New Relic screenshot is saved in the same
      evidence folder.
- [ ] Run the REPORTING.md final checklist (section 7) for each result before considering it "closed".

---

## 5. Writing the chapter

Use the structure from REPORTING.md section 6 (Methodology → Execution → Results → Analysis →
Limitations → Conclusion), filling in each subsection with what was collected in steps 1 through 4 above.
Don't write any conclusion before having the evidence (corresponding folder) saved — this avoids being
unable to back up a number at the defense.

- [ ] Methodology written (SLOs defined a priori, environment, control variables).
- [ ] Execution written (commands, dates, deviations from the plan).
- [ ] Results written (tables per profile + comparative table).
- [ ] Analysis written (client↔server correlation, Apdex per load range).
- [ ] Limitations written.
- [ ] Conclusion written (sustainable RPS, identified bottleneck, recommendation).

---

## Appendix A — New Relic queries (NRQL)

If you've never used New Relic: the query interface is **"Query your data"** in the side menu (magnifying
glass/database icon). Paste the query, adjust the time period in the top-right corner (or use
`SINCE`/`UNTIL` in the query itself, as below), and run it. `TIMESERIES` makes it appear as a chart over
time instead of a single number — use this whenever you want to look at behavior during a test, not just
the final result.

In all the queries below, swap the time window (`SINCE ... UNTIL ...`) for the exact time of the test you
noted in the evidence step (sections 1 to 3 of this runbook) — without this you'll mix traffic from the
test with traffic from some other time.

### A.1 — Is the API responding? (general health)

```sql
FROM Log SELECT count(*) WHERE entity.name = 'labviromol-api' SINCE 30 minutes ago TIMESERIES
```

**Why:** if this count drops to zero during the test, the API stopped logging — a symptom of a
crash/OOM (matches the Campaign A scenario of blowing up at 384MB, see README section 8).

### A.2 — Errors during the test, grouped by message

```sql
FROM Log SELECT count(*) WHERE entity.name = 'labviromol-api' AND severity.text = 'Error'
FACET message SINCE 30 minutes ago LIMIT 20
```

**Why:** this is the query that showed the SMTP error — it groups by error message type, so you quickly
see **which** errors happened and **how many times**, without reading logs line by line. Use this after
each profile (smoke/load/stress/soak) to check whether anything unexpected showed up.

### A.3 — See the full stack trace of a specific error

```sql
FROM Log SELECT timestamp, message, exception.message, exception.stacktrace
WHERE entity.name = 'labviromol-api' AND severity.text = 'Error'
SINCE 30 minutes ago LIMIT 50
```

**Why:** A.2 tells you "what" and "how much"; this one gives you "where in the code" (full stack trace),
so you can decide whether the error is expected (e.g. 429 from the rate limiter) or a real bug found by
the load test.

### A.4 — Latency per command/query (correlates with NBomber's p95/p99)

```sql
FROM Metric SELECT average(cqrs.duration), percentile(cqrs.duration, 95, 99)
WHERE entity.name = 'labviromol-api' FACET request SINCE 30 minutes ago TIMESERIES
```

**Why:** this is the metric that backs the table in REPORTING.md section 5 — `request` is the name of the
Mediator command/query (e.g. `ApproveScheduleCommand`). Compare the p95/p99 here with the p95/p99
NBomber reported for the same scenario: if they're similar, the latency is in the application; if the
client's is much higher, the extra time is network/nginx/queue, not the code itself.

### A.5 — Error rate per command/query

```sql
FROM Metric SELECT sum(cqrs.requests) WHERE entity.name = 'labviromol-api'
FACET request, outcome SINCE 30 minutes ago TIMESERIES
```

**Why:** shows **which** operation is failing (not just "there was an error" — which one), with `outcome`
= `success`/`failure`. Cross-reference with the status code in NBomber's `summary.json` to confirm the
client and server see the same failure.

### A.6 — Outbox backlog (async saturation finding)

```sql
FROM Metric SELECT latest(outbox.pending) WHERE entity.name = 'labviromol-api'
SINCE 30 minutes ago TIMESERIES
```

**Why:** this is the gauge mentioned in README section 5 — if this number rises and doesn't come back
down during stress/soak, the Outbox worker isn't keeping up with the write volume generated by the test
(async bottleneck, even if the HTTP requests themselves stay fast).

### A.7 — How much it costs to process an Outbox batch (and how much fails)

```sql
FROM Metric SELECT average(outbox.batch.duration), sum(outbox.messages.processed), sum(outbox.messages.failed)
WHERE entity.name = 'labviromol-api' SINCE 30 minutes ago TIMESERIES
```

**Why:** this is the query that would have shown the SMTP problem **before** even looking at the error
log — a rising `outbox.messages.failed` is the aggregate symptom; A.2/A.3 give the exact reason.

### A.8 — Cost of translation (LibreTranslate ablation)

```sql
FROM Metric SELECT average(translation.duration), sum(translation.failures)
WHERE entity.name = 'labviromol-api' FACET job SINCE 30 minutes ago TIMESERIES
```

**Why:** this is the number that backs the `A` vs `A-noop-translate` comparison from step 2 — run this
query in both time windows (one per campaign) and compare.

### A.9 — Postgres connection pool health (Npgsql)

```sql
FROM Metric SELECT count(*) FACET metricName
WHERE entity.name = 'labviromol-api' AND metricName LIKE '%npgsql%' SINCE 30 minutes ago LIMIT 20
```

**Why:** this is a **discovery** query — the exact name of Npgsql metrics varies by library version, so
instead of guessing, this query lists what's actually coming in. After finding the right name (something
like `db.client.connections.usage`), swap it for a normal query like:

```sql
FROM Metric SELECT average(`db.client.connections.usage`)
WHERE entity.name = 'labviromol-api' FACET state SINCE 30 minutes ago TIMESERIES
```

**Why (this second one):** this is the metric that confirms the 20-connection pool bottleneck (README
section 5) — if `state=used` gets close to 20 during stress, it's the pool saturating, not a lack of CPU.

### A.10 — CPU, GC and memory of the .NET runtime

```sql
FROM Metric SELECT count(*) FACET metricName
WHERE entity.name = 'labviromol-api' AND metricName LIKE '%runtime.dotnet%' SINCE 30 minutes ago LIMIT 30
```

**Why:** same reasoning as A.9 — discover the exact names first (they vary by version of
`OpenTelemetry.Instrumentation.Runtime`). The ones that matter most for the soak (section 1.4) are
`gc.heap.size` (memory) and `gc.collections.count` (collection frequency) — if the heap keeps growing
without stopping over 60 minutes, that's the memory leak the soak is meant to catch.

### A.11 — Email: latency and failures (the SMTP problem, live)

```sql
FROM Metric SELECT average(email.latency), sum(email.failures)
WHERE entity.name = 'labviromol-api' SINCE 30 minutes ago TIMESERIES
```

**Why:** if you fix the SMTP block (see the diagnosis from 06/21) and want to confirm it's working again,
this is the query: `email.failures` should go to zero and `email.latency` should drop from "timeout"
(~many seconds) to normal SMTP latency (a few hundred ms).

### A.12 — Individual traces of a slow request

In the side menu, **Distributed tracing** (not NRQL, it's its own screen) → filter by `labviromol-api`
and by the test's time window → sort by duration. Click a slow trace to see the span-by-span breakdown
(how much was a Postgres query, how much was an HTTP call to LibreTranslate, etc.). Use this when A.4
shows a slow operation and you want to know **the internal part** that's taking the time.
