namespace LabViroMol.Modules.Research.Application.Positions.Commands.Create;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public class CreatePositionHandler(
    IPositionRepository repository,
    ICurrentUser currentUser,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<CreatePositionCommand, Result>
{
    public async ValueTask<Result> Handle(CreatePositionCommand command, CancellationToken ct)
    {
        var result = Position.Create(currentUser.Id, command.Name, command.Description);
        if (result.IsFailure)
            return result;

        await repository.AddAsync(result.Data!, ct);
        await unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
