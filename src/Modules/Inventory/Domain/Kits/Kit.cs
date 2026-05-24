
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Kits;

public class Kit : AggregateRoot<KitId>
{
    private Kit(KitId id, UserId createdBy, string name, string description)
        : base(id, createdBy)
    {
        Name = name;
        Description = description;
    }
    
    private Kit() { }
    public string Name { get; private set; }
    public string Description { get; private set; }

    private readonly List<KitItem> _materials = new();
    public IReadOnlyCollection<KitItem> Materials => _materials.AsReadOnly();
    
    public static Kit Create(UserId createdBy, string name, string description, List<KitItem> initialItems)
    {
        if (initialItems == null || initialItems.Count == 0)
            throw new DomainException("Um kit deve conter pelo menos um material.");

        var kit = new Kit(IdFactory.New<KitId>(), createdBy, name, description);
    
        kit.DefineMaterials(initialItems, createdBy);
        
        return kit;
    }
    
    public void UpdateMetadata(string name, string description, UserId modifiedBy)
    {
        Name = name;
        Description = description;
        MarkAsUpdated(modifiedBy);
    }

    public void DefineMaterials(IEnumerable<KitItem> newItems, UserId modifiedBy)
    {
        var hasChanged = false;
        var itemsToRemove = _materials.Where(existing => newItems.All(item => item.MaterialId != existing.MaterialId)).ToList();

        if (itemsToRemove.Count != 0)
        {
            foreach (var item in itemsToRemove)
                _materials.Remove(item);
            hasChanged = true;
        }
       

        foreach (var newItem in newItems)
        {
            var existingItem = _materials.FirstOrDefault(x => x.MaterialId == newItem.MaterialId);

            if (existingItem == null)
            {
                _materials.Add(newItem);
                hasChanged = true;
                
            }
            else if (existingItem.Quantity != newItem.Quantity)
            {
                _materials.Remove(existingItem);
                _materials.Add(newItem);
                hasChanged = true;
            }
        }
        
        if(hasChanged)
            MarkAsUpdated(modifiedBy);

    }
}
