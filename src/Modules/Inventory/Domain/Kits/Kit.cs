
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.Kits;

public class Kit : AggregateRoot<KitId>, ICreationAuditable, IModificationAuditable
{
    private Kit(KitId id, string name, string description)
        : base(id)
    {
        Name = name;
        Description = description;
    }

    private Kit() { }
    public string Name { get; private set; }
    public string Description { get; private set; }

    public DateTimeOffset CreatedAt { get; protected set; }
    public UserId CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public UserId? UpdatedBy { get; protected set; }

    private readonly List<KitItem> _materials = new();
    public IReadOnlyCollection<KitItem> Materials => _materials.AsReadOnly();

    public static Kit Create(string name, string description, List<KitItem> initialItems)
    {
        if (initialItems == null || initialItems.Count == 0)
            throw new DomainException("Um kit deve conter pelo menos um material.");

        var kit = new Kit(IdFactory.New<KitId>(), name, description);

        kit.DefineMaterials(initialItems);

        return kit;
    }

    public void UpdateMetadata(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public void DefineMaterials(IEnumerable<KitItem> newItems)
    {
        var itemsToRemove = _materials.Where(existing => newItems.All(item => item.MaterialId != existing.MaterialId)).ToList();

        foreach (var item in itemsToRemove)
            _materials.Remove(item);

        foreach (var newItem in newItems)
        {
            var existingItem = _materials.FirstOrDefault(x => x.MaterialId == newItem.MaterialId);

            if (existingItem == null)
            {
                _materials.Add(newItem);
            }
            else if (existingItem.Quantity != newItem.Quantity)
            {
                _materials.Remove(existingItem);
                _materials.Add(newItem);
            }
        }
    }
}
