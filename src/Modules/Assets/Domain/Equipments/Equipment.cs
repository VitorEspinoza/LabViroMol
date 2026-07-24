using LabViroMol.Modules.Assets.Domain.Equipments.Events;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Assets.Domain.Equipments;

public class Equipment : AggregateRoot<EquipmentId>, IFullAuditable, ITranslatable<EquipmentTranslation>
{
    private Equipment() { }

    public string Name { get; private set; }
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Location { get; private set; }
    public Dictionary<string, EquipmentTranslation> Translations { get; private set; } = new();

    private Equipment(EquipmentId id, string name, string brand, string model, string code, string description, string? location = null) : base(id)
    {
        Name = name;
        Brand = brand;
        Model = model;
        Code = code;
        Description = description;
        Location = location;
    }

    public void AddTranslation(string languageCode, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(languageCode)) return;

        Translations[languageCode.ToLower()] = new EquipmentTranslation(name, description);
    }

    public static Result<Equipment> Create(string name, string brand, string model, string code,
        string description, string? location = null)
    {
        return new Equipment(
            IdFactory.New<EquipmentId>(),
            name: name,
            brand: brand,
            model: model,
            code: code,
            description: description,
            location: location);
    }

    public void Update(string name, string brand, string model, string description, string? location = null)
    {
        Name = name;
        Brand = brand;
        Model = model;
        Description = description;
        Location = location;
    }

    public void AttachImageUrl(string imageUrl)
    {
        ImageUrl = imageUrl;
    }

    public void Delete()
    {
        AddEvent(new EquipmentDeletedDomainEvent(Id));
    }

    public string GetName(string? language)
    {
        if (language == "en"
            && Translations.TryGetValue("en", out var translation)
            && !string.IsNullOrWhiteSpace(translation.Name))
        {
            return translation.Name;
        }

        return Name;
    }

    public string GetDescription(string? language)
    {
        if (language == "en"
            && Translations.TryGetValue("en", out var translation)
            && !string.IsNullOrWhiteSpace(translation.Description))
        {
            return translation.Description;
        }

        return Description;
    }
}
