namespace LabViroMol.Modules.Inventory.Application.Shared;

public interface IProjectChecker
{
    Task<bool> IsEligibleForConsumptionAsync(Guid projectId, CancellationToken ct);
    Task<bool> IsEligibleForOrdersAsync(Guid projectId, CancellationToken ct);
}