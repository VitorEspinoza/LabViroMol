# Diagrama de Implantação (Deployment) — LabViroMol

[English](./deployment.md) · **Português**

Este diagrama mostra a topologia física/de infraestrutura real em produção do LabViroMol: um único nó (droplet DigitalOcean) executando os 6 containers Docker definidos em `docker-compose.yaml`, os 3 volumes persistentes, e a única porta pública exposta ao mundo externo. Ele complementa o C4 Nível 2 (Container, ver `docs/architecture/c4-model/c4-container.md`) com a perspectiva de **onde isso roda**, não de como os componentes se comunicam logicamente.

Optou-se pela notação `flowchart TB` estilizada (em vez de `C4Deployment` nativo) para priorizar fidelidade de renderização: blocos C4 dedicados a deployment são pouco usados e têm suporte menos consistente entre ferramentas Mermaid, enquanto `flowchart` com `subgraph` é amplamente suportado e permite representar nó físico, containers e volumes com o mesmo nível de detalhe.

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

**Notas sobre a estratégia de deploy:**

- As imagens de `api`, `admin` e `institucional` são publicadas no GHCR (GitHub Container Registry) por pipeline CI; a atualização em produção é feita manualmente no droplet via `docker compose pull && docker compose up -d`.
- `postgres` e `libretranslate` não são expostos publicamente: toda comunicação entre eles e os demais containers ocorre na rede Docker interna padrão do compose (rede `default`). O `docker-compose.yaml` atual ainda publica `5432:5432` e `5000:5000` no host por conveniência operacional (acesso administrativo via SSH tunnel/firewall), mas nenhuma dessas portas é acessível pela Internet — apenas a porta `80` do `gateway` é exposta publicamente.
- `gateway` depende de `api`, `admin` e `institucional` estarem disponíveis antes de iniciar o roteamento; `api` depende do healthcheck `pg_isready` do `postgres`.
