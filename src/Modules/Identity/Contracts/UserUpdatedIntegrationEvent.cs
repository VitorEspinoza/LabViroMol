using System;
using System.Collections.Generic;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Identity.Contracts;

public record UserUpdatedIntegrationEvent(
    UserId UserId,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? EmergencyContactNumber,
    List<Guid> RoleIds,
    ResearchRegistrationData? ResearchData) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
