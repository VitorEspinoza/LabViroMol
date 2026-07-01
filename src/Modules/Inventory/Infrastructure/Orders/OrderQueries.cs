using LabViroMol.Modules.Inventory.Application.Orders.Queries;
using LabViroMol.Modules.Inventory.Application.Orders.ViewModels;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Orders;

internal sealed class OrderQueries : IOrderQueries
{
    private readonly InventoryDbContext _context;
    private readonly IProjectCatalog _projectCatalog;
    private readonly IResearcherProfileProvider _researcherProfileProvider;

    public OrderQueries(InventoryDbContext context, IProjectCatalog projectCatalog, IResearcherProfileProvider researcherProfileProvider)
    {
        _context = context;
        _projectCatalog = projectCatalog;
        _researcherProfileProvider = researcherProfileProvider;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<OrderViewModel?> GetById(Guid id, CancellationToken ct = default)
    {
        var orderId = OrderId.From(id);

        var row = await _context.Orders
            .Where(o => o.Id == orderId)
            .Join(_context.Materials,
                o => o.MaterialId,
                m => m.Id,
                (o, m) => new { Order = o, MaterialName = m.Name })
            .FirstOrDefaultAsync(ct);

        if (row is null)
            return null;

        var order = row.Order;
        var projectTitles = await _projectCatalog.GetProjectTitlesAsync([order.ProjectId.Value], ct);

        return new OrderViewModel(
            order.Id,
            order.MaterialId,
            row.MaterialName,
            order.ProjectId,
            projectTitles.GetValueOrDefault(order.ProjectId.Value, string.Empty),
            order.Status.ToString(),
            order.Description,
            order.RequestedQuantity,
            order.Processing != null ? order.Processing.ProcessedByName : null,
            order.Processing != null ? order.Processing.ProcessedAt : null,
            order.Processing != null ? order.Processing.Notes : null,
            order.Receipt != null ? order.Receipt.ReceivedByName : null,
            order.Receipt != null ? order.Receipt.ReceivedAt : null,
            order.Receipt?.Quantity.Value,
            order.Receipt != null ? order.Receipt.Notes : null
        );
    }

    public async Task<PagedResponse<OrderSummaryViewModel>> GetAllAsync(PagedRequest request, CancellationToken ct = default)
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

        var totalCount = await query.CountAsync(ct);

        var sortBy = request.SortBy?.ToLower();

        var projection = query.Select(x => new
        {
            x.Order.Id,
            x.Order.MaterialId,
            x.Order.ProjectId,
            x.MaterialName,
            x.MaterialUnit,
            x.Order.RequestedQuantity,
            ReceivedQuantity = (x.Order.Receipt != null ? x.Order.Receipt.Quantity : null)!,
            x.Order.Status,
            CreatedBy = EF.Property<UserId>(x.Order, "CreatedBy"),
            x.CreatedAt
        });

        if (sortBy == "projecttitle")
        {
            var allRows = await projection.ToListAsync(ct);

            var allProjectTitles = await _projectCatalog.GetProjectTitlesAsync(allRows.Select(r => r.ProjectId.Value), ct);

            var ordered = request.SortDirection == "desc"
                ? allRows.OrderByDescending(r => allProjectTitles.GetValueOrDefault(r.ProjectId.Value, string.Empty))
                : allRows.OrderBy(r => allProjectTitles.GetValueOrDefault(r.ProjectId.Value, string.Empty));

            var pageRows = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            var pageRequesterNames = await _researcherProfileProvider.GetNamesAsync(pageRows.Select(r => r.CreatedBy.Value), ct);

            var pageItems = pageRows.Select(r => new OrderSummaryViewModel(
                r.Id,
                r.MaterialId,
                r.ProjectId,
                allProjectTitles.GetValueOrDefault(r.ProjectId.Value, string.Empty),
                r.MaterialName,
                r.MaterialUnit.ToString(),
                r.RequestedQuantity,
                r.ReceivedQuantity?.Value,
                r.Status.ToString(),
                pageRequesterNames.GetValueOrDefault(r.CreatedBy.Value, string.Empty),
                r.CreatedAt))
                .ToList();

            return PagedResult.Create(pageItems, pageNumber, pageSize, totalCount);
        }

        projection = sortBy switch
        {
            "status" => request.SortDirection == "desc"
                ? projection.OrderByDescending(x => x.Status)
                : projection.OrderBy(x => x.Status),
            "createdat" => request.SortDirection == "desc"
                ? projection.OrderByDescending(x => x.CreatedAt)
                : projection.OrderBy(x => x.CreatedAt),
            "materialname" => request.SortDirection == "desc"
                ? projection.OrderByDescending(x => x.MaterialName)
                : projection.OrderBy(x => x.MaterialName),
            _ => projection.OrderByDescending(x => x.CreatedAt)
        };

        var rows = await projection.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var projectTitles = await _projectCatalog.GetProjectTitlesAsync(rows.Select(r => r.ProjectId.Value), ct);
        var requesterNames = await _researcherProfileProvider.GetNamesAsync(rows.Select(r => r.CreatedBy.Value), ct);

        var items = rows.Select(r => new OrderSummaryViewModel(
            r.Id,
            r.MaterialId,
            r.ProjectId,
            projectTitles.GetValueOrDefault(r.ProjectId.Value, string.Empty),
            r.MaterialName,
            r.MaterialUnit.ToString(),
            r.RequestedQuantity,
            r.ReceivedQuantity?.Value,
            r.Status.ToString(),
            requesterNames.GetValueOrDefault(r.CreatedBy.Value, string.Empty),
            r.CreatedAt))
            .ToList();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }
}
