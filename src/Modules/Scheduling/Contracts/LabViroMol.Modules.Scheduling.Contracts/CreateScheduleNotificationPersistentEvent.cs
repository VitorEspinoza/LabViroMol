using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Scheduling.Contracts;

public record ScheduleEquipmentInfo(Guid EquipmentId, string Name);

public record CreateScheduleNotificationPersistentEvent(
    Guid ScheduleId,
    string SchedulerName,
    DateOnly Date,
    DateTimeOffset Start,
    DateTimeOffset End,
    List<ScheduleEquipmentInfo> Equipments) : IPersistentEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}
