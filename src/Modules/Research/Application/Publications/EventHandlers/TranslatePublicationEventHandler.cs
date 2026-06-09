using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Research.Application.Publications.EventHandlers;

public class TranslatePublicationEventHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TranslatePublicationEventHandler(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public ValueTask Handle(
        PublicationCreatedDomainEvent notification,
        CancellationToken ct)
    { 
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var repository =
                    scope.ServiceProvider
                        .GetRequiredService<IPublicationRepository>();

                var translator =
                    scope.ServiceProvider
                        .GetRequiredService<ITextTranslator>();

                var unitOfWork =
                    scope.ServiceProvider
                        .GetRequiredService<IResearchUnitOfWork>();

                var position =
                    await repository.GetByIdAsync(notification.PublicationId, ct);

                if (position is null)
                    return;

                var englishTitle =
                    await translator.TranslateAsync(
                        "pt",
                        "en",
                        position.Title);

                var englishDescription =
                    await translator.TranslateAsync(
                        "pt",
                        "en",
                        position.Description);

                position.AddTranslation(
                    "en",
                    englishTitle,
                    englishDescription);

                await unitOfWork.CompleteAsync(
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                // log
            }
        });

        return ValueTask.CompletedTask;
    }
}