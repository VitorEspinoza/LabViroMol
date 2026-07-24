using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Identity.Contracts;

public record ResetPasswordPersistentEvent(
    string Email,
    string Subject,
    string Body) : IPersistentEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}