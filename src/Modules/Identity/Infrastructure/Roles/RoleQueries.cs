using LabViroMol.Modules.Identity.Application.Roles.Queries;
using LabViroMol.Modules.Identity.Application.Users.ViewModels;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Identity.Infrastructure.Roles;

public class RoleQueries(LabViroMolIdentityDbContext context) : IRoleQueries
{
    public async Task<IReadOnlyCollection<RoleViewModel>> GetAll()
        => await context.Roles.AsNoTracking()
            .Select(r => new RoleViewModel(r.Id, r.Name!))
            .ToListAsync();

    public async Task<IReadOnlyCollection<RoleDetailViewModel>> GetAllWithPermissions()
        => await context.Roles.AsNoTracking()
            .Select(r => new RoleDetailViewModel(
                r.Id,
                r.Name!,
                context.RoleClaims
                    .Where(rc => rc.RoleId == r.Id && rc.ClaimType == "permission")
                    .Select(rc => rc.ClaimValue!)
                    .ToList()))
            .ToListAsync();
}
