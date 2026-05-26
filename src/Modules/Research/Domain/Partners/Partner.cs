using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;


namespace LabViroMol.Modules.Research.Domain.Partners;

public class Partner : AggregateRoot<PartnerId>, IFullAuditable
{
    private Partner() { }

    private Partner(PartnerId id, string name, string? description)
        : base(id)
    {
        Name = Guard.AgainstMinLength(name, 3, "O nome do parceiro deve ter ao menos 3 caracteres.");
        Description = description;
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    public DateTimeOffset CreatedAt { get; protected set; }
    public UserId CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public UserId? UpdatedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? RemovedAt { get; protected set; }
    public UserId? RemovedBy { get; protected set; }

    public static Result<Partner> Create(string name, string? description)
    {
        var partner = new Partner(IdFactory.New<PartnerId>(), name, description);
        return Result<Partner>.Success(partner);
    }

    public Result Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
            return Result.BusinessRule("O nome do parceiro deve ter ao menos 3 caracteres.");

        Name = name;
        Description = description;
        return Result.Success();
    }
}
