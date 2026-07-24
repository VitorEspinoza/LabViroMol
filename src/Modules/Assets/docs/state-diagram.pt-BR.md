# Diagrama de Estado — MaintenanceRequest (Módulo Assets)

[English](./state-diagram.md) · **Português**

Este documento apresenta o diagrama de estado do agregado `MaintenanceRequest`.

Fontes: `src/Modules/Assets/Domain/MaintenanceRequests/MaintenanceRequest.cs`, `src/Modules/Assets/Domain/MaintenanceRequests/MaintenanceRequestStatus.cs`, Handlers em `src/Modules/Assets/Application/MaintenanceRequests/Commands/{Create,Start,Done,Cancel}/`, `src/Modules/Assets/Application/Equipments/EventHandlers/EquipmentDeletedDomainEventHandler.cs`.

`MaintenanceRequestStatus` tem 4 estados: `Requested`, `InProgress`, `Done`, `Cancelled`. `Done` e `Cancelled` são estados terminais.

```mermaid
stateDiagram-v2
 [*] --> Requested: MaintenanceRequest.Create()
 note right of Requested
 Guarda na Application (CreateMaintenanceHandler):
 bloqueia criação se já existir solicitação
 ativa para o mesmo Equipment
 ("Manutenção para equipamento já
 requisitada ou em andamento.").
 end note

 Requested --> InProgress: Start()
 note right of InProgress
 Guarda: Status == Requested, senão
 "Não é possível alterar o status para
 'Em progresso'."
 end note

 InProgress --> Done: Done()
 note right of Done
 Guarda: Status == InProgress, senão
 "Não é possível alterar o status para
 'Finalizado'."
 end note

 Requested --> Cancelled: Cancel()
 InProgress --> Cancelled: Cancel()
 note right of Cancelled
 Guarda (por exclusão): bloqueia somente
 se Status == Done
 ("Não é possível cancelar solicitações
 finalizadas."). Permite cancelar tanto
 Requested quanto InProgress.
 end note

 note left of Requested
 Exclusão do Equipment associado dispara
 EquipmentDeletedDomainEvent, tratado por
 EquipmentDeletedDomainEventHandler, que
 remove (hard delete) todas as
 MaintenanceRequest daquele equipamento,
 independente do status. Não é uma
 transição de estado, é exclusão de
 registro — representado aqui apenas
 como observação.
 end note

 Done --> [*]
 Cancelled --> [*]
```

**Guia de leitura**: toda solicitação de manutenção nasce `Requested` (bloqueada na criação se já houver outra ativa para o mesmo equipamento) e segue o fluxo linear `Requested → InProgress → Done`. O cancelamento (`Cancel()`) é permitido em qualquer ponto antes da conclusão (`Requested` ou `InProgress`), mas nunca depois (`Done` é terminal e protegido). A exclusão do `Equipment` associado não é uma transição de estado — é uma remoção direta do registro pelo `EquipmentDeletedDomainEventHandler`, fora do controle de status do agregado.
