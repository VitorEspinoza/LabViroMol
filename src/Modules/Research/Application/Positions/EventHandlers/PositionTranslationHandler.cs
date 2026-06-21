using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Positions.EventHandlers;

public sealed class PositionTranslationHandler : INotificationHandler<PositionTranslationPersistentEvent>
{
    private readonly IPositionRepository _repository;
    private readonly ITextTranslator _translator;
    private readonly IResearchUnitOfWork _unitOfWork;
    
    public PositionTranslationHandler(
        IPositionRepository repository,
        ITextTranslator translator,
        IResearchUnitOfWork unitOfWork)
    {
        _repository = repository;
        _translator = translator;
        _unitOfWork = unitOfWork;
    }
    
    public async ValueTask Handle(PositionTranslationPersistentEvent notification, CancellationToken ct)
    {
        var positions =
            await _repository.GetMissingEnglishTranslationAsync(5,ct);

        foreach (var position in positions)
        {
            var englishName =
                await _translator.TranslateAsync(
                    "pt",
                    "en",
                    position.Name);

            var englishDescription =
                await _translator.TranslateAsync(
                    "pt",
                    "en",
                    position.Description);

            position.AddTranslation(
                "en",
                englishName,
                englishDescription);
        }
        
        await _unitOfWork.CompleteAsync(ct);
    }
}