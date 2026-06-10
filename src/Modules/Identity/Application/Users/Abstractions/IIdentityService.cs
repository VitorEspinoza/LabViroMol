using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Identity.Application.Users.Abstractions;

public interface IIdentityService
{
    Task<Result<(Guid UserId, string ResetToken)>> CreateUserAsync(string email, CancellationToken ct);
    Task<List<Guid>> GetUserRoleIdsAsync(Guid userId, CancellationToken ct);
    Task<Result<(string AccessToken, string RefreshToken)>> LoginAsync(string email, string password, CancellationToken ct);
    Task<Result<(string NewAccessToken, string NewRefreshToken)>> RefreshTokenAsync(string refreshToken, CancellationToken ct);
    Task<Result> LogoutAsync(Guid userId, CancellationToken ct);
    Task<Result<string>> GeneratePasswordResetTokenAsync(string email, CancellationToken ct);
    Task<Result> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct);
    Task<Result> SetUserLockoutAsync(Guid userId, bool locked, CancellationToken ct);
    Task<Result> UpdateUserRolesAsync(Guid userId, List<Guid> roleIds, CancellationToken ct);
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct);
    Task<Result> CreateRoleAsync(string name, List<string> permissions, CancellationToken ct);
    Task<Result> UpdateRolePermissionsAsync(Guid roleId, List<string> permissions, CancellationToken ct);
}
