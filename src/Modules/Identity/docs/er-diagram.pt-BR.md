# Diagrama Entidade-Relacionamento — Schema `identity`

[English](./er-diagram.md) · **Português**

Este documento extrai o bloco do schema **`identity`**. Modela a
camada de persistência real (não os agregados de domínio): tabelas físicas, colunas,
tipos, chaves primárias/estrangeiras e cardinalidade, extraídos diretamente dos arquivos
`*Configuration.cs` e confirmados contra as migrations mais recentes do módulo.

DbContext: `LabViroMolIdentityDbContext` (`IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`).
Convive nesta migração o framework ASP.NET Core Identity (`IdentityUsers`, `Roles`,
`UserRoles`, `UserClaims`, `UserLogins`, `UserTokens`, `RoleClaims`) e o agregado de
domínio próprio `Users`, ligados 1:1 pelo mesmo `Guid` de Id (sem FK de banco entre eles
— é o mesmo valor de chave primária compartilhado intencionalmente, não uma referência).

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
 uuid Id PK "mesmo Guid de IdentityUsers.Id"
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

> Nota: `Users.Id` e `IdentityUsers.Id` compartilham o mesmo valor de `Guid` por
> convenção de aplicação (criados juntos no fluxo de registro) — não há FK de banco
> entre as duas tabelas, por isso não há linha ER entre elas. `Users` não tem soft
> delete (`User` no domínio implementa apenas `ICreationAuditable`/`IModificationAuditable`,
> não `IDeletionAuditable`) — desativação de usuário usa `DeactivatedAt`, não `IsDeleted`.
