namespace LabViroMol.Modules.Research.Application.Positions.Commands.Delete;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public class DeletePositionHandler(
    IPositionRepository repository,
    ICurrentUser currentUser,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<DeletePositionCommand, Result>
{
    public async ValueTask<Result> Handle(DeletePositionCommand command, CancellationToken ct)
    {
        var position = await repository.GetByIdAsync(PositionId.From(command.PositionId), ct);
        if (position is null)
            return Result.NotFound("Cargo nao encontrado.");

        position.MarkAsRemoved(currentUser.Id);
        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
