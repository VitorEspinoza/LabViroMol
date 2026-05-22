namespace LabViroMol.Modules.Research.Application.Projects.Commands.Start;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public class StartProjectHandler(
    IProjectRepository repository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<StartProjectCommand, Result>
{
    public async ValueTask<Result> Handle(StartProjectCommand command, CancellationToken ct)
    {
        var project = await repository.GetByIdAsync(ProjectId.From(command.ProjectId), ct);
        if (project is null)
            return Result.NotFound("Projeto não encontrado.");

        var result = project.Start(ResearcherId.From(command.ResearcherId));
        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
