using LabViroMol.Modules.Inventory.Application.Orders.ViewModels;
using LabViroMol.Modules.Inventory.Domain.Materials;
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

        var query = _context.Orders
            .Join(_context.Materials,
                o => o.MaterialId,
                m => m.Id,
                (order, material) => new
                {
                    Order = order,
                    MaterialName = material.Name,
                    MaterialUnit = material.Unit,
                    CreatedAt = EF.Property<DateTimeOffset>(order, "CreatedAt")
                });

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search;
            var matchingUnits = Enum.GetValues<Unit>()
                .Where(u => u.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var matchingStatuses = Enum.GetValues<OrderStatus>()
                .Where(s => s.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            query = query.Where(x =>
                x.MaterialName.Contains(search) ||
                matchingUnits.Contains(x.MaterialUnit) ||
                matchingStatuses.Contains(x.Order.Status));
        }

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Order.Status)
                : query.OrderBy(x => x.Order.Status),
            "createdon" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new OrderSummaryViewModel(
                "MockProject",
                x.MaterialName,
                x.MaterialUnit.ToString(),
                x.Order.RequestedQuantity,
                (x.Order.Receipt != null ? x.Order.Receipt.Quantity : null)!,
                x.Order.Status.ToString(),
                "Mock User",
                x.CreatedAt))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }
}
