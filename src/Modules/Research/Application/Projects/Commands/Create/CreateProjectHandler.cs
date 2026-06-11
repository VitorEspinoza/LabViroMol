using LabViroMol.Modules.Research.Application.Projects.EventHandlers;
using LabViroMol.Modules.Research.Domain.Partners;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Research.Application.Projects.Commands.Create;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class CreateProjectHandler : ICommandHandler<CreateProjectCommand, Result>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IResearcherRepository _researcherRepository;
    private readonly IResearchUnitOfWork _unitOfWork;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public CreateProjectHandler(
        IProjectRepository projectRepository,
        IResearcherRepository researcherRepository,
        IResearchUnitOfWork unitOfWork,
        IServiceScopeFactory scopeFactory)
    {
        _projectRepository = projectRepository;
        _researcherRepository = researcherRepository;
        _unitOfWork = unitOfWork;
        _scopeFactory = scopeFactory;
    }
    
    public async ValueTask<Result> Handle(CreateProjectCommand command, CancellationToken ct)
    {
        var piId = ResearcherId.From(command.PrincipalInvestigatorId);
        var researcher = await _researcherRepository.GetByIdAsync(piId, ct);
        if (researcher is null)
            return Result.NotFound("Pesquisador nao encontrado.");

        var result = Project.Create(
            piId,
            command.Title,
            command.Description,
            PartnerId.From(command.PartnerId));

        if (result.IsFailure)
            return result;
        var project = result.Data!;

        await _projectRepository.AddAsync(project, ct);
        await _unitOfWork.CompleteAsync(ct);
        
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();

            var publisher =
                scope.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.Publish(
                new ProjectTranslationEvent(project.Id),
                CancellationToken.None);
        });

        return Result.Success();
    }
}
