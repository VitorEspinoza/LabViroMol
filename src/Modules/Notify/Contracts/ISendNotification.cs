using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Notify.Contracts;

public interface ISendNotification
{
    Task<Result> SendNotification(
        string title,
        string message,
        string referenceId,
        string referenceModule,
        string type,
        string permissionId,
        CancellationToken ct);
}