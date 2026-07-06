# Referência de API

[English](./api.md) · **Português**

A API é documentada de forma interativa via Scalar (OpenAPI). Em desenvolvimento, acesse `/scalar/v1` após iniciar o servidor.

## URL base

```
https://localhost:<port>/api
```

## Autenticação

Todos os endpoints protegidos exigem um token JWT Bearer no header `Authorization`:

```
Authorization: Bearer <token>
```

Obtenha um token via `POST /api/identity/users/login`.

---

## Grupos de endpoints

### Identity — `/api/identity`

| Método | Path | Descrição | Auth |
|--------|------|-------------|------|
| POST | `/users` | Registra um novo usuário | Obrigatório |
| POST | `/users/login` | Autentica e recebe um JWT | Público |
| POST | `/users/forgot-password` | Envia e-mail de reset de senha | Público |
| DELETE | `/users/{id}` | Desativa (soft-delete) um usuário | Obrigatório |
| GET | `/roles` | Lista roles | Obrigatório |
| POST | `/roles` | Cria uma role | Obrigatório |
| PUT | `/roles/{id}` | Atualiza as permissões de uma role | Obrigatório |

---

### Inventory — `/api/inventory`

| Método | Path | Descrição | Auth |
|--------|------|-------------|------|
| GET | `/materials` | Lista paginada de materiais | Obrigatório |
| POST | `/materials` | Cria material | Obrigatório |
| PUT | `/materials/{id}` | Atualiza material | Obrigatório |
| DELETE | `/materials/{id}` | Remove material | Obrigatório |
| GET | `/material-types` | Lista tipos de material | Obrigatório |
| POST | `/material-types` | Cria tipo | Obrigatório |
| GET | `/kits` | Lista paginada de kits | Obrigatório |
| POST | `/kits` | Cria kit | Obrigatório |
| PUT | `/kits/{id}` | Atualiza kit | Obrigatório |
| DELETE | `/kits/{id}` | Remove kit | Obrigatório |
| GET | `/orders` | Lista pedidos de compra | Obrigatório |
| POST | `/orders` | Cria pedido | Obrigatório |
| GET | `/reports/stock-outflows/by-project.pdf` | Relatório PDF: saídas de material por projeto | Obrigatório |
| GET | `/reports/stock-outflows/by-month.pdf` | Relatório PDF: saídas de material por mês | Obrigatório |
| GET | `/reports/stock-outflows/totals.pdf` | Relatório PDF: total de saídas de material | Obrigatório |
| GET | `/reports/stock-inflows/by-order-material-month.pdf` | Relatório PDF: entradas de material por pedido/material/mês | Obrigatório |
| GET | `/reports/critical-stock-balance.pdf` | Relatório PDF: estoque atual vs. estoque mínimo | Obrigatório |
| GET | `/reports/material-audit-movements.pdf` | Relatório PDF: movimentações auditáveis de material | Obrigatório |
| GET | `/reports/manual-stock-adjustments.pdf` | Relatório PDF: ajustes manuais de estoque | Obrigatório |

Os endpoints de relatório do Inventory retornam respostas binárias `application/pdf` geradas com QuestPDF.
Exigem um usuário autenticado com `Inventory.Stock.View` ou `Inventory.Stock.Manage`.

Filtros de query comuns:

- `from`: limite inferior obrigatório (data/hora UTC) para relatórios de transação.
- `to`: limite superior obrigatório (data/hora UTC) para relatórios de transação.
- `materialId`: id de material opcional.
- `materialTypeId`: id de tipo de material opcional.
- `projectId`: id de projeto opcional para `/reports/stock-outflows/by-project.pdf`.

Relatórios de transação exigem `from` e `to`, e o intervalo aceito é limitado a 366 dias. O relatório de estoque crítico é baseado no estado atual dos materiais e não exige intervalo de datas.

Filtros específicos:

- `onlyCritical`: booleano opcional para `/reports/critical-stock-balance.pdf`; default é `true`.
- `transactionType`: tipo de transação opcional para `/reports/material-audit-movements.pdf`.
- `limit`: limite opcional de linhas para `/reports/material-audit-movements.pdf`; limitado pelo backend.

Repasse ao frontend: chame esses endpoints como downloads de arquivo/requisições blob. Não faça parse de JSON em respostas de sucesso. Falhas de validação e autorização continuam usando o comportamento/status codes de erro padrão da API.

---

### Assets — `/api/assets`

| Método | Path | Descrição | Auth |
|--------|------|-------------|------|
| GET | `/equipments` | Lista paginada de equipamentos | Obrigatório |
| POST | `/equipments` | Cria equipamento (com imagem) | Obrigatório |
| PUT | `/equipments/{id}` | Atualiza equipamento | Obrigatório |
| DELETE | `/equipments/{id}` | Remove equipamento | Obrigatório |
| GET | `/public/equipments` | Listagem pública de equipamentos | Público |
| GET | `/maintenance-requests` | Lista solicitações de manutenção | Obrigatório |
| POST | `/maintenance-requests` | Cria solicitação | Obrigatório |
| PUT | `/maintenance-requests/{id}` | Atualiza status da solicitação | Obrigatório |

