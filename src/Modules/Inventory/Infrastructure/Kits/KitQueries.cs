using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabViroMol.Modules.Inventory.Application.Kits.ViewModels;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
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
    public async Task<List<KitViewModel>> GetAllKits()
    {
        var kits = await _context.Kits
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

        return kits;
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
