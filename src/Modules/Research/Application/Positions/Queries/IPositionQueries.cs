namespace LabViroMol.Modules.Research.Application.Positions.Queries;

using LabViroMol.Modules.Research.Application.Positions.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

public interface IPositionQueries
{
    Task<PagedResponse<PositionViewModel>> GetAllAsync(PagedRequest request);

    Task<PositionViewModel?> GetById(Guid id);
}
