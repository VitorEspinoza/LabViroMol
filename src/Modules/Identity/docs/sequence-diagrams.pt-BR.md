# Diagramas de Sequência — Módulo Identity

[English](./sequence-diagrams.md) · **Português**

Este documento reúne os 2 diagramas de sequência do módulo **Identity**: **Login** e **Refresh Token**.
Ambos seguem as mesmas convenções (`autonumber`, setas
sólidas/tracejadas para chamadas/retornos, blocos `alt`/`else` para regras de negócio
condicionais, `Note over` apenas para fronteiras de módulo e regras de negócio que se
manifestam como ramificação de fluxo).

---

## 1. Login

Fontes: `src/Modules/Identity/Presentation/Users/UserEndpoints.cs`, `src/Modules/Identity/Application/Users/Login/{LoginCommand,LoginCommandHandler,LoginCommandValidator}.cs`, `src/Modules/Identity/Application/Users/Abstractions/IIdentityService.cs`, `src/Modules/Identity/Infrastructure/Services/IdentityService.cs`, `src/Modules/Identity/Infrastructure/Identity/ApplicationUser.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin (Angular App)
 participant Endpoint as UserEndpoints
 participant Mediator as IMediator
 participant LoginHandler as LoginCommandHandler
 participant IdSvc as IdentityService
 participant UserMgr as UserManager~ApplicationUser~
 participant IdDb as LabViroMolIdentityDbContext
 participant Cookies as HttpContext.Response.Cookies

 Admin->>Endpoint: POST /api/identity/users/login (email, password)
 Endpoint->>Mediator: Send(LoginCommand)
 Note over Mediator: Executa Pipeline de Validação (FluentValidation) — email/senha não vazios
 Mediator->>+LoginHandler: Handle(command)
 LoginHandler->>+IdSvc: LoginAsync(email, password)
 IdSvc->>UserMgr: FindByEmailAsync(email)
 UserMgr-->>IdSvc: ApplicationUser?
 alt Usuário não encontrado
 IdSvc-->>LoginHandler: Result.NotFound("Credenciais inválidas.")
 else Usuário encontrado
 IdSvc->>UserMgr: IsLockedOutAsync(user)
 alt Conta bloqueada
 UserMgr-->>IdSvc: true
 IdSvc-->>LoginHandler: Result.BusinessRule("Conta bloqueada temporariamente...")
 else Conta não bloqueada
 IdSvc->>UserMgr: CheckPasswordAsync(user, password)
 alt Senha inválida
 UserMgr-->>IdSvc: false
 IdSvc->>UserMgr: AccessFailedAsync(user)
 IdSvc-->>LoginHandler: Result.NotFound("Credenciais inválidas.")
 else Senha válida
 UserMgr-->>IdSvc: true
 IdSvc->>UserMgr: ResetAccessFailedCountAsync(user)
 IdSvc->>UserMgr: GetRolesAsync / GetClaimsAsync(user)
 IdSvc->>IdSvc: GetRolePermissionClaims(roles)
 IdSvc->>IdDb: DomainUsers.AsNoTracking().FirstOrDefault(u => u.Id == UserId)
 IdDb-->>IdSvc: User? (domain)
 IdSvc->>IdSvc: GenerateAccessToken(user, roles, claims, permissions, firstName, lastName)
 IdSvc->>IdSvc: GenerateRefreshToken(user)
 IdSvc->>UserMgr: SetAuthenticationTokenAsync(user, "LabViroMol", "RefreshToken", refreshToken)
 IdSvc-->>-LoginHandler: Result.Success((accessToken, refreshToken))
 end
 end
 end
 LoginHandler-->>-Mediator: Result
 Mediator-->>Endpoint: Result
 alt Result.IsFailure
 Endpoint-->>Admin: 200 OK com erro no corpo
 else Result.IsSuccess
 Endpoint->>Cookies: Append("X-Access-Token", accessToken, HttpOnly, SameSite=Strict, MaxAge=2h)
 Endpoint->>Cookies: Append("X-Refresh-Token", refreshToken, HttpOnly, SameSite=Strict, Path=/api/identity/users/refresh, MaxAge=7d)
 Endpoint-->>Admin: 200 OK (Set-Cookie x2)
 end
```

