using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Materials;

public class MaterialRepository : IMaterialRepository
{
    private readonly InventoryDbContext _context;

    public MaterialRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<Material?> GetByIdAsync(MaterialId id, CancellationToken ct)
    {
        return await _context.Materials
            .FirstOrDefaultAsync(m => m.Id == id, ct);
     
    }

    public async Task AddAsync(Material material, CancellationToken ct)
    {
        await _context.Materials.AddAsync(material, ct);
    }

    public async Task<IReadOnlyCollection<MaterialId>> GetExistingIdsAsync(IEnumerable<MaterialId> ids, CancellationToken ct)
    {
        return await _context.Materials
            .Where(m => ids.Contains(m.Id))
            .Select(m => m.Id)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyCollection<MaterialId>)t.Result, ct);
    }
}
