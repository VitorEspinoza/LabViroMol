# C4 Model — Level 2: Containers — LabViroMol

**English** · [Português](./c4-container.pt-BR.md)

This level details the LabViroMol system in terms of its independent execution units (applications, services and data storage), showing how they communicate with each other and where traffic enters the system. It is useful for anyone who needs to understand the deployment topology and the responsibility boundaries between the gateway, the API, the frontends and the data infrastructure — for example, infrastructure staff and new developers integrating a new container.

> **Source of truth**: the full model is defined in a single Structurizr DSL file: [`workspace.dsl`](./workspace.dsl). This document shows only the excerpt relevant to Level 2 (Containers) — the `container` view of that workspace.

## Model excerpt (Level 2)

```dsl
model {
    admin = person "Administrador do Laboratório" "Usuário autenticado via JWT"
    visitante = person "Estudante Externo / Visitante" "Usuário anônimo"

    smtp = softwareSystem "Gmail SMTP" "Envio de e-mails transacionais" "External"

    labviromol = softwareSystem "LabViroMol" "..." {
        gateway       = container "Gateway" "nginx (Alpine)" "Reverse proxy / roteador único de entrada na porta 80"
        admin_panel   = container "Painel Administrativo" "Angular 21 SPA (servido por nginx interno)" "Painel administrativo autenticado"
        institucional = container "Site Institucional" "Next.js 16 (Node standalone)" "Site institucional público"
        api           = container "API" "ASP.NET Core 10 Minimal API" "Orquestra os 6 módulos de negócio via CQRS/Mediator"
        postgres      = container "Banco de Dados" "PostgreSQL 17" "Armazenamento relacional multi-schema (1 schema por módulo)" "Database"
        libretranslate = container "Tradução" "LibreTranslate (Docker)" "Serviço de tradução self-hosted"
    }

    admin -> gateway "Acessa painel administrativo" "HTTPS"
    visitante -> gateway "Acessa site institucional" "HTTPS"

    gateway -> admin_panel "Roteia requisições" "/gestao-lab-ufpr/"
    gateway -> institucional "Roteia requisições" "/ (default)"
    gateway -> api "Roteia requisições" "/api/ e /images/"

    admin_panel -> api "Consome API REST" "HTTPS/JSON, JWT Bearer"
    institucional -> api "Consome API REST" "HTTPS/JSON"

    api -> postgres "Lê/escreve dados" "EF Core/TCP"
    api -> libretranslate "Traduz conteúdo" "HTTP"
    api -> smtp "Envia e-mail" "SMTP/TLS"
}
```

## Corresponding view

```dsl
views {
    container labviromol "C4-Nivel-2-Containers" {
        include *
        autoLayout
        description "Blocos de execução independentes do LabViroMol (Gateway, Painel Administrativo, Site Institucional, API, Banco de Dados, Tradução) e como se comunicam entre si e com o Gmail SMTP."
    }
}
```

## Elements and relationships in this level

- **6 Container**: Gateway (nginx), Admin Panel (Angular 21 SPA), Institutional Site (Next.js 16), API (ASP.NET Core 10 Minimal API), Database (PostgreSQL 17), Translation (LibreTranslate)
- **1 System_Ext**: Gmail SMTP
- **10 Rel**: 2 Person→Gateway, 3 Gateway routing relations, 2 Frontend→API, 3 API→infra (Postgres/LibreTranslate/SMTP)

## How to render

There is no Structurizr validation/rendering environment available in this project — the check performed was manual (brace balancing and identifier consistency between `model` and `views` in the full workspace). To generate the actual visualization:

- **Structurizr Lite** (interactive, local):
  ```
  docker run -p 8080:8080 -v ./docs/architecture/c4-model:/usr/local/structurizr structurizr/lite
  ```
  Then open `http://localhost:8080`.

- **structurizr-cli** (export to image/other notation):
  ```
  structurizr-cli export -workspace workspace.dsl -format mermaid
  structurizr-cli export -workspace workspace.dsl -format plantuml
  ```
