# Deployment Diagram — LabViroMol

**English** · [Português](./deployment.pt-BR.md)

This diagram shows the actual physical/infrastructure topology of LabViroMol in production: a single node (DigitalOcean droplet) running the 6 Docker containers defined in `docker-compose.yaml`, the 3 persistent volumes, and the single public port exposed to the outside world. It complements C4 Level 2 (Container, see `docs/architecture/c4-model/c4-container.md`) with the perspective of **where this runs**, not how the components communicate logically.

The styled `flowchart TB` notation was chosen (instead of native `C4Deployment`) to prioritize rendering fidelity: dedicated C4 deployment blocks are rarely used and have less consistent support across Mermaid tools, while `flowchart` with `subgraph` is broadly supported and lets us represent physical node, containers and volumes with the same level of detail.

```mermaid
flowchart TB
    internet(["Internet / Cliente HTTP"])
    brevo[["Brevo API — HTTPS/443"]]

    subgraph droplet["Droplet DigitalOcean — 142.93.14.97 (Ubuntu + Docker Engine/Compose)"]
        direction TB

        gateway["gateway\nnginx:alpine\nporta 80"]
        api["api\nghcr.io/vitorespinoza/labviromol-api:latest\nporta 8080"]
        admin["admin\nghcr.io/vitorespinoza/labviromol-admin:latest\nporta 80"]
        institucional["institucional\nghcr.io/vitorespinoza/labviromol-institucional:latest\nporta 3000"]
        postgres["postgres\npostgres:17\nporta 5432"]
        libretranslate["libretranslate\nlibretranslate/libretranslate:latest\nporta 5000"]

        vol_postgres[("postgres_data\n/var/lib/postgresql/data")]
        vol_uploads[("uploads_images\n/app/Upload/Images")]
        vol_lt[("libretranslate_data\n/home/libretranslate/.local")]

        gateway -- "/api/, /images/ -> api:8080" --> api
        gateway -- "/gestao-lab-ufpr/ -> admin:80" --> admin
        gateway -- "/ -> institucional:3000" --> institucional

        api -- "depends_on (healthcheck pg_isready)" --> postgres

        postgres -. mount .-> vol_postgres
        api -. mount .-> vol_uploads
        libretranslate -. mount .-> vol_lt
    end

    internet -- "porta pública 80" --> gateway
    api -- "envio de e-mail" --> brevo

    classDef container fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
    classDef volume fill:#fef3c7,stroke:#b45309,color:#78350f;
    classDef external fill:#f3f4f6,stroke:#6b7280,color:#111827;

    class gateway,api,admin,institucional,postgres,libretranslate container;
    class vol_postgres,vol_uploads,vol_lt volume;
    class internet,brevo external;
```

**Notes on the deployment strategy:**

- The `api`, `admin` and `institucional` images are published to GHCR (GitHub Container Registry) by the CI pipeline; the production update is done manually on the droplet via `docker compose pull && docker compose up -d`.
- `postgres` and `libretranslate` are not publicly exposed: all communication between them and the other containers happens on the compose's standard internal Docker network (the `default` network). The current `docker-compose.yaml` still publishes `5432:5432` and `5000:5000` on the host for operational convenience (administrative access via SSH tunnel/firewall), but none of these ports is reachable from the Internet — only port `80` on the `gateway` is publicly exposed.
- `gateway` depends on `api`, `admin` and `institucional` being available before it starts routing; `api` depends on the `pg_isready` healthcheck of `postgres`.
