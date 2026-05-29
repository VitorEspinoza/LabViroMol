namespace LabViroMol.Modules.Research.Application.Projects.Commands.Cancel;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class CancelProjectHandler(
    IProjectRepository repository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<CancelProjectCommand, Result>
{
    public async ValueTask<Result> Handle(CancelProjectCommand command, CancellationToken ct)
    {
        var project = await repository.GetByIdAsync(ProjectId.From(command.ProjectId), ct);
        if (project is null)
            return Result.NotFound("Projeto não encontrado.");

        var result = project.Cancel(ResearcherId.From(command.ResearcherId));
        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
