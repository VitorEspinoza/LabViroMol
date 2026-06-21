namespace LabViroMol.Modules.Research.Application.Positions.Commands.Create;

using Shared;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class CreatePositionHandler : ICommandHandler<CreatePositionCommand, Result>
{
    private readonly IPositionRepository _repository;
    private readonly IResearchUnitOfWork _unitOfWork;

    public CreatePositionHandler(
        IPositionRepository repository,
        IResearchUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    
    public async ValueTask<Result> Handle(CreatePositionCommand command, CancellationToken ct)
    {
        var result = Position.Create(command.Name, command.Description);
        if (result.IsFailure)
            return result;
        var position = result.Data!;

        await _repository.AddAsync(position, ct);
        
        _unitOfWork.AddPersistentEvent(new PositionTranslationPersistentEvent());
        
        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}