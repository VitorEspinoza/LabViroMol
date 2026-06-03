using System;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Assets.Domain.Equipments;

public record struct EquipmentId(Guid Value) : IStrongId<EquipmentId>
{
    public static EquipmentId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(EquipmentId id) => id.Value;
}
