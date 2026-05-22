namespace LabViroMol.Modules.Research.Application.Projects.Commands.RemoveMember;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public class RemoveProjectMemberHandler(
    IProjectRepository repository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<RemoveProjectMemberCommand, Result>
{
    public async ValueTask<Result> Handle(RemoveProjectMemberCommand command, CancellationToken ct)
    {
        var project = await repository.GetByIdAsync(ProjectId.From(command.ProjectId), ct);
        if (project is null)
            return Result.NotFound("Projeto nao encontrado.");

        var result = project.RemoveMember(
            ResearcherId.From(command.ResearcherId),
            ResearcherId.From(command.RequestedById));

        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
