using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Scheduling.Contracts;

public record CreateScheduleEmailPersistentEvent(
    string SchedulerEmail,
    string SchedulerName,
    string ProjectTitle,
    DateOnly Date,
    DateTimeOffset Start,
    DateTimeOffset End) : IPersistentEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}
