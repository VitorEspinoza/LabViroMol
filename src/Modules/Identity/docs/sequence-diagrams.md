# Sequence Diagrams — Identity Module

**English** · [Português](./sequence-diagrams.pt-BR.md)

This document gathers the 2 sequence diagrams of the **Identity** module: **Login** and **Refresh Token**.
Both follow the same conventions (`autonumber`, solid/dashed
arrows for calls/returns, `alt`/`else` blocks for conditional business rules,
`Note over` used only for module boundaries and business rules that
manifest as flow branching).

---

## 1. Login

Sources: `src/Modules/Identity/Presentation/Users/UserEndpoints.cs`, `src/Modules/Identity/Application/Users/Login/{LoginCommand,LoginCommandHandler,LoginCommandValidator}.cs`, `src/Modules/Identity/Application/Users/Abstractions/IIdentityService.cs`, `src/Modules/Identity/Infrastructure/Services/IdentityService.cs`, `src/Modules/Identity/Infrastructure/Identity/ApplicationUser.cs`.

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
 Note over Mediator: Runs the Validation Pipeline (FluentValidation) — email/password not empty
 Mediator->>+LoginHandler: Handle(command)
 LoginHandler->>+IdSvc: LoginAsync(email, password)
 IdSvc->>UserMgr: FindByEmailAsync(email)
 UserMgr-->>IdSvc: ApplicationUser?
 alt User not found
 IdSvc-->>LoginHandler: Result.NotFound("Invalid credentials.")
 else User found
 IdSvc->>UserMgr: IsLockedOutAsync(user)
 alt Account locked
 UserMgr-->>IdSvc: true
 IdSvc-->>LoginHandler: Result.BusinessRule("Account temporarily locked...")
 else Account not locked
 IdSvc->>UserMgr: CheckPasswordAsync(user, password)
 alt Invalid password
 UserMgr-->>IdSvc: false
 IdSvc->>UserMgr: AccessFailedAsync(user)
 IdSvc-->>LoginHandler: Result.NotFound("Invalid credentials.")
 else Valid password
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
 Endpoint-->>Admin: 200 OK with error in body
 else Result.IsSuccess
 Endpoint->>Cookies: Append("X-Access-Token", accessToken, HttpOnly, SameSite=Strict, MaxAge=2h)
 Endpoint->>Cookies: Append("X-Refresh-Token", refreshToken, HttpOnly, SameSite=Strict, Path=/api/identity/users/refresh, MaxAge=7d)
 Endpoint-->>Admin: 200 OK (Set-Cookie x2)
 end
```

**Highlighted business rule:** `LoginCommandHandler` never interacts directly with the `User` domain aggregate — all authentication logic is delegated to `IdentityService`, which operates primarily on `ApplicationUser` (ASP.NET Identity) and only queries `_dbContext.DomainUsers` directly (bypassing the repository) to enrich the token with first/last name (a direct read of the `User` domain aggregate, outside the repository pattern, precisely because it is only for this one-off enrichment). The error response is always `200 OK` with the error in the body (there is no mapping to an error HTTP status on this endpoint).

---

## 2. Refresh Token

Sources: `src/Modules/Identity/Presentation/Users/UserEndpoints.cs`, `src/Modules/Identity/Application/Users/RefreshToken/{RefreshTokenCommand,RefreshTokenCommandHandler}.cs`, `src/Modules/Identity/Application/Users/Abstractions/IIdentityService.cs`, `src/Modules/Identity/Infrastructure/Services/IdentityService.cs`.

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
 alt Cookie missing
 Endpoint-->>Admin: 400 BadRequest ("Refresh token not found.")
 else Cookie present
 Endpoint->>Mediator: Send(RefreshTokenCommand(refreshToken))
 Mediator->>+RefreshHandler: Handle(command)
 RefreshHandler->>+IdSvc: RefreshTokenAsync(refreshToken)
 IdSvc->>IdSvc: ExtractUserIdFromRefreshToken(refreshToken) — validates JWT signature/expiration
 alt Invalid token/incorrect signature
 IdSvc-->>RefreshHandler: Result.BusinessRule("Invalid refresh token.")
 else Valid token
 IdSvc->>UserMgr: FindByIdAsync(userId)
 alt User not found
 UserMgr-->>IdSvc: null
 IdSvc-->>RefreshHandler: Result.NotFound("User not found.")
 else User found
 IdSvc->>UserMgr: GetAuthenticationTokenAsync(user, "LabViroMol", "RefreshToken")
 UserMgr-->>IdSvc: storedToken
 alt storedToken != received refreshToken
 IdSvc-->>RefreshHandler: Result.BusinessRule("Invalid or revoked refresh token.")
 else Tokens match
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
 Endpoint-->>Admin: 200 OK with error in body
 else Result.IsSuccess
 Endpoint->>Cookies: Append("X-Access-Token", newAccessToken, HttpOnly, SameSite=Strict, MaxAge=2h)
 Endpoint->>Cookies: Append("X-Refresh-Token", newRefreshToken, HttpOnly, SameSite=Strict, Path=/api/identity/users/refresh, MaxAge=7d)
 Endpoint-->>Admin: 200 OK (Set-Cookie x2)
 end
 end
```

**Highlighted business rule:** `RefreshTokenAsync` requires both a valid JWT signature (`ExtractUserIdFromRefreshToken`) and that the received token exactly matches the token stored in the Identity store (`GetAuthenticationTokenAsync`) — this double check is what enables revocation: a logout that clears the stored token immediately invalidates any refresh token JWT still valid in the client's possession. `RefreshTokenCommandHandler` is a class distinct from `LoginCommandHandler`, but it delegates to the same `IdentityService` and follows the same HttpOnly-cookie response structure.
