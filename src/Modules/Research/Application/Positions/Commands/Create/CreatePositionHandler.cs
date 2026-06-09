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
    ILogger<CreatePositionHandler> logger)
    : ICommandHandler<CreatePositionCommand, Result>
{
    public async ValueTask<Result> Handle(CreatePositionCommand command, CancellationToken ct)
    {
        _logger.LogInformation(
            "Tracked entities: {count}",
            context.ChangeTracker.Entries().Count());

        var sw = Stopwatch.StartNew();

        context.Positions.Add(position);

        _logger.LogInformation(
            "Add: {ms}",
            sw.ElapsedMilliseconds);

        sw.Restart();

        context.ChangeTracker.DetectChanges();

        _logger.LogInformation(
            "DetectChanges: {ms}",
            sw.ElapsedMilliseconds);

        await repository.AddAsync(result.Data!, ct);

        logger.LogInformation(
            "Repository AddAsync took {ms}",
            sw.ElapsedMilliseconds);

        sw.Restart();

        await unitOfWork.CompleteAsync(ct);

        logger.LogInformation(
            "CompleteAsync took {ms}",
            sw.ElapsedMilliseconds);
        
        await backgroundJobQueue.QueueAsync(
            async (sp, token) =>
            {
                var repository =
                    sp.GetRequiredService<IPositionRepository>();

                var translator =
                    sp.GetRequiredService<ITextTranslator>();

                var unitOfWork =
                    sp.GetRequiredService<IResearchUnitOfWork>();

                var position =
                    await repository.GetByIdAsync(
                        result.Data!.Id,
                        token);

                if (position is null)
                    return;

                var englishName =
                    await translator.TranslateAsync(
                        "pt",
                        "en",
                        position.Name);

                var englishDescription =
                    await translator.TranslateAsync(
                        "pt",
                        "en",
                        position.Description);

                position.AddTranslation(
                    "en",
                    englishName,
                    englishDescription);

                await unitOfWork.CompleteAsync(token);
            });

        return Result.Success();
    }
}
