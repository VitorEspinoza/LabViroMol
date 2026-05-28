using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Contracts;

public interface IProjectChecker
{
    Task<Result> IsEligibleForConsumptionAsync(Guid projectId, UserId projectMemberId, CancellationToken ct);
    Task<Result> IsEligibleForOrdersAsync(Guid projectId, CancellationToken ct);
}