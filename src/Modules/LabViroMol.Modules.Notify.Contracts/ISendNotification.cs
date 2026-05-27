using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Notify.Contracts;

public interface ISendNotification
{
    Task<Result> SendNotification(string title, string message, string permissionId, CancellationToken ct);
}