using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;


namespace LabViroMol.Modules.Research.Domain.Partners;

public class Partner : AggregateRoot<PartnerId>
{
    private Partner() { }

    private Partner(PartnerId id, UserId createdBy, string name, string? description)
        : base(id, createdBy)
    {
        Name = Guard.AgainstMinLength(name, 3, "O nome do parceiro deve ter ao menos 3 caracteres.");
        Description = description;
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    public static Result<Partner> Create(UserId createdBy, string name, string? description)
    {
        var partner = new Partner(IdFactory.New<PartnerId>(), createdBy, name, description);
        return Result<Partner>.Success(partner);
    }

    public Result Update(string name, string? description, UserId modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
            return Result.BusinessRule("O nome do parceiro deve ter ao menos 3 caracteres.");

        Name = name;
        Description = description;
        MarkAsUpdated(modifiedBy);
        return Result.Success();
    }
}
