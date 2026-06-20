using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Identity.Contracts;

public record UserUpdatedPersistentEvent(
    UserId UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? EmergencyContactName,
    string? EmergencyContactNumber,
    List<Guid> RoleIds,
    ResearchRegistrationData? ResearchData) : IPersistentEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
