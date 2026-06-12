using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Shared.Infrastructure.Translation;

namespace LabViroMol.Modules.Research.Application.Publications.Jobs;

public class PublicationTranslationJob : ITranslationJob
{
    private readonly IPublicationRepository _repository;
    private readonly ITextTranslator _translator;
    private readonly IResearchUnitOfWork _unitOfWork;
    
    public PublicationTranslationJob(
        IPublicationRepository repository,
        ITextTranslator translator,
        IResearchUnitOfWork unitOfWork)
    {
        _repository = repository;
        _translator = translator;
        _unitOfWork = unitOfWork;
    }
    
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var publications =
            await _repository.GetMissingEnglishTranslationAsync(50,ct);

        foreach (var publication in publications)
        {
            var englishTitle =
                await _translator.TranslateAsync(
                    "pt",
                    "en",
                    publication.Title);

            var englishDescription =
                await _translator.TranslateAsync(
                    "pt",
                    "en",
                    publication.Description);

            publication.AddTranslation(
                "en",
                englishTitle,
                englishDescription);
        }
        
        await _unitOfWork.CompleteAsync(ct);
    }
}