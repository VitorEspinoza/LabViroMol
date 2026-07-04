# Tutorial — Generating, organizing and writing about load-test reports

**English** · [Português](./REPORTING.pt-BR.md)

This document complements the [README.md](README.md) (which explains *what* each piece of the suite
does) with the step-by-step for **after the test has run**: where the numbers live, how to organize them
so you don't lose track across campaigns, and how to turn that into thesis text.

---

## 1. What each run generates

Every `--command=run` execution creates the folder:

```
tests/LoadTests/bin/Release/net10.0/reports/<campaign>/<profile>/<scenario>/
```

Inside it, two groups of artifacts:

| File | Who generates it | What's in it |
|---|---|---|
| `*.html` | NBomber (`WithReportFolder`) | Visual report with latency/RPS charts per *step*, browsable in the browser |
| `*.csv`, `*.md`, `*.txt` | NBomber | The same numbers from the html in raw/tabular format — use the `.csv` if you're going to plot your own chart |
| `summary.json` | Our `ResultExporter.cs` | Campaign/profile/scenario, **status code breakdown**, **Apdex per operation**, and p95/p99/max per scenario — it's the most useful file for building a comparative table, since it's already flattened |

The folder name (`<campaign>/<profile>/<scenario>`) comes directly from the `--campaign`, `--profile`,
`--scenario` arguments you passed on the command line. **Running again with the same three values
overwrites the previous report** — there's no automatic timestamp versioning.

---

## 2. Naming convention to avoid losing results

Since `--campaign` is just a free-form string (it doesn't have to literally be "A" or "B"), use it to
stamp any experiment variable that isn't the profile/scenario:

| Experiment | Suggested `--campaign` value |
|---|---|
| Campaign A, real translation (default) | `A` |
| Campaign A, LibreTranslate ablation | `A-noop-translate` |
| Campaign B | `B` |
| Campaign B, ablation | `B-noop-translate` |
| Rerun after fixing a found bug | `A-rerun-2026-06-25` (or any suffix that tells you "this is after fix X") |

Before starting a new campaign, copy the entire `reports/` folder out of `bin/` (which gets discarded on
every build) — see section 3.

---

## 3. Organizing artifacts as evidence (outside `bin/`)

`bin/` gets recreated on every `dotnet build`/`dotnet run`, so it's **not a place to keep evidence**.
Create a versioned evidence folder, outside the code project (so as not to bloat the repo with test
html/csv):

```
evidencias-tcc/
  capitulo-testes/
    01-smoke/
    02-load-nominal/
    03-stress/
    04-soak-60min/
    05-ablation-libretranslate/
    06-campanha-b/
```

After each run, copy the report folder into the corresponding subfolder, with a name that includes the
date and the command used, for example:

```bash
cp -r tests/LoadTests/bin/Release/net10.0/reports/A/load/mixed \
      evidencias-tcc/capitulo-testes/02-load-nominal/2026-06-21_A_load_mixed
```

Also keep, alongside it, a `comando.txt` with the exact line you ran — you'll need to reproduce this at
the thesis defense if someone asks "how did you generate this number?".

---

## 4. Building the comparative table (A × B × ablation)

Each run's `summary.json` already comes with the fields ready to go. To compare N runs, build a manual
table (or a simple script that reads the `summary.json` from each folder) with one row per
scenario/operation:

| Campaign | Scenario | p95 (ms) | p99 (ms) | Error (%) | Apdex |
|---|---|---|---|---|---|
| A | read-simple | … | … | … | … |
| A-noop-translate | read-simple | … | … | … | … |
| B | read-simple | … | … | … | … |

The delta between `A` and `A-noop-translate` on the same row is the real cost of LibreTranslate. The
delta between `A` and `B` is the cost of the container limits (384 MB / 0.5 vCPU). These two comparisons
are the chapter's most citable results — each one isolates **a single variable**, which is what gives it
the strength of a "controlled experiment" instead of "I ran it and got this number".

---

## 5. Correlating with New Relic

For each run you're going to cite in the thesis:

1. **Note down the exact start and end time** (UTC) — NBomber logs this in the `.txt`/`.html`.
2. In New Relic, go to the `cqrs.duration` dashboard, the Npgsql meter, and the runtime (CPU/GC), and
   **restrict the time window** to that exact interval.
3. Take screenshots of the charts already trimmed to that window — not of the "live" dashboard (which
   mixes in other periods). Save them alongside the evidence from section 3, in the same subfolder.
4. If the client (NBomber) recorded a 500/timeout error at time X, check whether the server shows the
   corresponding symptom (Npgsql connection queue, 100% CPU, GC looping) **at that same X** — this is the
   "proof" of cause and effect that backs the analysis section.

---

## 6. How to structure the writing (thesis testing chapter)

Suggested sections, in the order they normally appear in an experimental evaluation chapter:

### 6.1 Methodology (before showing any numbers)
- Which tool (NBomber) and why (language, integration with the project's own contracts/DTOs).
- The SLOs defined **a priori** — the T/4T table per operation type (`README.md` section 2/3). It's
  important to make clear that the threshold was decided *before* running, not adjusted afterward to
  "pass".
- The environment: VM specification, container limits for each campaign, and why Campaign A isolates only
  resources as the variable (see README section 6).
- The control variables: mocked SMTP (why it's safe to mock — a 100% external dependency) vs. real
  LibreTranslate (why it's not safe to mock — it contends for resources on the same host).

### 6.2 Execution
- List of the exact commands run (reuse the `comando.txt` files from section 3).
- Dates/times of each run.
- Any deviations from the plan (e.g. a test that had to be redone due to a bug found mid-run).

### 6.3 Results
- One table per profile (smoke/load/stress/soak), with p95/p99/error/Apdex per scenario.
- The NBomber charts (latency over time) for the longer profiles (soak, stress) — this is where memory
  leaks or degradation appear visually.
- The comparative table from section 4 (A × B × ablation).

### 6.4 Analysis / Discussion
- For each limit found, the client↔server correlation from section 5 (which resource saturated first).
- Apdex per load range (nominal/tolerated/breaking) — this is the "verdict" on quality of service.
- Explicit comparison against the SLOs defined in the methodology: passed, partially passed, or failed —
  and why.

### 6.5 Limitations
- The test runs outside the real server (external agent) but still on the same network/region — state
  this.
- No real simultaneous users validating behavior (it's synthetic load).
- Reduced trace sampling (0.1) during the test — some individual traces weren't available for post-mortem
  inspection, only the aggregates.

### 6.6 Conclusion
- The practical reference number: "the API sustains X RPS with Apdex ≥ 0.90 under the current production
  limits (384 MB / 0.5 vCPU)".
- The identified bottleneck (RAM/GC, pool, or nginx CPU) and the resulting recommendation (e.g. "if real
  traffic exceeds X, the first resource to scale up is Y").

---

## 7. Checklist before considering a result "closed"

- [ ] The exact command was saved (`comando.txt`) alongside the evidence.
- [ ] `summary.json` confirms whether it passed or failed the thresholds defined a priori.
- [ ] New Relic screenshot trimmed to the exact time window of the run.
- [ ] If it's a result cited in a comparative table (A×B or ablation), the other end of the comparison has
      already been run and is in the same folder structure.
- [ ] The soak run (if applicable) doesn't show monotonic growth of latency/heap in the NBomber chart —
      or, if it does, this was noted as a finding (not hidden).
