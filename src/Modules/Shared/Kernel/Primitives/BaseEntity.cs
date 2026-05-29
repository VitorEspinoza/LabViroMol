using LabViroMol.Modules.Shared.Kernel.Identity;

namespace LabViroMol.Modules.Shared.Kernel.Primitives;
public abstract class BaseEntity<TId>
{
    protected BaseEntity(TId id) => Id = id;
    protected BaseEntity() { }
    public TId Id { get; protected set; }
}