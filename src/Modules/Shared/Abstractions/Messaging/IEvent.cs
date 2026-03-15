using Mediator;

namespace LabViroMol.Modules.Shared.Abstractions.Messaging;

public interface IEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}
