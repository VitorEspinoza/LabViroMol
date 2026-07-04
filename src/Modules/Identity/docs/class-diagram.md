# Class Diagram — Identity Module

**English** · [Português](./class-diagram.pt-BR.md)

This document extracts the section specific to the **Identity** module, covering exclusively the Domain layer: the `User` aggregate root and its value
objects (`UserName`, `Email`, `EmergencyContact`). It also includes `ApplicationUser` and
`ApplicationRole` (`src/Modules/Identity/Infrastructure/Identity`), a documented exception
since they are special ASP.NET Identity infrastructure cases closely tied to the
`User` aggregate — without them the "domain user vs. authentication user"
duality would be invisible in the diagram.

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

 User "1" --> "1" ApplicationUser: same Id (Guid), no class-level FK
```
