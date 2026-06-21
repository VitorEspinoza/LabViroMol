using LabViroMol.Modules.Inventory.Application.Kits.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Inventory.Application.Kits.Queries;

public interface IKitQueries
{
    Task<PagedResponse<KitViewModel>> GetAllAsync(PagedRequest request);
    Task<KitViewModel?> GetKitById(Guid id);
}
