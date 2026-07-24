using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Identity.Infrastructure.Users;

internal sealed class UserCatalog(LabViroMolIdentityDbContext context) : IUserCatalog
{
    public async Task<Dictionary<Guid, string>> GetUserDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().Select(UserId.From).ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await context.DomainUsers.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id.Value, u => u.Name.FullName, ct);
    }
}
