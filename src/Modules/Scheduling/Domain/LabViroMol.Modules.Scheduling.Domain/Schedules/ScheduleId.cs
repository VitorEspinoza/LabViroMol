using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public record struct ScheduleId(Guid Value) : IStrongId<ScheduleId>
{
    public static ScheduleId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();

    public static implicit operator Guid(ScheduleId id) => id.Value;
};