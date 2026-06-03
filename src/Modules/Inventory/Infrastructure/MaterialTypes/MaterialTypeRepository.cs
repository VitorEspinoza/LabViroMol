using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.MaterialTypes;

public class MaterialTypeRepository : IMaterialTypeRepository
{
    private readonly InventoryDbContext _context;

    public MaterialTypeRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<MaterialType?> GetByIdAsync(MaterialTypeId id, CancellationToken ct)
    {
        return await _context.MaterialTypes
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task AddAsync(MaterialType materialType, CancellationToken ct)
    {
        await _context.MaterialTypes.AddAsync(materialType, ct);
    }
}
