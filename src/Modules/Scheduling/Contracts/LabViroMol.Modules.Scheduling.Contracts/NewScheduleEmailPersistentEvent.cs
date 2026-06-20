using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Scheduling.Contracts;

public record NewScheduleEmailPersistentEvent(
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