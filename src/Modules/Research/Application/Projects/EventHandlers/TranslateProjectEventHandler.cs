using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Projects.EventHandlers;

public class TranslateProjectEventHandler : INotificationHandler<ProjectCreatedDomainEvent>
{
    private readonly ITextTranslator _translator;
    private readonly IResearchUnitOfWork _unitOfWork;

    public TranslateProjectEventHandler(
        ITextTranslator translator,
        IResearchUnitOfWork unitOfWork)
    {
        _translator = translator;
        _unitOfWork = unitOfWork;
    }
    
    public async ValueTask Handle(ProjectCreatedDomainEvent notification, CancellationToken ct)
    {
        var project = notification.Project;
        var englishTitle = await _translator.TranslateAsync("pt", "en", project.Title);
        var englishDescription = await _translator.TranslateAsync("pt", "en", project.Description);
        
        project.AddTranslation("en", englishTitle, englishDescription);
        await _unitOfWork.CompleteAsync(ct);
    }
}