**Regra de negócio em destaque:** o `LoginCommandHandler` nunca interage diretamente com o agregado de domínio `User` — toda a lógica de autenticação é delegada ao `IdentityService`, que opera primariamente sobre `ApplicationUser` (ASP.NET Identity) e só consulta `_dbContext.DomainUsers` diretamente (sem repository) para enriquecer o token com nome/sobrenome (leitura direta do agregado de domínio `User`, fora do padrão de repository, justamente porque é só para esse enriquecimento pontual). A resposta de erro é sempre `200 OK` com o erro no corpo (não há mapeamento para status HTTP de erro nesse endpoint).

---

## 2. Refresh Token

Fontes: `src/Modules/Identity/Presentation/Users/UserEndpoints.cs`, `src/Modules/Identity/Application/Users/RefreshToken/{RefreshTokenCommand,RefreshTokenCommandHandler}.cs`, `src/Modules/Identity/Application/Users/Abstractions/IIdentityService.cs`, `src/Modules/Identity/Infrastructure/Services/IdentityService.cs`.

```mermaid
sequenceDiagram
 autonumber
 actor Admin as Admin (Angular App)
 participant Endpoint as UserEndpoints
 participant Mediator as IMediator
 participant RefreshHandler as RefreshTokenCommandHandler
 participant IdSvc as IdentityService
 participant UserMgr as UserManager~ApplicationUser~
 participant IdDb as LabViroMolIdentityDbContext
 participant Cookies as HttpContext.Response.Cookies

 Admin->>Endpoint: POST /api/identity/users/refresh (cookie X-Refresh-Token)
 Endpoint->>Endpoint: Cookies.TryGetValue("X-Refresh-Token", out refreshToken)
 alt Cookie ausente
 Endpoint-->>Admin: 400 BadRequest ("Token de atualização não encontrado.")
 else Cookie presente
 Endpoint->>Mediator: Send(RefreshTokenCommand(refreshToken))
 Mediator->>+RefreshHandler: Handle(command)
 RefreshHandler->>+IdSvc: RefreshTokenAsync(refreshToken)
 IdSvc->>IdSvc: ExtractUserIdFromRefreshToken(refreshToken) — valida assinatura/expiração JWT
 alt Token inválido/assinatura incorreta
 IdSvc-->>RefreshHandler: Result.BusinessRule("Token de atualização inválido.")
 else Token válido
 IdSvc->>UserMgr: FindByIdAsync(userId)
 alt Usuário não encontrado
 UserMgr-->>IdSvc: null
 IdSvc-->>RefreshHandler: Result.NotFound("Usuário não encontrado.")
 else Usuário encontrado
 IdSvc->>UserMgr: GetAuthenticationTokenAsync(user, "LabViroMol", "RefreshToken")
 UserMgr-->>IdSvc: storedToken
 alt storedToken != refreshToken recebido
 IdSvc-->>RefreshHandler: Result.BusinessRule("Token de atualização inválido ou revogado.")
 else Tokens coincidem
 IdSvc->>IdDb: DomainUsers.AsNoTracking().FirstOrDefault(...)
 IdDb-->>IdSvc: User? (domain)
 IdSvc->>IdSvc: GenerateAccessToken(...) / GenerateRefreshToken(...)
 IdSvc->>UserMgr: SetAuthenticationTokenAsync(user, "LabViroMol", "RefreshToken", newRefreshToken)
 IdSvc-->>-RefreshHandler: Result.Success((newAccessToken, newRefreshToken))
 end
 end
 end
 RefreshHandler-->>-Mediator: Result
 Mediator-->>Endpoint: Result
 alt Result.IsFailure
 Endpoint-->>Admin: 200 OK com erro no corpo
 else Result.IsSuccess
 Endpoint->>Cookies: Append("X-Access-Token", newAccessToken, HttpOnly, SameSite=Strict, MaxAge=2h)
 Endpoint->>Cookies: Append("X-Refresh-Token", newRefreshToken, HttpOnly, SameSite=Strict, Path=/api/identity/users/refresh, MaxAge=7d)
 Endpoint-->>Admin: 200 OK (Set-Cookie x2)
 end
 end
```

**Regra de negócio em destaque:** o `RefreshTokenAsync` exige tanto uma assinatura JWT válida (`ExtractUserIdFromRefreshToken`) quanto que o token recebido coincida exatamente com o token armazenado no Identity store (`GetAuthenticationTokenAsync`) — essa dupla checagem é o que permite revogação: um logout que limpe o token armazenado invalida imediatamente qualquer refresh token JWT ainda válido em posse do cliente. O `RefreshTokenCommandHandler` é uma classe distinta do `LoginCommandHandler`, mas delega à mesma `IdentityService` e segue a mesma estrutura de resposta via cookies HttpOnly.
