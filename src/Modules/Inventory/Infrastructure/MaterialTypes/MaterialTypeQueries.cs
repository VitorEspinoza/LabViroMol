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
        var all = await _context.MaterialTypes
            .Select(t => new MaterialTypeViewModel(t.Id.Value, t.Name, t.Active))
            .ToListAsync();

        var sorted = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == "desc"
                ? all.OrderByDescending(t => t.Name).ToList()
                : all.OrderBy(t => t.Name).ToList(),
            _ => all.OrderBy(t => t.Name).ToList()
        };

        return PagedResult.From(sorted, request.Page, Math.Clamp(request.PageSize, 1, 100));
    }

    public async Task<List<MaterialTypeViewModel>> GetAll()
    {
        return await _context.MaterialTypes
            .Select(t => new MaterialTypeViewModel(t.Id.Value, t.Name, t.Active))
            .ToListAsync();
    }
}
