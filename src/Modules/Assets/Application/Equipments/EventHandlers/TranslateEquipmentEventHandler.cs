using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.EventHandlers;

public class TranslateEquipmentEventHandler : INotificationHandler<EquipmentCreatedDomainEvent>
{
    private readonly ITextTranslator _translator;
    private readonly IAssetsUnitOfWork _unitOfWork;

    public TranslateEquipmentEventHandler(
        ITextTranslator translator,
        IAssetsUnitOfWork unitOfWork)
    {
        _translator = translator;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask Handle(EquipmentCreatedDomainEvent notification, CancellationToken ct)
    {
        var equipment = notification.Equipment;
        var englishName = await _translator.TranslateAsync("pt", "en", equipment.Name);
        var englishDescription = await _translator.TranslateAsync("pt", "en", equipment.Description);
        
        equipment.AddTranslation("en", englishName, englishDescription);
        
        await _unitOfWork.CompleteAsync(ct);
    }
}