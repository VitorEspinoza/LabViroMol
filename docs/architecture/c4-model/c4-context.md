# C4 Model — Level 1: Context — LabViroMol

**English** · [Português](./c4-context.pt-BR.md)

This level shows the LabViroMol system as a single box, its human users, and the external systems it integrates with. It is the highest-level diagram of the C4 Model and serves to give quick context to anyone new to the project — including non-technical stakeholders — about who uses the system and what it communicates with outside its own boundaries.

> **Source of truth**: the full model (People, System, Containers, Components and all relationships) is defined in a single Structurizr DSL file: [`workspace.dsl`](./workspace.dsl). This document shows only the excerpt relevant to Level 1 (Context) — the `systemContext` view of that workspace.

## Model excerpt (Level 1)

```dsl
model {
    admin = person "Administrador do Laboratório" "Usuário autenticado via JWT, acessa o painel Angular, gerencia estoque/agendamentos/equipamentos/pesquisa/usuários conforme permissões."
    visitante = person "Estudante Externo / Visitante" "Usuário anônimo, acessa o site institucional Next.js, pode solicitar agendamento de uso do laboratório (rate-limited)."

    smtp = softwareSystem "Gmail SMTP" "Envio de e-mails transacionais (recuperação de senha, confirmações de agendamento)." "External"

    labviromol = softwareSystem "LabViroMol" "Sistema de gestão de laboratório de virologia: controle de estoque, agendamento de uso, gestão de pesquisa e equipamentos."

    admin -> labviromol "Gerencia estoque, agendamentos, equipamentos, pesquisa e usuários" "HTTPS/JSON"
    visitante -> labviromol "Consulta informações públicas e solicita agendamento" "HTTPS/JSON"
    labviromol -> smtp "Envia e-mails transacionais" "SMTP/TLS"
}
```

## Corresponding view

```dsl
views {
    systemContext labviromol "C4-Nivel-1-Contexto" {
        include *
        autoLayout
        description "Visão de mais alto nível: LabViroMol como caixa única, seus usuários humanos (Administrador, Visitante) e o único sistema verdadeiramente externo (Gmail SMTP — LibreTranslate é self-hosted, por isso só aparece no Nível 2 como Container)."
    }
}
```

## Elements and relationships in this level

- **2 Person**: Lab Administrator, External Student / Visitor
- **1 System** (LabViroMol) + **1 System_Ext** (Gmail SMTP)
- **3 Rel**: Admin→System, Visitor→System, System→SMTP

**Modeling note**: LibreTranslate does **not** appear at this level because it is self-hosted (a Docker container in the same `docker-compose.yaml` as LabViroMol, with no dependency on a third-party provider) — only Gmail SMTP is genuinely external to our deployment. LibreTranslate correctly appears as a **Container** at [Level 2](./c4-container.md), within the boundary of the system itself.

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
