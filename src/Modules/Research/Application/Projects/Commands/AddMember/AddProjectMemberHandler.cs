namespace LabViroMol.Modules.Research.Application.Projects.Commands.AddMember;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public sealed class AddProjectMemberHandler(
    IProjectRepository projectRepository,
    IResearcherRepository researcherRepository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<AddProjectMemberCommand, Result>
{
    public async ValueTask<Result> Handle(AddProjectMemberCommand command, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(ProjectId.From(command.ProjectId), ct);
        if (project is null)
            return Result.NotFound("Projeto nao encontrado.");

        var researcherId = ResearcherId.From(command.ResearcherId);
        var researcher = await researcherRepository.GetByIdAsync(researcherId, ct);
        if (researcher is null)
            return Result.NotFound("Pesquisador nao encontrado.");

        var role = Enum.Parse<ProjectRole>(command.Role);
        var result = project.AddMember(researcherId, role, ResearcherId.From(command.RequestedById));
        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
