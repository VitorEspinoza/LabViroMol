namespace LabViroMol.Modules.Research.Domain.Positions;

using LabViroMol.Modules.Shared.Kernel.Primitives;

public record struct PositionId(Guid Value) : IStrongId<PositionId>
{
    public static PositionId New() => new(Guid.CreateVersion7());
    public static PositionId From(Guid value) => new(value);
    public static implicit operator Guid(PositionId id) => id.Value;
}
