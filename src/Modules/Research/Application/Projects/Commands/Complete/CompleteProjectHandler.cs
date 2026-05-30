namespace LabViroMol.Modules.Research.Application.Projects.Commands.Complete;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class CompleteProjectHandler(
    IProjectRepository repository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<CompleteProjectCommand, Result>
{
    public async ValueTask<Result> Handle(CompleteProjectCommand command, CancellationToken ct)
    {
        var project = await repository.GetByIdAsync(ProjectId.From(command.ProjectId), ct);
        if (project is null)
            return Result.NotFound("Projeto não encontrado.");

        var result = project.Complete(ResearcherId.From(command.ResearcherId));
        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
