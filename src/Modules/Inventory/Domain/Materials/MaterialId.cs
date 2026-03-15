using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Materials;

public record struct MaterialId(Guid Value) :  IStrongId<MaterialId>
{
    public static MaterialId New() => new(Guid.CreateVersion7());
    
    public static MaterialId From(Guid value) => new(value);

    public static implicit operator Guid(MaterialId id) => id.Value;
};
