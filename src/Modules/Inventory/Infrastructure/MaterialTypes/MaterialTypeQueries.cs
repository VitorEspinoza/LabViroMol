using LabViroMol.Modules.Inventory.Application.MaterialTypes.ViewModels;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
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

    public async Task<List<MaterialTypeViewModel>> GetAll()
    {
        return await _context.MaterialTypes
            .Select(t => new MaterialTypeViewModel(t.Id.Value, t.Name, t.Active))
            .ToListAsync();
    }
}
