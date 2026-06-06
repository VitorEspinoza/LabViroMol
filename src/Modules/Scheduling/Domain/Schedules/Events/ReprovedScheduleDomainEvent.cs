using LabViroMol.Modules.Shared.Kernel.Messaging;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules.Events;

public record ReprovedScheduleDomainEvent(Schedule Schedule, string Justification) : IDomainEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}