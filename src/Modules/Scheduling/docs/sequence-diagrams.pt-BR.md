# Diagramas de Sequência — Módulo Scheduling

[English](./sequence-diagrams.md) · **Português**

Este documento reúne os 4 diagramas de sequência específicos do módulo **Scheduling**: Solicitar Agendamento, Aprovar Agendamento, Recusar Agendamento e Cancelar Agendamento. Cobrem o ciclo de vida completo do agregado `Schedule`.

Convenções comuns aos diagramas (herdadas do documento fonte):
- `autonumber` para referenciar passos durante revisão.
- Setas sólidas (`->>`) para chamadas síncronas, tracejadas (`-->>`) para retornos.
- Ativação de lifeline via `+`/`-`.
- Blocos `alt`/`else` para regras de negócio condicionais e transições de estado.
- Blocos `loop` para o laço de publicação de domain events em `BaseUnitOfWork.CompleteAsync()`.
- `Note over` apenas para fronteiras de módulo e regras de negócio que se manifestam como ramificação de fluxo.
- A Domain Entity nunca interage diretamente com o DbContext — apenas Repository/UnitOfWork persiste.
- O `Validator` (FluentValidation) não aparece como participante/lifeline própria: a validação roda dentro do pipeline do `Mediator` (Behavior).
- A busca de agregado por Id seguida de checagem de existência é simplificada para uma única seta de busca + nota `Retorna Result.NotFound se não existir`.

---

## 1. Solicitar Agendamento

Fontes: `src/Modules/Scheduling/Presentation/Schedules/ScheduleEndpoints.cs` (`MapInstitutionalScheduleEndpoints`), `src/Modules/Scheduling/Presentation/SchedulingModule.cs` (compõe a rota `/api/scheduling/public/schedules`), `src/Modules/Scheduling/Application/Schedules/Commands/Create/{CreateScheduleCommand,CreateScheduleHandler,CreateScheduleValidator}.cs`, `src/Modules/Scheduling/Domain/Schedules/{Schedule,Scheduling}.cs`, `src/Modules/Scheduling/Domain/Schedules/Policies/BusinessTimePolicies.cs`, `src/Modules/Scheduling/Domain/Schedules/Events/NewScheduleDomainEvent.cs`, `src/Modules/Scheduling/Application/Schedules/EventHandlers/{NewScheduleNotificationEventHandler,NewScheduleEmailEventHandler}.cs`, `src/Modules/Notify/Contracts/ISendNotification.cs`, `src/Modules/Notify/Domain/Notifications/Notification.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Visitante as Visitante (Next.js institucional)
 participant Endpoint as ScheduleEndpoints
 participant Mediator as IMediator
 participant Handler as CreateScheduleHandler
 participant Repo as IScheduleRepository
 participant SchedulingVO as Scheduling (Domain VO)
 participant ScheduleAgg as Schedule (Domain Aggregate)
 participant UoW as ISchedulingUnitOfWork
 participant Observers as Domain Event Handlers

 Note over Visitante,Endpoint: RequireRateLimiting("SchedulingPolicy") — 5 requisições/hora por IP
 Visitante->>Endpoint: POST /api/scheduling/public/schedules
 Endpoint->>Mediator: Send(CreateScheduleCommand)
 Note over Mediator: Executa Pipeline de Validação (FluentValidation)
 Mediator->>+Handler: Handle(command)

 Handler->>Repo: GetSchedulesConflictAsync(start, end, equipmentIds)
 Repo-->>Handler: List~Schedule~ conflitos

 alt Conflitos.Count > 0
 Handler-->>Mediator: Result.BusinessRule("...horário conflitante com outros agendamentos confirmados")
 else Sem conflitos
 Handler->>+SchedulingVO: Scheduling.Create(date, start, end)
 SchedulingVO->>SchedulingVO: BusinessTimePolicies.Validate(date, start, end)
 alt Data passada / fora de dia útil ou horário comercial
 SchedulingVO-->>Handler: Result.Failure (Validation/BusinessRule)
 Handler-->>Mediator: Result (erro)
 else Validação OK
 SchedulingVO-->>-Handler: Result.Success(Scheduling)
 Handler->>+ScheduleAgg: Schedule.Create(scheduler, scheduling, acceptTerm, advisorProfessor, projectTitle, description, equipments)
 ScheduleAgg-->>-Handler: Result.Success(Schedule)
 Handler->>Repo: AddAsync(schedule)
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(NewScheduleDomainEvent)
 Note over Observers: Dispara notificação interna (ISendNotification) + e-mail ao Scheduler.Email — handlers independentes reagindo ao mesmo evento (NewScheduleNotificationEventHandler, NewScheduleEmailEventHandler)
 UoW->>UoW: Aplica auditoria (CreatedAt/CreatedBy) e SaveChangesAsync()
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Visitante: 201 Created
```

