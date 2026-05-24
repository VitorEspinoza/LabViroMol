using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Research.Application.External;


public record UserRegisteredEvent(
    UserId RequestedBy, 
    UserId TargetUserId,
    UserProfilePayload Data
) : IIntegrationEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}