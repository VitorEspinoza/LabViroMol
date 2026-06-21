using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Projects.Jobs;

public class ProjectTranslationHandler : INotificationHandler<ProjectTranslationPersistentEvent>
{
    private readonly IProjectRepository _repository;
    private readonly ITextTranslator _translator;
    private readonly IResearchUnitOfWork _unitOfWork;
    
    public ProjectTranslationHandler(
        IProjectRepository repository,
        ITextTranslator translator,
        IResearchUnitOfWork unitOfWork)
    {
        _repository = repository;
        _translator = translator;
        _unitOfWork = unitOfWork;
    }
    
    public async ValueTask Handle(ProjectTranslationPersistentEvent notification, CancellationToken ct)
    {
        var projects =
            await _repository.GetMissingEnglishTranslationAsync(5,ct);

        foreach (var project in projects)
        {
            var englishTitle =
                await _translator.TranslateAsync(
                    "pt",
                    "en",
                    project.Title);

            var englishDescription =
                await _translator.TranslateAsync(
                    "pt",
                    "en",
                    project.Description);

            project.AddTranslation(
                "en",
                englishTitle,
                englishDescription);
        }
        
        await _unitOfWork.CompleteAsync(ct);
    }
}