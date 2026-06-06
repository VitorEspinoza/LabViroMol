using LabViroMol.Modules.Inventory.Application.Kits.ViewModels;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Kits;

public class KitQueries
{
    private readonly InventoryDbContext _context;

    public KitQueries(InventoryDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<PagedResponse<KitViewModel>> GetAllAsync(PagedRequest request)
    {
        var all = await _context.Kits
            .Select(kit => new KitViewModel(
                kit.Id.Value,
                kit.Name,
                kit.Description,
                kit.Materials.Join(
                    _context.Materials,
                    item => item.MaterialId,
                    material => material.Id,
                    (item, material) => new KitItemViewModel(
                        item.MaterialId.Value,
                        material.Name,
                        item.Quantity.Value,
                        material.Unit.ToString())
                ).ToList()
            )).ToListAsync();

        var sorted = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == "desc"
                ? all.OrderByDescending(k => k.Name).ToList()
                : all.OrderBy(k => k.Name).ToList(),
            _ => all.OrderBy(k => k.Name).ToList()
        };

        return PagedResult.From(sorted, request.Page, Math.Clamp(request.PageSize, 1, 100));
    }

    public async Task<List<KitViewModel>> GetAllKits()
    {
        return await _context.Kits
            .Select(kit => new KitViewModel(
                kit.Id.Value,
                kit.Name,
                kit.Description,
                kit.Materials.Join(
                    _context.Materials,
                    item => item.MaterialId,
                    material => material.Id,
                    (item, material) => new KitItemViewModel(
                        item.MaterialId.Value,
                        material.Name,
                        item.Quantity.Value,
                        material.Unit.ToString())
                ).ToList()
            )).ToListAsync();
    }

    public async Task<KitViewModel?> GetKitById(Guid id)
    {
        return await _context.Kits
            .Where(k => k.Id == KitId.From(id))
            .Select(kit => new KitViewModel(
                kit.Id.Value,
                kit.Name,
                kit.Description,
                kit.Materials.Join(
                    _context.Materials,
                    item => item.MaterialId,
                    material => material.Id,
                    (item, material) => new KitItemViewModel(
                        item.MaterialId.Value,
                        material.Name,
                        item.Quantity.Value,
                        material.Unit.ToString())).ToList()
            ))
            .FirstOrDefaultAsync();
    }
}