**Regra de negócio em destaque:** o construtor privado de `Schedule.Create` já define `Status=PENDING` e chama `AddEvent(NewScheduleDomainEvent)`, mas a publicação real do evento via Mediator só acontece dentro do loop de `BaseUnitOfWork.CompleteAsync()` — há, portanto, uma defasagem entre "evento criado" (buffer em memória) e "evento publicado". A checagem de conflito de horário (`GetSchedulesConflictAsync`) ocorre ANTES de qualquer criação de objeto de domínio, e o endpoint público está protegido por rate limiting (5 requisições/hora por IP).

---

## 2. Aprovar Agendamento

Fontes: `src/Modules/Scheduling/Presentation/Schedules/ScheduleEndpoints.cs`, `src/Modules/Scheduling/Application/Schedules/Commands/Approve/{ApproveScheduleCommand,ApproveScheduleCommandHandler}.cs`, `src/Modules/Scheduling/Domain/Schedules/Schedule.cs` (`EnsureCanBeApprovedOrRefused`, `Approve`), `src/Modules/Scheduling/Domain/Schedules/Events/{ApprovedScheduleDomainEvent,ReprovedScheduleDomainEvent}.cs`, `src/Modules/Scheduling/Application/Schedules/EventHandlers/{ApprovedScheduleEmailEventHandler,ReprovedScheduleEmailEventHandler}.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as ScheduleEndpoints
 participant Mediator as IMediator
 participant Handler as ApproveScheduleCommandHandler
 participant Repo as IScheduleRepository
 participant ScheduleAgg as Schedule (Domain Aggregate)
 participant Conflict as Schedule conflitante (Domain)
 participant UoW as ISchedulingUnitOfWork
 participant Observers as Domain Event Handlers

 Admin->>Endpoint: POST /api/scheduling/schedules/{id}/approve
 Endpoint->>Mediator: Send(ApproveScheduleCommand)
 Note over Mediator: Executa Pipeline de Validação (FluentValidation) — sem validator dedicado além do pipeline padrão
 Mediator->>+Handler: Handle(command)
 Handler->>Repo: Busca Schedule por Id
 Repo-->>Handler: Schedule
 Note over Handler: Retorna Result.NotFound se não existir

 Handler->>+ScheduleAgg: Approve(currentUser.Id)
 ScheduleAgg->>ScheduleAgg: EnsureCanBeApprovedOrRefused()
 alt Status != PENDING
 ScheduleAgg-->>Handler: Result.BusinessRule("Agendamento não está pendente.")
 else Scheduling.Date anterior a hoje
 ScheduleAgg-->>Handler: Result.BusinessRule("Não é possível alterar agendamento com data passada.")
 else Válido para aprovação
 ScheduleAgg->>ScheduleAgg: Status = SCHEDULED, ApprovedBy = userId
 ScheduleAgg->>ScheduleAgg: AddEvent(ApprovedScheduleDomainEvent(this))
 ScheduleAgg-->>-Handler: Result.Success()
 end

 alt result.IsFailure
 Handler-->>Mediator: result (erro) — retorna imediatamente, antes da cascata de conflitos e de CompleteAsync
 else result.IsSuccess
 Handler->>Repo: GetSchedulesConflictAsync(StartDateHour, EndDateHour, equipmentIds)
 Repo-->>Handler: List~Schedule~ conflitos
 loop Para cada agendamento conflitante PENDING (Id != schedule.Id)
 Handler->>+Conflict: Refuse(currentUser.Id, "Outro agendamento com horário conflitante foi aprovado.")
 Conflict->>Conflict: EnsureCanBeApprovedOrRefused() + Status=REFUSED, RefusedBy, RefuseJustification
 Conflict->>Conflict: AddEvent(ReprovedScheduleDomainEvent(this, justification))
 Conflict-->>-Handler: Result (não verificado pelo handler)
 end
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(ApprovedScheduleDomainEvent + N ReprovedScheduleDomainEvent)
 Note over Observers: Dispara e-mail de Aprovação ao Scheduler.Email e N e-mails de Reprovação por Conflito aos solicitantes conflitantes. ÚNICO CompleteAsync publica todos no mesmo loop. Não existe handler de notificação interna para estes eventos, só e-mail
 UoW-->>-Handler: OK
 Handler-->>Mediator: result (Result.Success())
 end
 deactivate Handler
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 202 Accepted
```

