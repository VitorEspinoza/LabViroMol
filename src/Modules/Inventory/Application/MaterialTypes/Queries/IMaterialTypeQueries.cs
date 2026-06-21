using LabViroMol.Modules.Inventory.Application.MaterialTypes.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Inventory.Application.MaterialTypes.Queries;

public interface IMaterialTypeQueries
{
    Task<MaterialTypeViewModel?> GetById(Guid id);
    Task<PagedResponse<MaterialTypeViewModel>> GetAllAsync(PagedRequest request);
}
