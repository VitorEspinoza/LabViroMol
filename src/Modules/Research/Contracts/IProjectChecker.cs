using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Research.Contracts;

public interface IProjectChecker
{
    Task<Result> IsEligibleForConsumptionAsync(Guid projectId, UserId projectMemberId, CancellationToken ct);
    Task<Result> IsEligibleForOrdersAsync(Guid projectId, CancellationToken ct);
}