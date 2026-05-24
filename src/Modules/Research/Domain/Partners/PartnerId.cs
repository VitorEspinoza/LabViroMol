using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Partners;

public record struct PartnerId(Guid Value) : IStrongId<PartnerId>
{
    public static PartnerId From(Guid value) => new(value);
    public static implicit operator Guid(PartnerId id) => id.Value;
}
