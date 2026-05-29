using LabViroMol.Modules.Shared.Kernel.Identity;

namespace LabViroMol.Modules.Research.Application.External;

using LabViroMol.Modules.Shared.Kernel.Messaging;


public record UserUpdatedEvent(
    UserId RequestedBy, 
    UserId TargetUserId, 
    UserProfilePayload Data
) : IIntegrationEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}