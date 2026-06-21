using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.Equipments.Events;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.EventHandlers;

public sealed class EquipmentTranslationHandler : INotificationHandler<EquipmentTranslationPersistentEvent>
{
    private readonly IEquipmentRepository _repository;
    private readonly ITextTranslator _translator;
    private readonly IAssetsUnitOfWork _unitOfWork;

    public EquipmentTranslationHandler(
        IEquipmentRepository repository,
        ITextTranslator translator,
        IAssetsUnitOfWork unitOfWork)
    {
        _repository = repository;
        _translator = translator;
        _unitOfWork = unitOfWork;
    }
    
    public async ValueTask Handle(EquipmentTranslationPersistentEvent notification, CancellationToken ct)
    {
        var equipments =
            await _repository.GetMissingEnglishTranslationAsync(5,ct);

        foreach (var equipment in equipments)
        {
            var englishName =
                await _translator.TranslateAsync(
                    "pt",
                    "en",
                    equipment.Name);

            var englishDescription =
                await _translator.TranslateAsync(
                    "pt",
                    "en",
                    equipment.Description);

            equipment.AddTranslation(
                "en",
                englishName,
                englishDescription);
        }

        await _unitOfWork.CompleteAsync(ct);
    }
}