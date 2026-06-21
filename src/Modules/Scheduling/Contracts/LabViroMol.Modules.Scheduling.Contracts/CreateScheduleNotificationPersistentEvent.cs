using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Scheduling.Contracts;

public record CreateScheduleNotificationPersistentEvent(
    ScheduleId ScheduleId,
    string SchedulerName,
    DateOnly Date,
    DateTimeOffset Start,
    DateTimeOffset End,
    List<ScheduleEquipment> Equipments) : IPersistentEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}