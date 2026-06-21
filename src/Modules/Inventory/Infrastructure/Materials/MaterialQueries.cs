using LabViroMol.Modules.Inventory.Application.Materials.Queries;
using LabViroMol.Modules.Inventory.Application.Materials.ViewModels;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Materials;

public class MaterialQueries : IMaterialQueries
{
    private readonly InventoryDbContext _context;

    public MaterialQueries(InventoryDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<PagedResponse<MaterialViewModel>> GetAllAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        var query = _context.Materials
            .Join(
                _context.MaterialTypes,
                m => m.TypeId,
                t => t.Id,
                (m, t) => new { Material = m, TypeName = t.Name });

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search;
            var matchingUnits = Enum.GetValues<Unit>()
                .Where(u => u.ToString().Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            query = query.Where(x =>
                x.Material.Name.Contains(search) ||
                x.TypeName.Contains(search) ||
                x.Material.Location.Contains(search) ||
                matchingUnits.Contains(x.Material.Unit));
        }

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Material.Name)
                : query.OrderBy(x => x.Material.Name),
            "location" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Material.Location)
                : query.OrderBy(x => x.Material.Location),
            "minstock" => request.SortDirection == "desc"
                ? query.OrderByDescending(x => x.Material.MinStock)
                : query.OrderBy(x => x.Material.MinStock),
            _ => query.OrderBy(x => x.Material.Name)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new MaterialViewModel(
                x.Material.Id.Value,
                x.Material.Name,
                x.TypeName,
                x.Material.MinStock.Value,
                x.Material.StockQuantity.Value,
                x.Material.Unit.ToString(),
                x.Material.Location))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<MaterialViewModel?> GetById(Guid id)
    {
        return await _context.Materials
            .Where(m => m.Id == MaterialId.From(id))
            .Join(
                _context.MaterialTypes,
                m => m.TypeId,
                t => t.Id,
                (m, t) => new MaterialViewModel(
                    m.Id.Value,
                    m.Name,
                    t.Name,
                    m.MinStock.Value,
                    m.StockQuantity.Value,
                    m.Unit.ToString(),
                    m.Location))
            .FirstOrDefaultAsync();
    }
}
