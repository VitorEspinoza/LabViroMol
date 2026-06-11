using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Research.Application.Positions.Commands.Create;

using Shared;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using EventHandlers;
using Mediator;

public class CreatePositionHandler : ICommandHandler<CreatePositionCommand, Result>
{
    private readonly IPositionRepository _repository;
    private readonly IResearchUnitOfWork _unitOfWork;
    private readonly IServiceScopeFactory _scopeFactory;

    public CreatePositionHandler(
        IPositionRepository repository,
        IResearchUnitOfWork unitOfWork,
        IServiceScopeFactory scopeFactory)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _scopeFactory = scopeFactory;
    }
    
    public async ValueTask<Result> Handle(CreatePositionCommand command, CancellationToken ct)
    {
        var result = Position.Create(command.Name, command.Description);
        if (result.IsFailure)
            return result;
        var position = result.Data!;

        await _repository.AddAsync(position, ct);
        await _unitOfWork.CompleteAsync(ct);

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();

            var publisher =
                scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.Publish(
                new PositionTranslationEvent(position.Id),
                CancellationToken.None);
        });

        return Result.Success();
    }
}