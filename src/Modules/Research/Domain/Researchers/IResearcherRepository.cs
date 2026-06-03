using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Research.Domain.Researchers;

using LabViroMol.Modules.Shared.Kernel.Identity;

public interface IResearcherRepository
{
    Task<Researcher?> GetByIdAsync(ResearcherId id, CancellationToken ct);
    Task<Researcher?> GetByUserIdAsync(UserId userId, CancellationToken ct);
    Task AddAsync(Researcher researcher, CancellationToken ct);
    void Delete(Researcher researcher);
}
