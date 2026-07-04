# Class Diagram — Assets Module

**English** · [Português](./class-diagram.pt-BR.md)

This document presents the domain class diagram specific to the **Assets** module. It covers exclusively the Domain layer: the `Equipment` and `MaintenanceRequest` aggregate roots, the `EquipmentTranslation` value object and the `MaintenanceRequestStatus` enum.

```mermaid
classDiagram
 class Equipment {
 +Name: string
 +Brand: string
 +Model: string
 +Code: string
 +Description: string
 +ImageUrl: string
 +Location: string
 +Translations: Dictionary~string, EquipmentTranslation~
 +Create(name, brand, model, code, description, location) Result~Equipment~
 +Update(name, brand, model, description, location) void
 +AttachImageUrl(imageUrl) void
 +Delete() void
 +AddTranslation(languageCode, name, description) void
 +GetName(language) string
 +GetDescription(language) string
 }
 Equipment --|> AggregateRoot~EquipmentId~
 Equipment..|> IFullAuditable
 Equipment..|> ITranslatable~EquipmentTranslation~

 class EquipmentTranslation {
 <<value object>>
 +Name: string
 +Description: string
 }
 Equipment "1" *-- "many" EquipmentTranslation: Translations

 class MaintenanceRequest {
 +Status: MaintenanceRequestStatus
 +Description: string
 +ProblemDescription: string
 +EquipmentId: EquipmentId
 +Create(description, problemDescription, equipmentId) Result~MaintenanceRequest~
 +Start() Result
 +Done() Result
 +Cancel() Result
 }
 MaintenanceRequest --|> AggregateRoot~MaintenanceRequestId~
 MaintenanceRequest..|> IFullAuditable
 MaintenanceRequest "many" --> "1" Equipment: EquipmentId

 class MaintenanceRequestStatus {
 <<enumeration>>
 Requested
 InProgress
 Done
 Cancelled
 }
 MaintenanceRequest --> MaintenanceRequestStatus: Status
```
