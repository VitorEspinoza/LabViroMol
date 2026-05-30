using LabViroMol.Modules.Identity.Application.Users.ViewModels;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Identity.Infrastructure.Users;

public class UserQueries(LabViroMolIdentityDbContext context)
{
    public async Task<IReadOnlyCollection<UserSummaryViewModel>> GetAllAsync()
    {
        var users = await context.DomainUsers.AsNoTracking().ToListAsync();

        var userIds = users.Select(u => u.Id.Value).ToList();

        var rolesByUser = await context.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name! })
            .ToListAsync();

        return users.Select(u => new UserSummaryViewModel(
            u.Id.Value,
            u.Name.FullName,
            u.Email.Value,
            u.IsActive,
            rolesByUser.Where(r => r.UserId == u.Id.Value).Select(r => r.RoleName).ToList()
        )).ToList();
    }

    public async Task<UserProfileViewModel?> GetByIdAsync(Guid userId)
    {
        var id = UserId.From(userId);
        var user = await context.DomainUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return null;

        var roles = await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
            .ToListAsync();

        return new UserProfileViewModel(
            user.Id.Value,
            new UserInfo(
                user.Name.FirstName,
                user.Name.LastName,
                user.PhoneNumber,
                user.EmergencyContactNumber,
                null),
            user.IsActive,
            roles);
    }
}
