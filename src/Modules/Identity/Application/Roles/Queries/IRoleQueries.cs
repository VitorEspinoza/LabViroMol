using LabViroMol.Modules.Identity.Application.Users.ViewModels;

namespace LabViroMol.Modules.Identity.Application.Roles.Queries;

public interface IRoleQueries
{
    Task<IReadOnlyCollection<RoleViewModel>> GetAll();

    Task<IReadOnlyCollection<RoleDetailViewModel>> GetAllWithPermissions();
}
