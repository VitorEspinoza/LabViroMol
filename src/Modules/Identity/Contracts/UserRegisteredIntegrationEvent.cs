using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Identity.Contracts;

public record UserRegisteredIntegrationEvent(
    UserId UserId,
    string Email,
    string FirstName,
    string LastName,
    List<Guid> RoleIds,
    ResearchRegistrationData? ResearchData) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
