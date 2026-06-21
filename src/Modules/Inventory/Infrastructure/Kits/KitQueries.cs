using LabViroMol.Modules.Inventory.Application.Kits.Queries;
using LabViroMol.Modules.Inventory.Application.Kits.ViewModels;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Kits;

public class KitQueries : IKitQueries
{
    private readonly InventoryDbContext _context;

    public KitQueries(InventoryDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<PagedResponse<KitViewModel>> GetAllAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Kit> query = _context.Kits;

        query = query.WhereSearch(request.Search, x => x.Name, x => x.Description);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDirection == "desc"
                ? query.OrderByDescending(k => k.Name)
                : query.OrderBy(k => k.Name),
            _ => query.OrderBy(k => k.Name)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
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
            ))
            .ToListAsync();

        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
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