**Regra de negócio em destaque:** ao aprovar um agendamento, o `ApproveScheduleCommandHandler` só executa a cascata de recusa de conflitos (`RefuseConflictingSchedules` — busca novamente conflitos via `GetSchedulesConflictAsync` e recusa todos os agendamentos `PENDING` conflitantes) e chama `CompleteAsync()` se `Approve()` tiver retornado sucesso; há uma guarda `if (result.IsFailure) return result;` logo após a chamada a `Approve`, que interrompe o fluxo antes da cascata e da persistência em caso de falha. Quando bem-sucedido, `CompleteAsync()` publica o `ApprovedScheduleDomainEvent` do agregado aprovado e todos os `ReprovedScheduleDomainEvent` dos conflitantes recusados no mesmo loop de eventos.

---

## 3. Recusar Agendamento

Fontes: `src/Modules/Scheduling/Presentation/Schedules/ScheduleEndpoints.cs`, `src/Modules/Scheduling/Application/Schedules/Commands/Refuse/{RefuseScheduleCommand,RefuseScheduleCommandHandler,RefuseScheduleValidator}.cs`, `src/Modules/Scheduling/Domain/Schedules/Schedule.cs` (`EnsureCanBeApprovedOrRefused`, `Refuse`), `src/Modules/Scheduling/Domain/Schedules/Events/ReprovedScheduleDomainEvent.cs`, `src/Modules/Scheduling/Application/Schedules/EventHandlers/ReprovedScheduleEmailEventHandler.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as ScheduleEndpoints
 participant Mediator as IMediator
 participant Handler as RefuseScheduleCommandHandler
 participant Repo as IScheduleRepository
 participant ScheduleAgg as Schedule (Domain Aggregate)
 participant UoW as ISchedulingUnitOfWork
 participant Observers as Domain Event Handlers

 Admin->>Endpoint: POST /api/scheduling/schedules/{id}/refuse (Justification)
 Endpoint->>Mediator: Send(RefuseScheduleCommand)
 Note over Mediator: Executa Pipeline de Validação (FluentValidation) — Justification >= 20 caracteres
 Mediator->>+Handler: Handle(command)
 Handler->>Repo: Busca Schedule por Id
 Repo-->>Handler: Schedule
 Note over Handler: Retorna Result.NotFound se não existir

 Handler->>+ScheduleAgg: Refuse(currentUser.Id, justification)
 ScheduleAgg->>ScheduleAgg: EnsureCanBeApprovedOrRefused()
 alt Status != PENDING ou data passada
 ScheduleAgg-->>Handler: Result.BusinessRule(mensagem)
 else Válido para recusa
 ScheduleAgg->>ScheduleAgg: Status = REFUSED, RefusedBy = userId, RefuseJustification = justification
 ScheduleAgg->>ScheduleAgg: AddEvent(ReprovedScheduleDomainEvent(this, justification))
 ScheduleAgg-->>-Handler: Result.Success()
 end
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(ReprovedScheduleDomainEvent)
 Note over Observers: ReprovedScheduleEmailEventHandler envia e-mail ao Scheduler.Email — não existe handler de notificação interna (ISendNotification) para este evento
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 202 Accepted
```

