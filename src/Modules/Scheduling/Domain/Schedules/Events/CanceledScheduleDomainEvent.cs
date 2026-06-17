using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules.Events;

public record CanceledScheduleDomainEvent(Schedule Schedule, string Justification) : IDomainEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}