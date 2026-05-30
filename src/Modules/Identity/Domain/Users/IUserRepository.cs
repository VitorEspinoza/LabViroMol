using LabViroMol.Modules.Shared.Kernel.Identity;

namespace LabViroMol.Modules.Identity.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}
