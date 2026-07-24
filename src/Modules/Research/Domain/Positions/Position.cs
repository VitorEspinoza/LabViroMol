using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Positions;

public class Position : AggregateRoot<PositionId>, ICreationAuditable, IDeletionAuditable, ITranslatable<PositionTranslation>
{
    private Position() { }

    private Position(PositionId id, string name, string description)
        : base(id)
    {
        Name = Guard.AgainstMinLength(name, 3, "O nome do cargo deve ter ao menos 3 caracteres.");
        Description = description;
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public Dictionary<string, PositionTranslation> Translations { get; private set; } = new();

    public static Result<Position> Create(string name, string description)
    {
        var position = new Position(IdFactory.New<PositionId>(), name, description);
        return Result<Position>.Success(position);
    }

    public void AddTranslation(string languageCode, string name, string description)
    {
        if (string.IsNullOrWhiteSpace(languageCode)) return;

        Translations[languageCode.ToLower()] = new PositionTranslation(name, description);
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
