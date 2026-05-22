using LabViroMol.Modules.Shared.Abstractions.Identity;

namespace LabViroMol.Modules.Research.Application.External;

using LabViroMol.Modules.Shared.Abstractions.Messaging;


public record UserUpdatedEvent(
    UserId RequestedBy, 
    UserId TargetUserId, 
    UserProfilePayload Data
) : IIntegrationEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}