
namespace LabViroMol.Modules.Inventory.Domain.Materials;

public interface IMaterialRepository
{
    Task<Material?> GetByIdAsync(MaterialId id, CancellationToken ct);
    Task AddAsync(Material material, CancellationToken ct);
    
    Task<IReadOnlyCollection<MaterialId>> GetExistingIdsAsync(IEnumerable<MaterialId> ids, CancellationToken ct);
}
