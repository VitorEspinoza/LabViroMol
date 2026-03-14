using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Shared.Abstractions.Identity;

public record struct UserId(Guid Value) : IStrongId<UserId>
{
    public static UserId From(Guid value) => new (value);
    
    public static UserId New() => new(Guid.CreateVersion7());
    
}


