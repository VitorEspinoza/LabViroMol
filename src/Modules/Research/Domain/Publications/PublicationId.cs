namespace LabViroMol.Modules.Research.Domain.Publications;

using LabViroMol.Modules.Shared.Abstractions.Primitives;

public record struct PublicationId(Guid Value) : IStrongId<PublicationId>
{
    public static PublicationId New() => new(Guid.CreateVersion7());
    public static PublicationId From(Guid value) => new(value);
    public static implicit operator Guid(PublicationId id) => id.Value;
}
