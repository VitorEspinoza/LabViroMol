namespace LabViroMol.Modules.Inventory.Domain.Kits;

public interface IKitRepository
{
    Task<Kit?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Kit kit, CancellationToken cancellationToken);
}
