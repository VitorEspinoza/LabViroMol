using LabViroMol.Modules.Identity.Application.Users.ViewModels;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Identity.Infrastructure.Users;

public class UserQueries(LabViroMolIdentityDbContext context, IResearcherProfileProvider researcherProfileProvider)
{
    public async Task<PagedResponse<UserSummaryViewModel>> GetAllAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<User> query = context.DomainUsers.AsNoTracking();

        query = query.WhereSearch(request.Search,
            u => u.Name.FirstName, u => u.Name.LastName, u => u.Email.Value);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "email" => request.SortDirection == "desc"
                ? query.OrderByDescending(u => u.Email.Value)
                : query.OrderBy(u => u.Email.Value),
            "isactive" => request.SortDirection == "desc"
                ? query.OrderByDescending(u => u.IsActive)
                : query.OrderBy(u => u.IsActive),
            _ => request.SortDirection == "desc"
                ? query.OrderByDescending(u => u.Name.LastName).ThenByDescending(u => u.Name.FirstName)
                : query.OrderBy(u => u.Name.LastName).ThenBy(u => u.Name.FirstName)
        };

        var users = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        var userIds = users.Select(u => u.Id.Value).ToList();
        var rolesByUser = await context.UserRoles
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name! })
            .ToListAsync();

        var items = users.Select(u => new UserSummaryViewModel(
            u.Id.Value,
            u.Name.FullName,
            u.Email.Value,
            u.IsActive,
            rolesByUser.Where(r => r.UserId == u.Id.Value).Select(r => r.RoleName).ToList(),
            u.EmergencyContact?.Name,
            u.EmergencyContact?.Number
        )).ToList();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<UserProfileViewModel?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var id = UserId.From(userId);
        var user = await context.DomainUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return null;

        var userRoles = await context.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { r.Id, RoleName = r.Name! })
            .ToListAsync(ct);

        var roleIds = userRoles.Select(r => r.Id).ToList();

        var permissions = await context.RoleClaims.AsNoTracking()
            .Where(rc => roleIds.Contains(rc.RoleId) && rc.ClaimType == "permission")
            .Select(rc => rc.ClaimValue!)
            .Distinct()
            .ToListAsync(ct);

        var researchData = await researcherProfileProvider.GetByUserIdAsync(userId, ct);

        return new UserProfileViewModel(
            user.Id.Value,
            new UserInfo(
                user.Name.FirstName,
                user.Name.LastName,
                user.PhoneNumber,
                user.EmergencyContact?.Name,
                user.EmergencyContact?.Number,
                researchData),
            user.IsActive,
            userRoles.Select(r => r.RoleName).ToList(),
            permissions);
    }
}
