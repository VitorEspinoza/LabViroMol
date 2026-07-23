# Módulos

[English](./modules.md) · **Português**

Cada módulo é uma fatia vertical autocontida da aplicação. Abaixo está a descrição da responsabilidade, entidades principais e surface de API de cada módulo.

---

## Identity

Gerencia usuários, roles e controle de acesso baseado em permissões.

**Entidades principais**: User, Role, Permission

**Features**:
- Registro de usuário, login e reset de senha (via e-mail)
- Desativação de usuário via soft-delete
- Atribuição de roles e gestão de permissões
- Emissão de token JWT

**Endpoints**: `/api/identity/*`

---

## Inventory

Gerencia materiais de laboratório, níveis de estoque, kits e pedidos de compra.

**Entidades principais**: Material, MaterialType, Kit, Order, StockTransaction

**Features**:
- CRUD de materiais com limites mínimos de estoque
- Kits (conjuntos pré-configurados de materiais com quantidades)
- Criação e processamento de pedidos de compra
- Histórico de transações de estoque
- Domain event disparado quando o estoque fica abaixo do mínimo (aciona notificação)

**Endpoints**: `/api/inventory/*`

---

## Research

Gerencia as atividades acadêmicas e de pesquisa do laboratório.

**Entidades principais**: Project, Researcher, Partner, Position, Publication

**Features**:
- Gestão do ciclo de vida de projetos de pesquisa
- Gestão de pesquisadores e parceiros externos
- Rastreamento de publicações acadêmicas
- Endpoints institucionais públicos (sem autenticação)

**Endpoints**: `/api/research/*` (algumas rotas sob `/public`)

---

## Scheduling

Gerencia agendamentos e reservas de uso do laboratório.

**Entidades principais**: Schedule

**Features**:
- Criação e gestão de agendamentos
- Listagem institucional pública de agendamentos
- Rate limiting: 5 requisições/hora nos endpoints públicos

**Endpoints**: `/api/scheduling/*` (algumas rotas sob `/public`)

---

## Assets

Gerencia o inventário de equipamentos do laboratório e solicitações de manutenção.

**Entidades principais**: Equipment, MaintenanceRequest

**Features**:
- CRUD de equipamentos com upload de imagem
- Descrições de equipamento multi-idioma via LibreTranslate
- Rastreamento de solicitações de manutenção com fluxo de status
- Listagem pública de equipamentos

**Endpoints**: `/api/assets/*` (algumas rotas sob `/public`)

---

## Notify

Trata notificações internas e e-mails de saída.

**Entidades principais**: Notification

**Features**:
- Criar, dispensar e dispensar em lote notificações
- Envio de e-mail via API HTTP da Brevo

**Endpoints**: `/api/notify/*`

---

## Shared

Infraestrutura transversal consumida por todos os outros módulos. Não é exposta diretamente via HTTP.

**Fornece**:
- Classe base `AggregateRoot<TId>` com suporte a domain events
- `BaseUnitOfWork<TContext>` com campos de auditoria e publicação de eventos
- Interfaces de entidade auditável (`ICreationAuditable`, `IModificationAuditable`, `IDeletionAuditable`)
- Padrão de value object de Strong ID (`IEntityId`)
- Utilitários de paginação (`PagedRequest`, `PagedResponse<T>`)
- Middleware `GlobalExceptionHandler` com ProblemDetails
- Constantes de permissão (classe estática `Permissions`)
- Conversores JSON e EF para `SmartEnum`
