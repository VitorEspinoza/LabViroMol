using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LabViroMol.Modules.Research.Application.Publications.EventHandlers;

public class PublicationTranslationHandler : INotificationHandler<PublicationTranslationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PublicationTranslationHandler> _logger;

    public PublicationTranslationHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<PublicationTranslationHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async ValueTask Handle(PublicationTranslationEvent notification, CancellationToken cancellationToken)
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
                await repository.GetByIdAsync(notification.PublicationId, CancellationToken.None);

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
            _logger.LogError("Erro na tradução: {erro}", ex.Message);
        }
    }
}