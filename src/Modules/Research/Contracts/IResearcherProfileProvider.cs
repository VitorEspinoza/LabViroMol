using LabViroMol.Modules.Identity.Contracts;

namespace LabViroMol.Modules.Research.Contracts;

public interface IResearcherProfileProvider
{
    Task<ResearchRegistrationData?> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<Dictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct);
}
