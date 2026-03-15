using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.MaterialTypes;

public class MaterialType : AggregateRoot<MaterialTypeId>
{
    private MaterialType() { }

    private MaterialType(MaterialTypeId id, UserId createdBy, string name) : base(id, createdBy)
    {
        Name = name;
        Active = true;
    }

    public string Name { get; private set; }
    public bool Active { get; private set; }
    public UserId? DeactivatedBy { get; private set; }
    public DateTimeOffset? DeactivatedAt { get; private set; }
    
    public static MaterialType Create(UserId createdBy, string name)
    {
        return new MaterialType(IdFactory.New<MaterialTypeId>(), createdBy, name);
    }

    public void Deactivate(UserId userId)
    {
        if (!Active) return;
        
        DeactivatedBy = userId;
        DeactivatedAt = DateTimeOffset.UtcNow;
        Active = false;
        MarkAsUpdated(userId);
    }

    public void Activate(UserId userId)
    {
        if (Active) return;
        Active = true;
        DeactivatedBy = null; 
        DeactivatedAt = null;

        MarkAsUpdated(userId);
    }
}
