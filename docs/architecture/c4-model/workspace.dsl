workspace "LabViroMol" "Sistema de gestão de laboratório de virologia: controle de estoque, agendamento de uso, gestão de pesquisa e equipamentos." {

    model {
        // Pessoas (Nível 1)
        admin = person "Administrador do Laboratório" "Usuário autenticado via JWT, acessa o painel Angular, gerencia estoque/agendamentos/equipamentos/pesquisa/usuários conforme permissões."
        visitante = person "Estudante Externo / Visitante" "Usuário anônimo, acessa o site institucional Next.js, pode solicitar agendamento de uso do laboratório (rate-limited)."

        // Sistema externo (Nível 1 / Nível 2) — só a Brevo é de fato externa
        // (third-party, fora do nosso deploy). LibreTranslate é self-hosted via Docker
        // no mesmo docker-compose, por isso é modelado como Container (ver dentro de
        // labviromol abaixo), nunca como softwareSystem externo.
        brevo = softwareSystem "Brevo" "Envio de e-mails transacionais (recuperação de senha, confirmações de agendamento) via API HTTP." "External"

        // Sistema principal
        labviromol = softwareSystem "LabViroMol" "Sistema de gestão de laboratório de virologia: controle de estoque, agendamento de uso, gestão de pesquisa e equipamentos." {

            gateway = container "Gateway" "nginx (Alpine)" "Reverse proxy / roteador único de entrada na porta 80"
            admin_panel = container "Painel Administrativo" "Angular 21 SPA (servido por nginx interno)" "Painel administrativo autenticado"
            institucional = container "Site Institucional" "Next.js 16 (Node standalone)" "Site institucional público"
            postgres = container "Banco de Dados" "PostgreSQL 17" "Armazenamento relacional multi-schema (1 schema por módulo)" "Database"
            libretranslate = container "Tradução" "LibreTranslate (Docker)" "Serviço de tradução self-hosted"

            api = container "API" "ASP.NET Core 10 Minimal API" "Orquestra os 6 módulos de negócio via CQRS/Mediator" {
                authComponent = component "Autenticação & Autorização" "ASP.NET Core Identity + JWT Bearer" "Emissão/validação de token, checagem de permissão por endpoint"
                mediatorPipeline = component "Mediator Pipeline" "Mediator (source-gen)" "Roteamento de Commands/Queries, ValidationBehavior (FluentValidation)"
                sharedKernel = component "Shared Kernel" "Classes base .NET" "Primitivas comuns: AggregateRoot, StrongId, SmartEnum, Permissions"

                identityModule = component "Módulo Identity" "C# / Clean Architecture" "Autenticação JWT, usuários, roles, permissões"
                researchModule = component "Módulo Research" "C# / Clean Architecture" "Projetos, pesquisadores, publicações, parceiros"
                inventoryModule = component "Módulo Inventory" "C# / Clean Architecture" "Materiais, estoque, kits, pedidos de compra"
                schedulingModule = component "Módulo Scheduling" "C# / Clean Architecture" "Agendamento de uso do laboratório"
                assetsModule = component "Módulo Assets" "C# / Clean Architecture" "Equipamentos, manutenção"
                notifyModule = component "Módulo Notify" "C# / Clean Architecture" "Notificações in-app e e-mail"
            }
        }

        // Relações — Nível 1 (Pessoa -> Sistema, Sistema -> Sistema Externo)
        admin -> labviromol "Gerencia estoque, agendamentos, equipamentos, pesquisa e usuários" "HTTPS/JSON"
        visitante -> labviromol "Consulta informações públicas e solicita agendamento" "HTTPS/JSON"
        labviromol -> brevo "Envia e-mails transacionais" "HTTPS/REST"

        // Relações — Nível 2 (Pessoa -> Container, roteamento do Gateway, Container -> Container/Sistema Externo)
        admin -> gateway "Acessa painel administrativo" "HTTPS"
        visitante -> gateway "Acessa site institucional" "HTTPS"

        gateway -> admin_panel "Roteia requisições" "/gestao-lab-ufpr/"
        gateway -> institucional "Roteia requisições" "/ (default)"
        gateway -> api "Roteia requisições" "/api/ e /images/"

        admin_panel -> api "Consome API REST" "HTTPS/JSON, JWT Bearer"
        institucional -> api "Consome API REST" "HTTPS/JSON"

        api -> postgres "Lê/escreve dados" "EF Core/TCP"
        api -> libretranslate "Traduz conteúdo" "HTTP"
        api -> brevo "Envia e-mail" "HTTPS/REST"

        // Relações — Nível 3 (intra-API entre componentes)
        authComponent -> identityModule "Autentica/autoriza (antes do despacho)"
        authComponent -> researchModule "Autentica/autoriza (antes do despacho)"
        authComponent -> inventoryModule "Autentica/autoriza (antes do despacho)"
        authComponent -> schedulingModule "Autentica/autoriza (antes do despacho)"
        authComponent -> assetsModule "Autentica/autoriza (antes do despacho)"
        authComponent -> notifyModule "Autentica/autoriza (antes do despacho)"

        identityModule -> mediatorPipeline "Despacha Commands/Queries"
        researchModule -> mediatorPipeline "Despacha Commands/Queries"
        inventoryModule -> mediatorPipeline "Despacha Commands/Queries"
        schedulingModule -> mediatorPipeline "Despacha Commands/Queries"
        assetsModule -> mediatorPipeline "Despacha Commands/Queries"
        notifyModule -> mediatorPipeline "Despacha Commands/Queries"

        identityModule -> sharedKernel "Herda primitivas"
        researchModule -> sharedKernel "Herda primitivas"
        inventoryModule -> sharedKernel "Herda primitivas"
        schedulingModule -> sharedKernel "Herda primitivas"
        assetsModule -> sharedKernel "Herda primitivas"
        notifyModule -> sharedKernel "Herda primitivas"

        inventoryModule -> researchModule "Consulta elegibilidade de projeto via Contract"
        inventoryModule -> notifyModule "Dispara notificação/e-mail via Domain Event"
        schedulingModule -> notifyModule "Dispara notificação/e-mail via Domain Event"
    }

    views {
        systemContext labviromol "C4-Nivel-1-Contexto" {
            include *
            autoLayout
            description "Visão de mais alto nível: LabViroMol como caixa única, seus usuários humanos (Administrador, Visitante) e o único sistema verdadeiramente externo (Brevo — LibreTranslate é self-hosted, por isso só aparece no Nível 2 como Container)."
        }

        container labviromol "C4-Nivel-2-Containers" {
            include *
            autoLayout
            description "Blocos de execução independentes do LabViroMol (Gateway, Painel Administrativo, Site Institucional, API, Banco de Dados, Tradução) e como se comunicam entre si e com a Brevo."
        }

        component api "C4-Nivel-3-Componentes" {
            include *
            autoLayout
            description "Componentes internos do container API: os 6 módulos de negócio, Shared Kernel, Mediator Pipeline e Autenticação & Autorização."
        }

        styles {
            element "Person" {
                shape person
                background #08427b
                color #ffffff
            }
            element "Software System" {
                background #1168bd
                color #ffffff
            }
            element "External" {
                background #999999
                color #ffffff
            }
            element "Container" {
                background #438dd5
                color #ffffff
            }
            element "Database" {
                shape cylinder
            }
            element "Component" {
                background #85bbf0
                color #000000
            }
        }
    }

}

/*
 * Como renderizar este modelo:
 *
 * Opção 1 — Structurizr Lite (recomendado para visualização interativa local):
 *   docker run -p 8080:8080 -v ./docs/architecture/c4-model:/usr/local/structurizr structurizr/lite
 *   Depois abrir http://localhost:8080 no navegador.
 *
 * Opção 2 — structurizr-cli (para gerar uma imagem/diagrama exportado a partir deste .dsl):
 *   structurizr-cli export -workspace workspace.dsl -format mermaid
 *   structurizr-cli export -workspace workspace.dsl -format plantuml
 *
 * Este arquivo é a FONTE DE VERDADE do C4 Model do LabViroMol. Os arquivos
 * docs/architecture/c4-model/c4-context.md, c4-container.md e c4-component.md contêm
 * apenas recortes legíveis e explicações de cada nível — não duplicam o modelo completo.
 */
