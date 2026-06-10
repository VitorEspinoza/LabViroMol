using LabViroMol.Modules.Inventory.Application.Orders.ViewModels;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Orders;

public class OrderQueries
{
    private readonly InventoryDbContext _context;

    public OrderQueries(InventoryDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<OrderViewModel?> GetById(Guid id)
    {
        var orderId = OrderId.From(id);

        return await _context.Orders
            .Where(o => o.Id == orderId)
            .Join(_context.Materials,
                o => o.MaterialId,
                m => m.Id,
                (o, m) => new OrderViewModel(
                    m.Id,
                    m.Name,
                    o.ProjectId,
                    "MockProject",
                    o.Status.ToString(),
                    o.Description,
                    o.RequestedQuantity,
                    o.Processing != null ? o.Processing.ProcessedByName : null,
                    o.Processing != null ? o.Processing.ProcessedAt : null,
                    o.Processing != null ? o.Processing.Notes : null,
                    o.Receipt != null ? o.Receipt.ReceivedByName : null,
                    o.Receipt != null ? o.Receipt.ReceivedAt : null,
                    (o.Receipt != null ? o.Receipt.Quantity : null)!,
                    o.Receipt != null ? o.Receipt.Notes : null
                ))
            .FirstOrDefaultAsync();
    }

    public async Task<PagedResponse<OrderSummaryViewModel>> GetAllAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<OrderSummaryViewModel> query = _context.Orders
            .Join(_context.Materials,
                o => o.MaterialId,
                m => m.Id,
                (order, material) =>
                    new OrderSummaryViewModel(
                        "MockProject",
                        material.Name,
                        material.Unit.ToString(),
                        order.RequestedQuantity,
                        (order.Receipt != null ? order.Receipt.Quantity : null)!,
                        order.Status.ToString(),
                        "Mock User",
                        EF.Property<DateTimeOffset>(order, "CreatedAt"))
            );

        query = query.WhereSearch(request.Search, x => x.MaterialName, x => x.MaterialUnit, x => x.Status);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),
            "createdon" => request.SortDirection == "desc"
                ? query.OrderByDescending(o => o.CreatedOn)
                : query.OrderBy(o => o.CreatedOn),
            _ => query.OrderByDescending(o => o.CreatedOn)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }
}
