using Mediator;

namespace LabViroMol.Modules.Shared.Kernel.Messaging;

public interface IEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}
