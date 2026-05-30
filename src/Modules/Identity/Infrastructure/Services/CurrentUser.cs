using System.Security.Claims;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LabViroMol.Modules.Identity.Infrastructure.Services;

public class CurrentUser : ICurrentUser
{
    public UserId Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; }
    public bool IsAuthenticated { get; }
    public IReadOnlyList<string> Roles { get; }
    public IReadOnlyList<string> Permissions { get; }

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;

        IsAuthenticated = user?.Identity?.IsAuthenticated ?? false;

        Id = UserId.From(
            Guid.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : Guid.Empty);

        FirstName = user?.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        LastName = user?.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        Email = user?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        Roles = user?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
                ?? [];
        Permissions = user?.FindAll("permission").Select(c => c.Value).ToList()
                      ?? [];
    }
}
