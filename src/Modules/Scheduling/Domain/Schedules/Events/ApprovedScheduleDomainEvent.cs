using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules.Events;

public record ApprovedScheduleDomainEvent(Schedule Schedule) : IDomainEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}