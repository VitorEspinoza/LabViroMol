using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Messaging;

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