using LabViroMol.Modules.Inventory.Application.Materials.ViewModels;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Materials;

public class MaterialQueries
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

        IQueryable<MaterialViewModel> query = _context.Materials
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
                    m.Location));

        query = query.WhereSearch(request.Search, x => x.Name, x => x.MaterialType, x => x.Unit, x => x.Location);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == "desc"
                ? query.OrderByDescending(m => m.Name)
                : query.OrderBy(m => m.Name),
            "location" => request.SortDirection == "desc"
                ? query.OrderByDescending(m => m.Location)
                : query.OrderBy(m => m.Location),
            "minstock" => request.SortDirection == "desc"
                ? query.OrderByDescending(m => m.MinStock)
                : query.OrderBy(m => m.MinStock),
            _ => query.OrderBy(m => m.Name)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

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
