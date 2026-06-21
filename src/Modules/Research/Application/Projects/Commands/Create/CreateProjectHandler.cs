using LabViroMol.Modules.Research.Domain.Partners;

namespace LabViroMol.Modules.Research.Application.Projects.Commands.Create;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class CreateProjectHandler(
    IProjectRepository projectRepository,
    IResearcherRepository researcherRepository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<CreateProjectCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreateProjectCommand command, CancellationToken ct)
    {
        var piId = ResearcherId.From(command.PrincipalInvestigatorId);
        var researcher = await researcherRepository.GetByIdAsync(piId, ct);
        if (researcher is null)
            return Result<Guid>.NotFound("Pesquisador nao encontrado.");

        var result = Project.Create(
            piId,
            command.Title,
            command.Description,
            PartnerId.From(command.PartnerId));

        if (result.IsFailure)
            return Result<Guid>.FromError(result);

        var project = result.Data!;

        await projectRepository.AddAsync(project, ct);
        
        unitOfWork.AddPersistentEvent(new ProjectTranslationPersistentEvent());
        
        await unitOfWork.CompleteAsync(ct);

        return Result<Guid>.Success(result.Data!.Id);
    }
}
