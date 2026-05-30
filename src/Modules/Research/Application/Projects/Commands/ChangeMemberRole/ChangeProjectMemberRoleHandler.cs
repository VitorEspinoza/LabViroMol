namespace LabViroMol.Modules.Research.Application.Projects.Commands.ChangeMemberRole;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class ChangeProjectMemberRoleHandler(
    IProjectRepository projectRepository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<ChangeProjectMemberRoleCommand, Result>
{
    public async ValueTask<Result> Handle(ChangeProjectMemberRoleCommand command, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(ProjectId.From(command.ProjectId), ct);
        if (project is null)
            return Result.NotFound("Projeto nao encontrado.");

        var role = ProjectRole.FromString(command.NewRole);

        var result = project.ChangeMemberRole(
            ResearcherId.From(command.ResearcherId),
            role,
            ResearcherId.From(command.RequestedById));

        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
