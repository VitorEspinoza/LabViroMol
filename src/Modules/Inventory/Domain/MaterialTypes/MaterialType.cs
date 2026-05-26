using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.MaterialTypes;

public class MaterialType : AggregateRoot<MaterialTypeId>, ICreationAuditable, IModificationAuditable
{
    private MaterialType() { }

    private MaterialType(MaterialTypeId id, string name) : base(id)
    {
        Name = name;
        Active = true;
    }

    public string Name { get; private set; }
    public bool Active { get; private set; }
    public UserId? DeactivatedBy { get; private set; }
    public DateTimeOffset? DeactivatedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; protected set; }
    public UserId CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public UserId? UpdatedBy { get; protected set; }

    public static MaterialType Create(string name)
    {
        return new MaterialType(IdFactory.New<MaterialTypeId>(), name);
    }

    public void Deactivate(UserId userId)
    {
        if (!Active) return;

        DeactivatedBy = userId;
        DeactivatedAt = DateTimeOffset.UtcNow;
        Active = false;
    }

    public void Activate(UserId userId)
    {
        if (Active) return;
        Active = true;
        DeactivatedBy = null;
        DeactivatedAt = null;
    }
}
