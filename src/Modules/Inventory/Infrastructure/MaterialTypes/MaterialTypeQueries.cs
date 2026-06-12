using LabViroMol.Modules.Inventory.Application.MaterialTypes.ViewModels;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.MaterialTypes;

public class MaterialTypeQueries
{
    private readonly InventoryDbContext _context;

    public MaterialTypeQueries(InventoryDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<MaterialTypeViewModel?> GetById(Guid id)
    {
        return await _context.MaterialTypes
            .Where(t => t.Id == MaterialTypeId.From(id))
            .Select(t => new MaterialTypeViewModel(t.Id.Value, t.Name, t.Active))
            .FirstOrDefaultAsync();
    }

    public async Task<PagedResponse<MaterialTypeViewModel>> GetAllAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<MaterialType> query = _context.MaterialTypes;

        query = query.WhereSearch(request.Search, t => t.Name);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == "desc"
                ? query.OrderByDescending(t => t.Name)
                : query.OrderBy(t => t.Name),
            _ => query.OrderBy(t => t.Name)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(t => new MaterialTypeViewModel(t.Id.Value, t.Name, t.Active))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }
}
