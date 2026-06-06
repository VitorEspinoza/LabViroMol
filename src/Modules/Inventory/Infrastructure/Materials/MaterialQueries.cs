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
        var all = await _context.Materials
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
            .ToListAsync();

        var sorted = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == "desc"
                ? all.OrderByDescending(m => m.Name).ToList()
                : all.OrderBy(m => m.Name).ToList(),
            "location" => request.SortDirection == "desc"
                ? all.OrderByDescending(m => m.Location).ToList()
                : all.OrderBy(m => m.Location).ToList(),
            "minstock" => request.SortDirection == "desc"
                ? all.OrderByDescending(m => m.MinStock).ToList()
                : all.OrderBy(m => m.MinStock).ToList(),
            _ => all.OrderBy(m => m.Name).ToList()
        };

        return PagedResult.From(sorted, request.Page, Math.Clamp(request.PageSize, 1, 100));
    }

    public async Task<List<MaterialViewModel>> GetAll()
    {
        return await _context.Materials
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
            .ToListAsync();
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
