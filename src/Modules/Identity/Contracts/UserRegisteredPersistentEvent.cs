using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Identity.Contracts;

public record UserRegisteredPersistentEvent(
    UserId UserId,
    string Email,
    string FirstName,
    string LastName,
    List<Guid> RoleIds,
    ResearchRegistrationData? ResearchData) : IPersistentEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
