using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Positions;

public class Position : AggregateRoot<PositionId>
{
    private Position() { }

    private Position(PositionId id, UserId createdBy, string name, string description)
        : base(id, createdBy)
    {
        Name = Guard.AgainstMinLength(name, 3, "O nome do cargo deve ter ao menos 3 caracteres.");
        Description = description;
    }

    public string Name { get; private set; }
    public string Description { get; private set; }

    public static Result<Position> Create(UserId createdBy, string name, string description)
    {
        var position = new Position(IdFactory.New<PositionId>(), createdBy, name, description);
        return Result<Position>.Success(position);
    }
}
