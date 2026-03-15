using LabViroMol.Modules.Shared.Abstractions.Identity;

namespace LabViroMol.Modules.Shared.Abstractions.Primitives;
public abstract class BaseEntity<TId>
{
    protected BaseEntity(TId id) => Id = id;
    protected BaseEntity() { }
    public TId Id { get; protected set; }
}