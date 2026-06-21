using LabViroMol.Modules.Assets.Application.MaintenanceRequests.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Queries;

public interface IMaintenanceRequestQueries
{
    Task<PagedResponse<MaintenanceRequestViewModel>> GetAllAsync(PagedRequest request);
}
