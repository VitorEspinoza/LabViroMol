using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LabViroMol.Modules.Research.Application.Positions.EventHandlers;

public class PositionTranslationHandler : INotificationHandler<PositionTranslationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PositionTranslationHandler> _logger;

    public PositionTranslationHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<PositionTranslationHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    public async ValueTask Handle(PositionTranslationEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var repository = scope.ServiceProvider.GetRequiredService<IPositionRepository>();
            var translator = scope.ServiceProvider.GetRequiredService<ITextTranslator>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IResearchUnitOfWork>();

            var position = await repository.GetByIdAsync(notification.PositionId, CancellationToken.None);
            if (position is null)
            {
                return;
            }

            var translatedName = await translator.TranslateAsync("pt", "en", position.Name);
            var translatedDescription = await translator.TranslateAsync("pt", "en", position.Description);

            position.AddTranslation("en", translatedName, translatedDescription);

            await unitOfWork.CompleteAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro na tradução: {erro}", ex.Message);
        }
    }
}