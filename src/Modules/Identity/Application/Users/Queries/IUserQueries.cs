using LabViroMol.Modules.Identity.Application.Users.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Identity.Application.Users.Queries;

public interface IUserQueries
{
    Task<PagedResponse<UserSummaryViewModel>> GetAllAsync(PagedRequest request);

    Task<UserProfileViewModel?> GetByIdAsync(Guid userId, CancellationToken ct = default);
}
