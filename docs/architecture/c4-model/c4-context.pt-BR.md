# C4 Model — Nível 1: Contexto — LabViroMol

[English](./c4-context.md) · **Português**

Este nível mostra o sistema LabViroMol como uma caixa única, seus usuários humanos e os sistemas externos com os quais ele se integra. É o diagrama de mais alto nível do C4 Model e serve para dar contexto rápido a qualquer pessoa nova no projeto — incluindo stakeholders não técnicos — sobre quem usa o sistema e com o que ele se comunica fora de suas próprias fronteiras.

> **Fonte de verdade**: o modelo completo (Pessoas, Sistema, Containers, Componentes e todas as relações) está definido em um único arquivo Structurizr DSL: [`workspace.dsl`](./workspace.dsl). Este documento mostra apenas o recorte relevante ao Nível 1 (Contexto) — a view `systemContext` desse workspace.

## Recorte do modelo (Nível 1)

```dsl
model {
    admin = person "Administrador do Laboratório" "Usuário autenticado via JWT, acessa o painel Angular, gerencia estoque/agendamentos/equipamentos/pesquisa/usuários conforme permissões."
    visitante = person "Estudante Externo / Visitante" "Usuário anônimo, acessa o site institucional Next.js, pode solicitar agendamento de uso do laboratório (rate-limited)."

    brevo = softwareSystem "Brevo" "Envio de e-mails transacionais (recuperação de senha, confirmações de agendamento) via API HTTP." "External"

    labviromol = softwareSystem "LabViroMol" "Sistema de gestão de laboratório de virologia: controle de estoque, agendamento de uso, gestão de pesquisa e equipamentos."

    admin -> labviromol "Gerencia estoque, agendamentos, equipamentos, pesquisa e usuários" "HTTPS/JSON"
    visitante -> labviromol "Consulta informações públicas e solicita agendamento" "HTTPS/JSON"
    labviromol -> brevo "Envia e-mails transacionais" "HTTPS/REST"
}
```

## View correspondente

```dsl
views {
    systemContext labviromol "C4-Nivel-1-Contexto" {
        include *
        autoLayout
        description "Visão de mais alto nível: LabViroMol como caixa única, seus usuários humanos (Administrador, Visitante) e o único sistema verdadeiramente externo (Brevo — LibreTranslate é self-hosted, por isso só aparece no Nível 2 como Container)."
    }
}
```

## Elementos e relações deste nível

- **2 Person**: Administrador do Laboratório, Estudante Externo / Visitante
- **1 System** (LabViroMol) + **1 System_Ext** (Brevo)
- **3 Rel**: Admin→Sistema, Visitante→Sistema, Sistema→Brevo

**Nota de modelagem**: LibreTranslate **não** aparece neste nível porque é self-hosted (container Docker no mesmo `docker-compose.yaml` do LabViroMol, sem dependência de um provedor terceiro) — só Brevo é genuinamente externa ao nosso deploy. LibreTranslate aparece corretamente como **Container** no [Nível 2](./c4-container.pt-BR.md), dentro da fronteira do próprio sistema.

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
