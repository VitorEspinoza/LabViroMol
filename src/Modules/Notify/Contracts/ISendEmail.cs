using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Notify.Contracts;

public interface ISendEmail
{
    Task SendEmail(
        string to, 
        string subject, 
        string htmlBody, 
        CancellationToken ct);
}