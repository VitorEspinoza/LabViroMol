using System.Diagnostics;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LabViroMol.Modules.Research.Application.Positions.Commands.Create;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class CreatePositionHandler(
    IPositionRepository repository,
    IResearchUnitOfWork unitOfWork,
    IBackgroundJobQueue backgroundJobQueue,
    ILogger<CreatePositionHandler> _logger)
    : ICommandHandler<CreatePositionCommand, Result>
{
    public async ValueTask<Result> Handle(CreatePositionCommand command, CancellationToken ct)
    {
        var result = Position.Create(command.Name, command.Description);
        if (result.IsFailure)
            return result;
        
        var sw = Stopwatch.StartNew();

        await repository.AddAsync(result.Data!, ct);

        _logger.LogInformation(
            "AddAsync: {ms}",
            sw.ElapsedMilliseconds);

        sw.Restart();

        await unitOfWork.CompleteAsync(ct);

        _logger.LogInformation(
            "CompleteAsync: {ms}",
            sw.ElapsedMilliseconds);

        sw.Restart();

        await backgroundJobQueue.QueueAsync(
            async (_, _) => { });

        _logger.LogInformation(
            "QueueAsync: {ms}",
            sw.ElapsedMilliseconds);

        return Result.Success();
    }
}
