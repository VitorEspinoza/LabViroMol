# C4 Model — Nível 2: Containers — LabViroMol

[English](./c4-container.md) · **Português**

Este nível detalha o sistema LabViroMol em seus blocos de execução independentes (aplicações, serviços e armazenamento de dados), mostrando como eles se comunicam entre si e por onde o tráfego entra. É útil para quem precisa entender a topologia de deploy e as fronteiras de responsabilidade entre o gateway, a API, os frontends e a infraestrutura de dados — por exemplo, quem cuida de infraestrutura e novos desenvolvedores que vão integrar um novo container.

> **Fonte de verdade**: o modelo completo está definido em um único arquivo Structurizr DSL: [`workspace.dsl`](./workspace.dsl). Este documento mostra apenas o recorte relevante ao Nível 2 (Containers) — a view `container` desse workspace.

## Recorte do modelo (Nível 2)

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

## View correspondente

```dsl
views {
    container labviromol "C4-Nivel-2-Containers" {
        include *
        autoLayout
        description "Blocos de execução independentes do LabViroMol (Gateway, Painel Administrativo, Site Institucional, API, Banco de Dados, Tradução) e como se comunicam entre si e com o Gmail SMTP."
    }
}
```

## Elementos e relações deste nível

- **6 Container**: Gateway (nginx), Painel Administrativo (Angular 21 SPA), Site Institucional (Next.js 16), API (ASP.NET Core 10 Minimal API), Banco de Dados (PostgreSQL 17), Tradução (LibreTranslate)
- **1 System_Ext**: Gmail SMTP
- **10 Rel**: 2 Pessoa→Gateway, 3 roteamento do Gateway, 2 Frontend→API, 3 API→infra (Postgres/LibreTranslate/SMTP)

## Como renderizar

Não há ambiente de validação/renderização Structurizr disponível neste projeto — a checagem feita foi manual (balanceamento de chaves e consistência de identificadores entre `model` e `views` no workspace completo). Para gerar a visualização real:

- **Structurizr Lite** (interativo, local):
  ```
  docker run -p 8080:8080 -v ./docs/architecture/c4-model:/usr/local/structurizr structurizr/lite
  ```
  Depois abrir `http://localhost:8080`.

- **structurizr-cli** (exportar para imagem/outra notação):
  ```
  structurizr-cli export -workspace workspace.dsl -format mermaid
  structurizr-cli export -workspace workspace.dsl -format plantuml
  ```
