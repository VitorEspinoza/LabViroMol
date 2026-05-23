using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Assets.Domain.Equipments;

public class Equipment : AggregateRoot<EquipmentId>
{
    private Equipment() {}
    
    public string Name { get; private set; }
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public string ImageUrl { get; private set; }

    private Equipment(EquipmentId id, UserId createdBy, string name, string brand, string model, string code, string description) : base(id, createdBy)
    {
        Name = name;
        Brand = brand;
        Model = model;
        Code = code;
        Description = description;
    }

    public static Result<Equipment> Create(UserId createdBy, string name, string brand, string model, string code,
        string description)
    {
        return new Equipment(
            IdFactory.New<EquipmentId>(),
            createdBy,
            name: name,
            brand: brand,
            model: model,
            code: code,
            description: description);
    }

    public void Update(string name, string brand, string model, string code, string description, UserId modifiedBy)
    {
        Name = name;
        Brand = brand;
        Model = model;
        Code = code; 
        Description = description;
        MarkAsUpdated(modifiedBy);
    }
    
    public void AttachImageUrl(string imageUrl)
    {
        ImageUrl = imageUrl;
    }
}