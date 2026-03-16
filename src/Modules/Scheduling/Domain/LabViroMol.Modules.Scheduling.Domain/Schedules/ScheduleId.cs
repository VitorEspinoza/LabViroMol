using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public record struct ScheduleId(Guid Value) : IStrongId<ScheduleId>
{
    public static ScheduleId New() => new(Guid.CreateVersion7());
    public static ScheduleId From(Guid value) => new(value);

    public static implicit operator Guid(ScheduleId id) => id.Value;
};