**Regra de negócio em destaque:** diferente do fluxo de Aprovação (Diagrama 2), a recusa direta NÃO desencadeia cascata sobre outros agendamentos — afeta apenas o próprio agregado. O `RefuseScheduleCommandHandler` chama `_unitOfWork.CompleteAsync()` mesmo quando `schedule.Refuse(...)` falha internamente (o handler não verifica o `Result` retornado por `Refuse` antes de persistir, ao contrário dos demais handlers deste documento), retornando sempre `Result.Success()` ao Mediator.

---

## 4. Cancelar Agendamento

Fontes: `src/Modules/Scheduling/Presentation/Schedules/ScheduleEndpoints.cs`, `src/Modules/Scheduling/Application/Schedules/Commands/Cancel/{CancelScheduleCommand,CancelScheduleHandler}.cs`, `src/Modules/Scheduling/Domain/Schedules/Schedule.cs` (`Cancel`), `src/Modules/Scheduling/Domain/Schedules/Events/CanceledScheduleDomainEvent.cs`, `src/Modules/Scheduling/Application/Schedules/EventHandlers/CanceledScheduleEmailEventHandler.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin
 participant Endpoint as ScheduleEndpoints
 participant Mediator as IMediator
 participant Handler as CancelScheduleHandler
 participant Repo as IScheduleRepository
 participant ScheduleAgg as Schedule (Domain Aggregate)
 participant UoW as ISchedulingUnitOfWork
 participant Observers as Domain Event Handlers

 Admin->>Endpoint: POST /api/scheduling/schedules/{id}/cancel (Justification)
 Endpoint->>Mediator: Send(CancelScheduleCommand)
 Mediator->>+Handler: Handle(command)
 Handler->>Repo: Busca Schedule por Id
 Repo-->>Handler: Schedule
 Note over Handler: Retorna Result.NotFound se não existir

 Handler->>+ScheduleAgg: Cancel(justification, currentUser.Id)
 alt Status == CANCELED
 ScheduleAgg-->>Handler: Result.BusinessRule("Agendamento já cancelado.")
 else Status == REFUSED
 ScheduleAgg-->>Handler: Result.BusinessRule("Agendamento reprovado não pode ser cancelado.")
 else Status == PENDING ou SCHEDULED (qualquer outro)
 ScheduleAgg->>ScheduleAgg: Status = CANCELED
 ScheduleAgg->>ScheduleAgg: RefusedBy = userId, RefuseJustification = justification
 ScheduleAgg->>ScheduleAgg: AddEvent(CanceledScheduleDomainEvent(this, justification))
 ScheduleAgg-->>-Handler: Result.Success()
 end
 alt Result.IsFailure
 Handler-->>Mediator: Result (erro) — CompleteAsync NÃO é chamado
 else Result.IsSuccess
 Handler->>+UoW: CompleteAsync()
 UoW->>Observers: Publish(CanceledScheduleDomainEvent)
 Note over Observers: CanceledScheduleEmailEventHandler envia e-mail ao Scheduler.Email — único handler, sem notificação interna, igual ao padrão de Aprovar/Recusar
 UoW-->>-Handler: OK
 Handler-->>-Mediator: Result.Success()
 end
 Mediator-->>Endpoint: Result
 Endpoint-->>Admin: 202 Accepted
```

**Regra de negócio em destaque:** `Schedule.Cancel` usa uma regra de exclusão DIFERENTE de `EnsureCanBeApprovedOrRefused` (usada em Aprovar/Recusar) — bloqueia apenas `Status == CANCELED` ou `Status == REFUSED`; qualquer outro status, incluindo `PENDING` (ainda não avaliado) e `SCHEDULED` (já aprovado), permite o cancelamento. A entidade reaproveita os campos `RefusedBy`/`RefuseJustification` (os MESMOS usados por `Refuse`, ver Diagrama 3) em vez de ter campos dedicados a cancelamento — não há campos próprios de cancelamento, uma decisão de modelagem que reduz a superfície de dados, mas reaproveita semântica de outro fluxo.
