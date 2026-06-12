using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LabViroMol.Modules.Assets.Application.Equipments.EventHandlers;

public class EquipmentTranslationHandler : INotificationHandler<EquipmentTranslationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EquipmentTranslationHandler> _logger;

    public EquipmentTranslationHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<EquipmentTranslationHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    public async ValueTask Handle(EquipmentTranslationEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var repository =
                scope.ServiceProvider
                    .GetRequiredService<IEquipmentRepository>();

            var translator =
                scope.ServiceProvider
                    .GetRequiredService<ITextTranslator>();

            var unitOfWork =
                scope.ServiceProvider
                    .GetRequiredService<IAssetsUnitOfWork>();

            var equipment =
                await repository.GetByIdAsync(notification.EquipmentId, CancellationToken.None);

            if (equipment is null)
                return;

            var englishTitle =
                await translator.TranslateAsync(
                    "pt",
                    "en",
                    equipment.Name);

            var englishDescription =
                await translator.TranslateAsync(
                    "pt",
                    "en",
                    equipment.Description);

            equipment.AddTranslation(
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