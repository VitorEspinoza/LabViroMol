# Diagrama de Classes — Módulo Identity

[English](./class-diagram.md) · **Português**

Este documento extrai a seção específica do módulo **Identity**, cobrindo exclusivamente a camada Domain: o aggregate root `User` e seus value
objects (`UserName`, `Email`, `EmergencyContact`). Inclui também `ApplicationUser` e
`ApplicationRole` (`src/Modules/Identity/Infrastructure/Identity`), exceção documentada
por serem casos especiais de infraestrutura do ASP.NET Identity intimamente ligados ao
agregado `User` — sem elas a dualidade "usuário de domínio vs. usuário de autenticação"
ficaria invisível no diagrama.

```mermaid
classDiagram
 class User {
 +Name: UserName
 +Email: Email
 +PhoneNumber: string
 +EmergencyContact: EmergencyContact
 +DeactivatedAt: DateTimeOffset
 +IsActive: bool
 +Create(id, name, email, phoneNumber, emergencyContact) User
 +Update(name, email, phoneNumber, emergencyContact) void
 +Deactivate() void
 +Reactivate() void
 }
 User --|> AggregateRoot~UserId~
 User..|> ICreationAuditable
 User..|> IModificationAuditable

 class AggregateRoot~UserId~ {
 <<Shared Kernel>>
 }

 class UserName {
 <<value object>>
 +FirstName: string
 +LastName: string
 +FullName: string
 }

 class Email {
 <<value object>>
 +Value: string
 }

 class EmergencyContact {
 <<value object>>
 +Name: string
 +Number: string
 }

 User "1" --> "1" UserName: Name
 User "1" --> "1" Email: Email
 User "1" --> "0..1" EmergencyContact: EmergencyContact

 class ApplicationUser {
 <<Infrastructure / ASP.NET Identity>>
 }
 ApplicationUser --|> IdentityUser~Guid~

 class ApplicationRole {
 <<Infrastructure / ASP.NET Identity>>
 }
 ApplicationRole --|> IdentityRole~Guid~
 ApplicationRole..|> IDeletionAuditable

 class IdentityUser~Guid~ {
 <<ASP.NET Identity>>
 }
 class IdentityRole~Guid~ {
 <<ASP.NET Identity>>
 }

 User "1" --> "1" ApplicationUser: mesmo Id (Guid), sem FK de classe
```
