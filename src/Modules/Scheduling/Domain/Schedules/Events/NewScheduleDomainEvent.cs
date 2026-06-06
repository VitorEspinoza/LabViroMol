using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules.Events;

public record NewScheduleDomainEvent(Schedule Schedule) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
