using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Inventory.Domain.MaterialTypes;

public interface IMaterialTypeRepository
{
    Task<MaterialType?> GetByIdAsync(MaterialTypeId id, CancellationToken ct);
    Task AddAsync(MaterialType materialType, CancellationToken ct);
}
