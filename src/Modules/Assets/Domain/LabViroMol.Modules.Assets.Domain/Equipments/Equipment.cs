using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Assets.Domain.Equipments;

public class Equipment : AggregateRoot<EquipmentId>
{
    private Equipment() {}
    
    public string Name { get; private set; }
    public Quantity Quantity { get; private set; }
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }

    private Equipment(string name, decimal quantity, string brand, string model, string code, string description)
    {
        Name = name;
        Quantity = new Quantity(quantity);
        Brand = brand;
        Model = model;
        Code = code;
        Description = description;
    }

    public static Result<Equipment> Create(string name, decimal quantity, string brand, string model, string code,
        string description)
    {
        return new Equipment(
            name: name,
            quantity: quantity,
            brand: brand,
            model: model,
            code: code,
            description: description);
    }
}