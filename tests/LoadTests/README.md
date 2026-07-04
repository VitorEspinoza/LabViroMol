# Performance & Resilience Testing — Guide for Newcomers

**English** · [Português](./README.pt-BR.md)

This document explains **what** each piece of this suite is, **why** it was built this way, and
**what the overall goal is**. It's written for someone who has never touched load testing before. Read it
from the start: the concepts in section 2 are used throughout the rest of the document.

---

## 1. Overall goal

We want to answer, with numbers rather than guesswork, three questions about the LabViroMol API:

1. **Does it handle expected everyday usage?** (nominal load — does it meet the SLO?)
2. **When it breaks, at what point does it break?** (saturation point — what's the breaking RPS?)
3. **Does it degrade on its own over time?** (memory leak / GC over 1h — soak)

And, most importantly: **when it breaks, we want to know WHO broke first** — CPU? database? memory?
That's why the suite was designed alongside the observability stack (OpenTelemetry → New Relic) already
present in the project. The load test is the "outside view" (requests per second, latency); telemetry is
the "inside view" (CPU, connection pool, GC). By cross-referencing the two, we find the bottleneck.

Tool chosen: **NBomber** (.NET library). Reason: it's the same stack as the project, so we reuse the
DTOs, endpoint contracts and data generators (Bogus) already used in our tests.

---

## 2. Concepts you need to know

| Term | What it is | Why it matters here |
|---|---|---|
| **SLO** | *Service Level Objective* — the quality target ("p95 ≤ 50 ms") | It's the pass/fail criterion |
| **Threshold (T)** | The acceptable latency limit for an operation | Write = 200 ms, simple read = 50 ms, etc. |
| **4T** | Four times T — the "tolerable zone" | Above this, the user gets frustrated |
| **Percentile (p95/p99)** | "95% of requests were faster than X" | The average hides outliers; percentiles don't. p95 is the SLO; p99 measures *jitter* (instability) |
| **Throughput / RPS** | Sustained requests per second | Measures capacity |
| **Apdex** | 0–1 index summarizing satisfaction: (satisfied + tolerated/2) / total | A single number to say "is it good?" (≥ 0.90 nominal) |
| **Warm-up** | Initial period discarded from measurement | .NET compiles code on demand (JIT) and fills the connection pool in the first seconds; measuring this would skew the results |
| **Closed model** | N "virtual users" who wait for a response before sending the next request | Good for simulating stable real-world usage |
| **Open model** | Injects X req/s **regardless** of whether the API is slow | **Mandatory for stress**: if the API stalls, we keep hitting it — that's how you find the breaking point |
| **Coordinated omission** | Classic mistake: in the closed model, if the API stalls, you send fewer requests and "don't see" the slowness | The reason we use the open model for stress |

### Test types (profiles)

- **smoke** — 30 s, minimal load. Just confirms "is it up and returning 2xx?" before spending more time.
- **load** — ramps up to nominal load (~10 min). Validates the everyday SLO.
- **soak** — constant nominal load for **60 min**. Hunts for *memory leaks* and Garbage Collector degradation.
- **stress** — injects increasingly more load (open model) until it degrades. Finds the breaking RPS.
- **spike** — abrupt jump in load. Tests resilience to sudden spikes.
- **breakpoint** — continuous ramp until it breaks. Finds the ceiling per operation.

---

## 3. How the pieces fit together (flow)

```
Program.cs
  │  reads command-line arguments (--scenario, --profile, --campaign, ...)
  │  reads appsettings.json  →  LoadTestConfig
  ├─ "seed" command?   →  Seeder      (populates the database)
  ├─ "reset" command?  →  Reset       (clears the database via Respawn)
  │
  ├─ HttpClientFactory →  creates ONE shared HttpClient
  ├─ AuthClient        →  logs in and holds a POOL of tokens
  ├─ LoadTestRuntime   →  loads the seed catalog + tokens; holds state and metrics
  ├─ ScenarioCatalog   →  chooses scenarios based on --scenario
  │
  └─ NBomberRunner.Run()  →  runs the scenarios
         └─ ResultExporter →  writes reports/<campaign>/<profile>/<scenario>/summary.json
```

---

## 4. The pieces, one by one

### `LoadTestConfig.cs` + `appsettings.json`

Centralizes all configuration: API URL, network tuning, login data, seed volume and the
**profiles** (smoke/load/stress/...). Each profile defines `WarmUpSeconds`, `DurationSeconds`,
`ClosedCopies` (number of users in the closed model) and `OpenRate` (req/s in the open model).

> **Why config instead of code?** So you can adjust test intensity without recompiling — you change
> the stress profile's `OpenRate` in the JSON and run again.

### `CommandLineOptions.cs`

Reads arguments in the `--key=value` format. The main ones:
`--command` (run/seed/reset), `--scenario`, `--profile`, `--campaign`, `--keepAlive`,
`--resetBeforeRun`, `--baseUrl`. This is what lets a single build run any combination.

### `HttpClientFactory.cs` — the most subtle part

Creates a **single** `HttpClient` on top of a `SocketsHttpHandler`. Important decisions:

- **`UseCookies = false`**: we don't want a shared "cookie jar". Authentication is sent via the
  `Authorization: Bearer` header (see `AuthClient`).
- **A single, reused instance**: this is what avoids **ephemeral port exhaustion**. If we created
  an `HttpClient` per request (a classic mistake), at thousands of req/s the operating system would
  run out of TCP ports (each closed connection stays in `TIME_WAIT` for ~60 s) and the **load
  generator** would fail with `SocketException` **before** the API — we'd end up measuring the wrong
  machine's limit.
- **`MaxConnectionsPerServer = 1024`**: sizes the reusable connection pool.
- **Keep-alive toggle** (`--keepAlive=false`): by default keep-alive is **on** (connection reuse). There's
  an option to disable it **only** to measure the cost of the TLS *handshake* (each request opens a new
  connection). In that variation port exhaustion becomes relevant again — hence the OS tuning at the
  campaign level (see section 6).

### `AuthClient.cs`

Logs in once per test user against `POST /api/identity/users/login` and extracts the token from the
`X-Access-Token` cookie in the response. From then on, every request uses that token via a **Bearer**
header.

> **Why Bearer instead of the cookie the API returns?** The API accepts both — the cookie is only a
> *fallback* (`OnMessageReceived` in `Identity/Infrastructure/InfrastructureModule.cs` only reads the
> cookie if the Bearer header is absent). Using Bearer lets us have **multiple users** with **a single
> `HttpClient`** (solving auth + connection reuse at the same time). The token lasts 2 h, so it covers
> even the 60-minute soak test without needing renewal.

### `Seeder.cs` + `SeedCatalog.cs`

Populates the database with **realistic volume** (thousands of records) so that paginated GETs actually
exercise indexes and pagination — testing against an empty database is worthless.

Built-in safeguards:
- **Batch writes** (`Batches = 500`) with `SaveChangesAsync()` + `ChangeTracker.Clear()` and
  `AutoDetectChangesEnabled = false`. **Why?** If we generated everything and did a single `SaveChanges`,
  EF Core would hold all tracked objects in memory — slow and can lead to out-of-memory (OOM) **in the
  seeder process itself**.
- Pre-created **stateful data**: schedules in the **pending** state (for the approval scenario),
  projects with vacancies, materials. Without this, the write scenario would have nothing to approve.
- **`SeedCatalog`** saves the generated IDs (equipment, projects, pending schedules...) to a JSON file.
  Scenarios read this catalog to know **which real IDs** to hit, without having to query the database at
  test time.

### `Reset.cs`

Uses **Respawn** to truncate the module schemas (`identity`, `inventory`, `research`, `scheduling`,
`assets`, `notify`) between runs. Since the `OutboxMessages` table lives **inside each module's schema**,
it also gets cleared — no leftover outbox messages carry over from one run to the next.

> **Warning:** Reset wipes **everything** in these schemas. Never run it against a production database
> that already has customer data.

### `LoadTestRuntime.cs` — the runtime brain

Holds shared state and metrics during the test:

- **Token pool** (`NextToken`): distributes user tokens *round-robin*.
- **Pending schedule queue** (`NextPendingScheduleIdAsync`): each approval consumes a pending ID (you
  can't approve the same one twice); when the queue empties, the runtime **replenishes** it by creating
  500 more in the database.
- **`CreateRequest`**: builds the request already with the Bearer header.
- **`SendAsync`**: sends, times, and **records two things**:
  - the **status code** per operation (`RecordStatus`) → becomes the *breakdown* in the report;
  - the **Apdex** per operation (`RecordApdex`) → classifies each response as satisfied (≤ T),
    tolerated (≤ 4T), or frustrated (> 4T).
- **`CreateLoadSimulations`**: decides the **load model**. For `stress/spike/breakpoint` it uses the
  **open** model (`RampingInject` + `Inject`); for the rest it uses the **closed** model
  (`RampingConstant` + `KeepConstant`). This is where the "open model for stress" from section 2
  actually happens.

### The scenarios (`Scenarios/`)

Each scenario is a set of requests plus their **thresholds** (SLOs). All of them start with warm-up.

| Scenario | What it does | p95 threshold | p99 ≤ |
|---|---|---|---|
| `ReadSimpleScenarios` | Paginated GETs (materials, equipments, schedules, projects) | 50 ms | 200 ms |
| `ReadComplexScenarios` | Cross-module admin dashboard | 150 ms | 600 ms |
| `InstitutionalReadScenarios` | Public institutional browsing (equipment, projects, publications, partners, researchers) | 800 ms | 2 s |
| `WriteScenarios` | Approve schedule, add/replace member, create material | 200 ms | 800 ms |
| `MixedWorkloadScenario` | Combines the ones above at a **~70/10/20** ratio (read/heavy-read/write) | per operation | per operation |
| `MixedPublicAdminScenario` | 2 flows aggregated in the same test: institutional and administrative, with separate concurrency | per operation | per operation |
| `ResilienceScenarios` | Burst on the public scheduling endpoint; **expects to receive 429** | — | — |

Each scenario has three thresholds: **error rate < 0.1%**, **p95 ≤ T**, and **p99 ≤ 4T**.

> **Why p99 ≤ 4T and not "max latency ≤ 4T"?** *Max* latency is the single worst request — a GC pause
> or the initial JIT alone can blow past this value, failing the run over pure noise. **p99** represents
> the real tail (the slowest 1%) without being dominated by a single outlier. The "tolerable zone" (4T)
> is also formally measured by Apdex; the p99 threshold is the SLO's complement.

The **weight** of the administrative mix (`WithWeight(18/5/7)`) distributes load between simple reads,
dashboard, and writes. Heavy analytical reports are not part of the interactive load suite.

For public capacity, `public-read` uses an aggregated institutional browsing flow: the `ClosedCopies`
value represents simultaneous institutional virtual users. For joint load, `mixed-public-admin` records
two flows in the same test: `InstitutionalClosedCopies` for institutional users and `AdminClosedCopies`
for administrative users. The `institutional-100`, `institutional-200`, `joint-100-20` and
`joint-150-30` profiles also define `MinThinkTimeSeconds` and `MaxThinkTimeSeconds`, modeling the
frequency of navigation between actions.

### `ResultExporter.cs`

At the end, writes a `summary.json` with: campaign/profile/scenario, configured load, duration, virtual
users, think time, **approximate RPS**, RPS by group (`Institutional`, `Admin`, `Dashboard`,
`PublicWrite`), **status code breakdown**, **Apdex per operation**, and percentiles for each scenario.
It's the input for the analysis (section 7).

---

## 5. The API side: "LoadTest mode"

The test doesn't only change the client — the **API boots into a special mode** when
`ASPNETCORE_ENVIRONMENT=LoadTest` (which loads `src/LabViroMol.Api/appsettings.LoadTest.json`). This mode:

| Adjustment | Where | Why |
|---|---|---|
| **Email becomes NoOp** | `LoadTest:UseNoOpEmail` → `NoOpEmailSender` | We don't want to fire real e-mails (Gmail) or wait on SMTP — this would distort latency and could send real e-mails |
| **Translation stays real** | `LoadTest:UseNoOpTranslator=false` → `LibreTranslator` | LibreTranslate is self-hosted (not an external dependency like SMTP) and runs asynchronously via Outbox every few seconds, independent of HTTP load. Keeping it real captures the CPU/RAM contention with the API on the same host, which is exactly what Campaign A wants to measure |
| **Postgres pool = 20** | `LoadTest:NpgsqlMaxPoolSize` → `ConnectionStringResolver` | Fixing a small pool (below Postgres's `max_connections=50`) makes **pool saturation** a clear, correlatable measurement point |
| **Trace sampling = 0.1** | `OpenTelemetry:Tracing:SamplingRatio` | Under load, recording 100% of traces is expensive and floods New Relic. Metrics stay at 100% (they're aggregated and cheap) |

> Outbox (internal async processing) **stays on** — it's part of what we want to measure. We monitor
> its backlog via the `outbox.pending` gauge.

### LibreTranslate ablation

Since the real translator stays on by default, there's an extra overlay to isolate exactly how much it
costs: `docker-compose.loadtest.noop-translate.yaml`, which overrides `LoadTest:UseNoOpTranslator` to
`true` via environment variable. Combine it with the desired campaign:

```bash
docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml -f docker-compose.loadtest.noop-translate.yaml up -d
```

Run the same profile/scenario with and without this overlay (using different `--campaign` values, see
section 7) to get the throughput/latency delta attributable solely to translation.

---

## 6. The two campaigns (A and B) — the experiment

The production API runs **constrained** in `docker-compose.yaml`: **0.5 vCPU / 384 MB**. These limits
are what makes the results **portable** (the same compose behaves the same on any host — see section 9).

To understand the impact of these limits, we run **two campaigns** that differ **only in resources**:

| Campaign | How to bring it up | API resources |
|---|---|---|
| **A — real prod** | `docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml up -d` | 0.5 vCPU / 384 MB |
| **B — VM ceiling** | `docker compose -f docker-compose.yaml -f docker-compose.loadtest.B.yaml up -d` | 2 vCPU / 4 GB |

> **Why does `loadtest.A.yaml` exist if it "doesn't change resources"?** Because it changes **only the
> environment** to `LoadTest`. Without it, Campaign A would run in `Production` (with real SMTP/
> translation, default pool, full sampling), and A and B would then differ in **several** things at
> once — the experiment would lose its meaning. With both overrides, **the only variable between A and B
> is resources**. This is what gives the comparison scientific validity (important for the thesis).

**Expectation:** in Campaign A, the first ceiling is likely **RAM/GC at 384 MB** (a .NET app with
6 DbContexts + OTel is tight against that limit). Campaign B shows what the architecture is capable
of without that constraint.

The **load agent runs from outside** (your laptop or another machine) over HTTPS against nginx — this
avoids the *noisy neighbor* effect (the generator stealing CPU from the target) and measures the real
cost of TLS. In the keep-alive-disabled variation (handshake cost), on Linux widen
`net.ipv4.ip_local_port_range` and `net.ipv4.tcp_tw_reuse` on the agent.

---

## 7. How to run

```bash
# 1. Bring up the API in LoadTest mode (Campaign A, for example)
docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml up -d

# 2. Populate the database (once)
dotnet run -c Release --project tests/LoadTests -- --command=seed

# 3. Sanity smoke (quick) before any campaign
dotnet run -c Release --project tests/LoadTests -- --scenario=read-simple --profile=smoke --campaign=A

# 4. Nominal load
dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=load --campaign=A

# 5. Isolated institutional capacity
dotnet run -c Release --project tests/LoadTests -- --scenario=public-read --profile=institutional-100 --campaign=A

# 6. Joint institutional + administrative load
dotnet run -c Release --project tests/LoadTests -- --scenario=mixed-public-admin --profile=joint-100-20 --campaign=A

# 7. Interactive administrative stress (open model, finds the breaking point)
dotnet run -c Release --project tests/LoadTests -- --scenario=read-complex --profile=stress --campaign=A

# 8. 60-min soak (memory leak / GC)
dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=soak --campaign=A

# 9. Rate limiter resilience (expects 429)
dotnet run -c Release --project tests/LoadTests -- --scenario=resilience --profile=smoke --campaign=A

# Clear the database between write runs
dotnet run -c Release --project tests/LoadTests -- --command=reset
# (or use --resetBeforeRun=true to clear+seed before running)

# 8. LibreTranslate ablation: bring up the noop-translate overlay on top of the campaign...
docker compose -f docker-compose.yaml -f docker-compose.loadtest.A.yaml -f docker-compose.loadtest.noop-translate.yaml up -d
# ...and repeat the same scenario/profile with a different --campaign, so as not to overwrite the real result
dotnet run -c Release --project tests/LoadTests -- --scenario=mixed --profile=load --campaign=A-noop-translate
```

Repeat everything swapping in `--campaign=B` (and bringing up `loadtest.B.yaml`) for the A×B comparison.

> **Campaign naming convention:** `--campaign` sets the report folder
> (`reports/<campaign>/<profile>/<scenario>/`). Running twice with the same value **overwrites** the
> previous report. For the LibreTranslate ablation (`noop-translate` overlay), use a different value,
> e.g. `--campaign=A-noop-translate`, to keep both results side by side.

---

## 8. How to read the results

1. **Did it pass the SLO?** Check whether `p95 ≤ T` and `error rate < 0.1%` for each scenario (NBomber
   marks the run as failed if any threshold is exceeded).
2. **Is it actually good?** Look at the **Apdex** per operation in `summary.json` (≥ 0.90 = healthy nominal).
3. **Who broke first?** (in stress) Cross-reference the client's **status code breakdown** with New
   Relic telemetry:

   | Client error | Likely cause | Server-side signal (New Relic) |
   |---|---|---|
   | 500 / TimeoutException | Npgsql pool (20) exhausted | active connections at the ceiling + waiting for connection |
   | 502 / 504 | nginx (0.2 vCPU) maxed out on TLS crypto | nginx CPU at 100% |
   | timeout / rising latency | *thread pool starvation* on Kestrel | thread pool queue / Kestrel active requests rising |
   | `SocketException` **on the agent** | **not the API** — the generator's port exhaustion | nothing abnormal on the server (see section 4) |
   | restart / mass 502s (Campaign A) | **OOM/GC at 384 MB** | container memory at ceiling, GC looping |

---

## 9. Where to run (single-server staging)

"Staging" is **not** a second server running 24/7 — it's an environment that **looks like** production,
brought up **when you're about to test** and torn down afterward. Since everything is `docker compose`,
staging is a `docker compose up` on a VM. And since the **compose limits** define the behavior, the same
compose runs equivalently on any host (the *absolute* RPS numbers vary with the host's core speed; the
**shape** — where it saturates, GC, pool — reproduces).

- **Heavy load** (load/stress/soak) → **isolated** environment: today, production itself *before
  launch* (with no client); later, a staging VM brought up for a few hours.
- **Live production (with clients)** → **never** heavy load. Observability (OTel → New Relic) plus a
  light *smoke* handle it. Heavy tests run in staging before each release.

---

## 10. System constants (and why)

| Constant | Value | Reason |
|---|---|---|
| API limit (prod) | 0.5 vCPU / 384 MB | This is what's set in the production `docker-compose.yaml` |
| Postgres `max_connections` | 50 | The database container's limit |
| Npgsql pool in tests | 20 | Deliberately small, so pool saturation is visible |
| Public `/scheduling` rate limit | 5/hour **global** | Not per IP/user — a single bucket for the whole API; only the resilience scenario tests it |
| Trace sampling in tests | 0.1 | Traces are expensive under load; metrics stay at 100% |
| Realistic mix | 70/10/20 | Approximates real traffic: lots of reads, a bit of heavy-read, some writes |

---

### Summed up in one sentence

This suite **bombards the API from the outside** with controlled loads, **measures latency/throughput/
errors** against clear SLOs, **isolates what could get in the way** (mocked external deps, fixed pool),
and **cross-references** everything with internal telemetry — to state, with numbers, **exactly how much
the application can handle and what gives out first**.
