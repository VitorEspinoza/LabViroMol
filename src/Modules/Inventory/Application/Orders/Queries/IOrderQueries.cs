using LabViroMol.Modules.Inventory.Application.Orders.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Inventory.Application.Orders.Queries;

public interface IOrderQueries
{
    Task<OrderViewModel?> GetById(Guid id, CancellationToken ct = default);
    Task<PagedResponse<OrderSummaryViewModel>> GetAllAsync(PagedRequest request, CancellationToken ct = default);
}
