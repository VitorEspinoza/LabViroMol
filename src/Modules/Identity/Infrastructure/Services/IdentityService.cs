using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Identity.Infrastructure.Identity;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LabViroMol.Modules.Identity.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly LabViroMolIdentityDbContext _dbContext;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly string _frontendBaseUrl;

    private const string LoginProvider = "LabViroMol";
    private const string RefreshTokenName = "RefreshToken";

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        LabViroMolIdentityDbContext dbContext,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _jwtKey = configuration["Jwt:Key"]!;
        _jwtIssuer = configuration["Jwt:Issuer"]!;
        _jwtAudience = configuration["Jwt:Audience"]!;
        _frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
    }

    public async Task<Result<(Guid UserId, string ResetLink)>> CreateUserAsync(string email, CancellationToken ct)
    {
        var applicationUser = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(applicationUser);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result<(Guid, string)>.Validation(errors);
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(applicationUser);
        var resetLink = BuildResetLink(email, resetToken);

        return Result<(Guid, string)>.Success((applicationUser.Id, resetLink));
    }

    public async Task<List<Guid>> GetUserRoleIdsAsync(Guid userId, CancellationToken ct)
        => await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

    public async Task<Result<(string AccessToken, string RefreshToken)>> LoginAsync(
        string email, string password, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return Result<(string, string)>.NotFound("Credenciais inválidas.");

        if (await _userManager.IsLockedOutAsync(user))
            return Result<(string, string)>.BusinessRule(
                "Conta bloqueada temporariamente. Tente novamente mais tarde.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            return Result<(string, string)>.NotFound("Credenciais inválidas.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);
        var rolePermissions = await GetRolePermissionClaims(roles);

        var domainUser = await _dbContext.DomainUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == UserId.From(user.Id), ct);

        var accessToken = GenerateAccessToken(
            user, roles, userClaims, rolePermissions,
            domainUser?.Name.FirstName ?? string.Empty,
            domainUser?.Name.LastName ?? string.Empty);
        var refreshToken = GenerateRefreshToken(user);

        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName, refreshToken);

        return Result<(string, string)>.Success((accessToken, refreshToken));
    }

    public async Task<Result<(string NewAccessToken, string NewRefreshToken)>> RefreshTokenAsync(
        string refreshToken, CancellationToken ct)
    {
        var userId = ExtractUserIdFromRefreshToken(refreshToken);
        if (userId is null)
            return Result<(string, string)>.BusinessRule("Token de atualização inválido.");

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user is null)
            return Result<(string, string)>.NotFound("Usuário não encontrado.");

        var storedToken = await _userManager.GetAuthenticationTokenAsync(
            user, LoginProvider, RefreshTokenName);

        if (storedToken != refreshToken)
            return Result<(string, string)>.BusinessRule("Token de atualização inválido ou revogado.");

        var roles = await _userManager.GetRolesAsync(user);
        var userClaims = await _userManager.GetClaimsAsync(user);
        var rolePermissions = await GetRolePermissionClaims(roles);

        var domainUser = await _dbContext.DomainUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == UserId.From(user.Id), ct);

        var newAccessToken = GenerateAccessToken(
            user, roles, userClaims, rolePermissions,
            domainUser?.Name.FirstName ?? string.Empty,
            domainUser?.Name.LastName ?? string.Empty);
        var newRefreshToken = GenerateRefreshToken(user);

        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName, newRefreshToken);

        return Result<(string, string)>.Success((newAccessToken, newRefreshToken));
    }

    public async Task<Result> LogoutAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.NotFound("Usuário não encontrado.");

        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName);

        return Result.Success();
    }

    public async Task<Result<(string ResetLink, string FirstName)>> GeneratePasswordResetTokenAsync(string email, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return Result<(string, string)>.NotFound("Usuário não encontrado.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = BuildResetLink(email, token);

        var domainUser = await _dbContext.DomainUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == UserId.From(user.Id), ct);

        return Result<(string, string)>.Success((resetLink, domainUser?.Name.FirstName ?? string.Empty));
    }

    public async Task<Result> ResetPasswordAsync(
        string email, string token, string newPassword, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return Result.NotFound("Usuário não encontrado.");

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result.Validation(errors);
        }

        return Result.Success();
    }

    public async Task<Result> SetUserLockoutAsync(Guid userId, bool locked, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.NotFound("Usuário não encontrado.");

        await _userManager.SetLockoutEndDateAsync(user,
            locked ? DateTimeOffset.MaxValue : null);

        return Result.Success();
    }

    public async Task<Result> UpdateUserRolesAsync(Guid userId, List<Guid> roleIds, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.NotFound("Usuário não encontrado.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any())
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (roleIds.Any())
        {
            var roleNames = await _dbContext.Roles
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Name!)
                .ToListAsync(ct);

            await _userManager.AddToRolesAsync(user, roleNames);
        }

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.NotFound("Usuário não encontrado.");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return Result.Validation(errors);
        }

        return Result.Success();
    }

    public async Task<Result> CreateRoleAsync(string name, List<string> permissions, CancellationToken ct)
    {
        var existingRole = await _roleManager.FindByNameAsync(name);
        if (existingRole is not null)
            return Result.BusinessRule("Perfil com este nome já existe.");

        var role = new ApplicationRole { Name = name };
        var createResult = await _roleManager.CreateAsync(role);

        if (!createResult.Succeeded)
            return Result.Validation(createResult.Errors.Select(e => e.Description).ToList());

        foreach (var permission in permissions)
            await _roleManager.AddClaimAsync(role, new Claim("permission", permission));

        return Result.Success();
    }

    public async Task<Result> UpdateRolePermissionsAsync(Guid roleId, List<string> permissions, CancellationToken ct)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return Result.NotFound("Perfil não encontrado.");

        var currentClaims = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in currentClaims.Where(c => c.Type == "permission"))
            await _roleManager.RemoveClaimAsync(role, claim);

        foreach (var permission in permissions)
            await _roleManager.AddClaimAsync(role, new Claim("permission", permission));

        return Result.Success();
    }

    private async Task<List<Claim>> GetRolePermissionClaims(IList<string> roleNames)
    {
        var permissionClaims = new List<Claim>();

        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var claims = await _roleManager.GetClaimsAsync(role);
            permissionClaims.AddRange(claims.Where(c => c.Type == "permission"));
        }

        return permissionClaims
            .DistinctBy(c => c.Value)
            .ToList();
    }

    private string GenerateAccessToken(
        ApplicationUser user, IList<string> roles, IList<Claim> userClaims,
        IList<Claim> rolePermissions, string firstName, string lastName)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.GivenName, firstName),
            new(ClaimTypes.Surname, lastName)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var claim in rolePermissions)
            claims.Add(claim);

        foreach (var claim in userClaims)
            claims.Add(claim);

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string BuildResetLink(string email, string token) =>
        $"{_frontendBaseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

    private string GenerateRefreshToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private Guid? ExtractUserIdFromRefreshToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtIssuer,
            ValidAudience = _jwtAudience,
            IssuerSigningKey = key
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out _);

            var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

            return sub is not null && Guid.TryParse(sub, out var userId)
                ? userId
                : null;
        }
        catch
        {
            return null;
        }
    }
}
