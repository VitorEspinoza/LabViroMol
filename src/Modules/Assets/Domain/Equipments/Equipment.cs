using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Assets.Domain.Equipments;

public class Equipment : AggregateRoot<EquipmentId>, IFullAuditable
{
    private Equipment() {}

    public string Name { get; private set; }
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public string ImageUrl { get; private set; }

    private Equipment(EquipmentId id, string name, string brand, string model, string code, string description) : base(id)
    {
        Name = name;
        Brand = brand;
        Model = model;
        Code = code;
        Description = description;
    }

    public static Result<Equipment> Create(string name, string brand, string model, string code,
        string description)
    {
        return new Equipment(
            IdFactory.New<EquipmentId>(),
            name: name,
            brand: brand,
            model: model,
            code: code,
            description: description);
    }

    public void Update(string name, string brand, string model, string code, string description)
    {
        Name = name;
        Brand = brand;
        Model = model;
        Code = code;
        Description = description;
    }

    public void AttachImageUrl(string imageUrl)
    {
        ImageUrl = imageUrl;
    }
}
