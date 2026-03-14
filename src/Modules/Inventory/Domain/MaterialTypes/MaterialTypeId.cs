using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.MaterialTypes;

public record struct MaterialTypeId(Guid Value) :  IStrongId<MaterialTypeId>
{
    
    public static MaterialTypeId From(Guid value) => new(value);
    
    public override string ToString() => Value.ToString();
    
    public static implicit operator Guid(MaterialTypeId id) => id.Value;
    
};