---

### Research — `/api/research`

| Método | Path | Descrição | Auth |
|--------|------|-------------|------|
| GET/POST/PUT/DELETE | `/projects` | CRUD de projetos de pesquisa | Obrigatório |
| GET/POST/PUT/DELETE | `/researchers` | CRUD de pesquisadores | Obrigatório |
| GET/POST/PUT/DELETE | `/partners` | CRUD de organizações parceiras | Obrigatório |
| GET/POST/PUT/DELETE | `/positions` | CRUD de cargos | Obrigatório |
| GET/POST/PUT/DELETE | `/publications` | CRUD de publicações | Obrigatório |
| GET | `/public/*` | Dados institucionais públicos | Público |

---

### Scheduling — `/api/scheduling`

| Método | Path | Descrição | Auth |
|--------|------|-------------|------|
| GET | `/schedules` | Lista agendamentos | Obrigatório |
| POST | `/schedules` | Cria agendamento | Obrigatório |
| GET | `/public/schedules` | Listagem pública de agendamentos (rate-limited) | Público |

---

### Notify — `/api/notify`

| Método | Path | Descrição | Auth |
|--------|------|-------------|------|
| GET | `/notifications` | Lista notificações do usuário | Obrigatório |
| POST | `/notifications/{id}/dismiss` | Dispensa uma notificação | Obrigatório |
| POST | `/notifications/dismiss-all` | Dispensa todas as notificações | Obrigatório |
| POST | `/notifications/dismiss-batch` | Dispensa um conjunto de notificações | Obrigatório |

---

### Admin BFF - `/api/admin`

Endpoints de read-model sob medida para o painel administrativo Angular.

| Método | Path | Descrição | Auth |
|--------|------|-------------|------|
| GET | `/dashboard/summary` | Contadores do dashboard e pequenas listas de preview | Obrigatório |

`GET /api/admin/dashboard/summary` exige um usuário autenticado. Cada seção da resposta só é retornada quando o usuário tem a permissão de módulo correspondente:

- `scheduling`: `Scheduling.Schedules.View` ou `Scheduling.Schedules.Manage`
- `inventory`: `Inventory.Materials.View` ou `Inventory.Materials.Manage`
- `assets`: `Assets.Maintenance.View` ou `Assets.Maintenance.Manage`

Se o usuário não tiver nenhuma permissão relevante ao dashboard, o endpoint retorna `403 Forbidden`. Se o usuário tiver permissões parciais, as seções não autorizadas ficam `null` e suas queries não são executadas.

Exemplo de resposta:

```json
{
  "scheduling": {
    "pendingSchedulesCount": 3,
    "approvedSchedulesThisMonthCount": 12,
    "upcomingSchedules": [
      {
        "id": "00000000-0000-0000-0000-000000000000",
        "schedulerName": "Joao da Silva",
        "date": "2026-06-25",
        "startDateHour": "2026-06-25T14:00:00Z",
        "equipmentNames": ["Microscopio Optico"],
        "status": "SCHEDULED"
      }
    ]
  },
  "inventory": {
    "lowStockMaterialsCount": 5,
    "lowStockMaterials": [
      {
        "id": "00000000-0000-0000-0000-000000000000",
        "name": "Alcool 70%",
        "location": "Armario 3",
        "stockQuantity": 1,
        "minStock": 5,
        "unit": "Milliliter"
      }
    ]
  },
  "assets": {
    "activeMaintenanceRequestsCount": 4
  },
  "generatedAt": "2026-06-20T18:00:00Z"
}
```

Repasse ao frontend: substitua as múltiplas chamadas do dashboard aos endpoints de scheduling, materiais e manutenção por este endpoint único. Mantenha as guardas de UI por permissão; use as seções `null` como fonte de verdade do backend para dados indisponíveis.

---

## Respostas de erro

Todos os erros seguem o formato ProblemDetails (RFC 9457):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed",
  "status": 422,
  "errors": {
    "Name": ["'Name' must not be empty."]
  }
}
```

Status codes comuns:

| Status | Significado |
|--------|---------|
| 400 | Requisição inválida / erro de validação |
| 401 | JWT ausente ou inválido |
| 403 | Permissões insuficientes |
| 404 | Recurso não encontrado |
| 422 | Violação de regra de domínio |
| 429 | Limite de taxa excedido |
| 500 | Erro inesperado do servidor |

## Arquivos estáticos

Imagens enviadas são servidas em:

```
/images/equipments/<filename>
/images/schedule-terms/<filename>
```
