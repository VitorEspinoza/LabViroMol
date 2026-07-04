# Diagrama de Estado — Schedule (Módulo Scheduling)

[English](./state-diagram.md) · **Português**

Este documento apresenta o diagrama de estado do agregado `Schedule`.

Fontes: `src/Modules/Scheduling/Domain/Schedules/Schedule.cs`, `src/Modules/Scheduling/Domain/Schedules/ScheduleStatus.cs`, Handlers em `src/Modules/Scheduling/Application/Schedules/Commands/{Approve,Refuse,Cancel}/`.

`ScheduleStatus` tem 4 estados: `PENDING`, `SCHEDULED`, `REFUSED`, `CANCELED`. `REFUSED` e `CANCELED` são estados terminais. `Approve` e `Refuse` compartilham a mesma guarda (`EnsureCanBeApprovedOrRefused`); `Cancel` usa uma guarda independente, por exclusão de estados.

```mermaid
stateDiagram-v2
 [*] --> PENDING: Schedule.Create()
 note right of PENDING
 Dispara NewScheduleDomainEvent
 end note

 PENDING --> SCHEDULED: Approve(userId)
 note right of SCHEDULED
 Guarda (EnsureCanBeApprovedOrRefused):
 Status == PENDING e Scheduling.Date
 não pode ser passada, senão
 "Agendamento não está pendente." /
 "Não é possível alterar agendamento
 com data passada."
 Dispara ApprovedScheduleDomainEvent.
 Efeito colateral (fora deste agregado):
 RefuseConflictingSchedules recusa em
 cascata qualquer outro agendamento
 PENDING com conflito de horário/
 equipamento — não representado como
 transição formal de outra instância.
 end note

 PENDING --> REFUSED: Refuse(userId, justification)
 note right of REFUSED
 Mesma guarda de EnsureCanBeApprovedOrRefused.
 Dispara ReprovedScheduleDomainEvent.
 end note

 PENDING --> CANCELED: Cancel(justification, userId)
 SCHEDULED --> CANCELED: Cancel(justification, userId)
 note right of CANCELED
 Guarda (por exclusão): bloqueia somente
 se Status == CANCELED
 ("Agendamento já cancelado.") ou
 Status == REFUSED
 ("Agendamento reprovado não pode ser
 cancelado."). Permite cancelar tanto
 PENDING quanto SCHEDULED.
 Dispara CanceledScheduleDomainEvent.
 Reaproveita os campos RefusedBy/
 RefuseJustification (os mesmos de
 Refuse), sem campos próprios.
 end note

 REFUSED --> [*]
 CANCELED --> [*]
```

**Guia de leitura**: todo agendamento nasce `PENDING` e aguarda decisão. A decisão é binária e terminal em um dos sentidos: `Refuse` leva a `REFUSED` (terminal, sem volta) ou `Approve` leva a `SCHEDULED`. A partir de `PENDING` ou `SCHEDULED`, `Cancel` é sempre possível e leva a `CANCELED` (terminal) — a única combinação bloqueada é cancelar algo que já está `CANCELED` ou `REFUSED`. O efeito cascata de `Approve` sobre outras instâncias de `Schedule` é documentado como nota porque o diagrama de estado é por instância de agregado, não um diagrama de interação entre agregados.
