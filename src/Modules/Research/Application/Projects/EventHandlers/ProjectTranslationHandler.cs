using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Shared.Infrastructure.Translation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LabViroMol.Modules.Research.Application.Projects.EventHandlers;

public class ProjectTranslationHandler : INotificationHandler<ProjectTranslationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProjectTranslationHandler> _logger;

    public ProjectTranslationHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<ProjectTranslationHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async ValueTask Handle(ProjectTranslationEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var repository =
                scope.ServiceProvider
                    .GetRequiredService<IProjectRepository>();

            var translator =
                scope.ServiceProvider
                    .GetRequiredService<ITextTranslator>();

            var unitOfWork =
                scope.ServiceProvider
                    .GetRequiredService<IResearchUnitOfWork>();

            var project =
                await repository.GetByIdAsync(notification.ProjectId, CancellationToken.None);

            if (project is null)
                return;

            var englishTitle =
                await translator.TranslateAsync(
                    "pt",
                    "en",
                    project.Title);

            var englishDescription =
                await translator.TranslateAsync(
                    "pt",
                    "en",
                    project.Description);

            project.AddTranslation(
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