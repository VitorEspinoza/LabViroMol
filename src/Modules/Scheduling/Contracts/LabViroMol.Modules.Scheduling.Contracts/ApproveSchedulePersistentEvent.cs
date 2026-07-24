using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Scheduling.Contracts;

public record ApproveSchedulePersistentEvent(
    string SchedulerEmail,
    string SchedulerName,
    string ProjectTitle,
    string AdvisorProfessor,
    DateOnly Date,
    DateTimeOffset Start,
    DateTimeOffset End) : IPersistentEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}