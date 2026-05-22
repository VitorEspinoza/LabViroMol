using LabViroMol.Modules.Research.Domain.Partners;

namespace LabViroMol.Modules.Research.Application.Projects.Commands.Create;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public class CreateProjectHandler(
    IProjectRepository projectRepository,
    IResearcherRepository researcherRepository,
    ICurrentUser currentUser,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<CreateProjectCommand, Result>
{
    public async ValueTask<Result> Handle(CreateProjectCommand command, CancellationToken ct)
    {
        var piId = ResearcherId.From(command.PrincipalInvestigatorId);
        var researcher = await researcherRepository.GetByIdAsync(piId, ct);
        if (researcher is null)
            return Result.NotFound("Pesquisador nao encontrado.");

        var result = Project.Create(
            currentUser.Id,
            piId,
            command.Title,
            command.Description,
            PartnerId.From(command.PartnerId));

        if (result.IsFailure)
            return result;

        await projectRepository.AddAsync(result.Data!, ct);
        await unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
