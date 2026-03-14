using LabViroMol.Modules.Inventory.Application.Shared;

namespace LabViroMol.Modules.Inventory.Infrastructure.External;

public class ProjectCheckerMock : IProjectChecker
{
    public Task<bool> IsEligibleForConsumptionAsync(Guid projectId, CancellationToken ct)
    {
        return Task.FromResult(true);
    }

    public Task<bool> IsEligibleForOrdersAsync(Guid projectId, CancellationToken ct)
    {
        return Task.FromResult(true);
    }
}