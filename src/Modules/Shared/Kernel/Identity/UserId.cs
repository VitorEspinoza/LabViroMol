using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Shared.Kernel.Identity;

public record struct UserId(Guid Value) : IStrongId<UserId>
{
    public static UserId From(Guid value) => new(value);

}


