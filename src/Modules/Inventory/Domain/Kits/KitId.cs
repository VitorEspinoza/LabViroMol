using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Kits;

public record struct KitId(Guid Value) : IStrongId<KitId>
{
    public static KitId From(Guid value) => new(value);

    public static implicit operator Guid(KitId id) => id.Value;
};
