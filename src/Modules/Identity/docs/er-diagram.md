# Entity-Relationship Diagram — `identity` Schema

**English** · [Português](./er-diagram.pt-BR.md)

This document extracts the **`identity`** schema block. It models the
real persistence layer (not the domain aggregates): physical tables, columns,
types, primary/foreign keys and cardinality, extracted directly from the
`*Configuration.cs` files and confirmed against the module's latest migrations.

DbContext: `LabViroMolIdentityDbContext` (`IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`).
This schema hosts both the ASP.NET Core Identity framework (`IdentityUsers`, `Roles`,
`UserRoles`, `UserClaims`, `UserLogins`, `UserTokens`, `RoleClaims`) and the app's own
domain aggregate `Users`, linked 1:1 by the same `Guid` Id (no database FK between them
— it is the same primary key value intentionally shared, not a reference).

```mermaid
erDiagram
 IdentityUsers {
 uuid Id PK
 varchar UserName
 varchar NormalizedUserName UK
 varchar Email
 varchar NormalizedEmail
 boolean EmailConfirmed
 text PasswordHash
 text SecurityStamp
 text ConcurrencyStamp
 text PhoneNumber
 boolean PhoneNumberConfirmed
 boolean TwoFactorEnabled
 timestamptz LockoutEnd
 boolean LockoutEnabled
 int AccessFailedCount
 }

 Users {
 uuid Id PK "same Guid as IdentityUsers.Id"
 varchar FirstName
 varchar LastName
 varchar Email
 varchar PhoneNumber
 varchar EmergencyContactName
 varchar EmergencyContactNumber
 timestamptz DeactivatedAt
 timestamptz CreatedAt
 uuid CreatedBy
 timestamptz UpdatedAt
 uuid UpdatedBy
 }

 Roles {
 uuid Id PK
 varchar Name
 varchar NormalizedName UK
 text ConcurrencyStamp
 boolean IsDeleted
 timestamptz RemovedAt
 uuid RemovedBy
 }

 UserClaims {
 int Id PK
 uuid UserId FK
 text ClaimType
 text ClaimValue
 }

 UserLogins {
 text LoginProvider PK
 text ProviderKey PK
 text ProviderDisplayName
 uuid UserId FK
 }

 UserTokens {
 uuid UserId PK,FK
 text LoginProvider PK
 text Name PK
 text Value
 }

 RoleClaims {
 int Id PK
 uuid RoleId FK
 text ClaimType
 text ClaimValue
 }

 UserRoles {
 uuid UserId PK,FK
 uuid RoleId PK,FK
 }

 IdentityUsers ||--o{ UserClaims: "1:N (FK_UserClaims_IdentityUsers_UserId, cascade)"
 IdentityUsers ||--o{ UserLogins: "1:N (FK_UserLogins_IdentityUsers_UserId, cascade)"
 IdentityUsers ||--o{ UserTokens: "1:N (FK_UserTokens_IdentityUsers_UserId, cascade)"
 IdentityUsers ||--o{ UserRoles: "1:N (FK_UserRoles_IdentityUsers_UserId, cascade)"
 Roles ||--o{ RoleClaims: "1:N (FK_RoleClaims_Roles_RoleId, cascade)"
 Roles ||--o{ UserRoles: "1:N (FK_UserRoles_Roles_RoleId, cascade)"
```

> Note: `Users.Id` and `IdentityUsers.Id` share the same `Guid` value by
> application convention (created together during registration) — there is no database FK
> between the two tables, hence no ER line between them. `Users` has no soft
> delete (the `User` domain entity implements only `ICreationAuditable`/`IModificationAuditable`,
> not `IDeletionAuditable`) — deactivating a user uses `DeactivatedAt`, not `IsDeleted`.
