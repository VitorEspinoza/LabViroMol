using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Inventory.Infrastructure.Kits;

internal sealed class KitRepository : IKitRepository
{
    private readonly InventoryDbContext _context;

    public KitRepository(InventoryDbContext context)
    {
        _context = context;
    }
    public async Task<Kit?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Kits
            .FirstOrDefaultAsync(k => k.Id == KitId.From(id), cancellationToken);
    }

    public async Task AddAsync(Kit kit, CancellationToken cancellationToken)
    {
        await _context.Kits.AddAsync(kit, cancellationToken);
    }
}
