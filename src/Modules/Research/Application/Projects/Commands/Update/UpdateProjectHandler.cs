using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Research.Application.Projects.Commands.Update;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class UpdateProjectHandler(
    IProjectRepository repository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProjectCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateProjectCommand command, CancellationToken ct)
    {
        var project = await repository.GetByIdAsync(ProjectId.From(command.ProjectId), ct);
        if (project is null)
            return Result.NotFound("Projeto nao encontrado.");

        var result = project.Update(command.Title, command.Description, ResearcherId.From(command.RequestedById));
        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
