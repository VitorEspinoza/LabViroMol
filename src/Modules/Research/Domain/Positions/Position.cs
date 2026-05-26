using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Positions;

public class Position : AggregateRoot<PositionId>, ICreationAuditable, IDeletionAuditable
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

    public DateTimeOffset CreatedAt { get; protected set; }
    public UserId CreatedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? RemovedAt { get; protected set; }
    public UserId? RemovedBy { get; protected set; }

    public static Result<Position> Create(string name, string description)
    {
        var position = new Position(IdFactory.New<PositionId>(), name, description);
        return Result<Position>.Success(position);
    }
}
