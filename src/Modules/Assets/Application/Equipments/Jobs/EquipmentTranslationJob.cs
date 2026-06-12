using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Infrastructure.Translation;

namespace LabViroMol.Modules.Assets.Application.Equipments.Jobs;

public class EquipmentTranslationJob : ITranslationJob
{
    private readonly IEquipmentRepository _repository;
    private readonly ITextTranslator _translator;
    private readonly IAssetsUnitOfWork _unitOfWork;

    public EquipmentTranslationJob(
        IEquipmentRepository repository,
        ITextTranslator translator,
        IAssetsUnitOfWork unitOfWork)
    {
        _repository = repository;
        _translator = translator;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var equipments =
            await _repository.GetMissingEnglishTranslationAsync(50,ct);

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