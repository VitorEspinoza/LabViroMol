using LabViroMol.Modules.Inventory.Application.Materials.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Inventory.Application.Materials.Queries;

public interface IMaterialQueries
{
    Task<PagedResponse<MaterialViewModel>> GetAllAsync(PagedRequest request);
    Task<MaterialViewModel?> GetById(Guid id);
}
