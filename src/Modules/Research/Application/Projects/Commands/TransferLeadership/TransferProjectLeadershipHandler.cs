using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Research.Application.Projects.Commands.TransferLeadership;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class TransferProjectLeadershipHandler(
    IProjectRepository projectRepository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<TransferProjectLeadershipCommand, Result>
{
    public async ValueTask<Result> Handle(TransferProjectLeadershipCommand command, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(ProjectId.From(command.ProjectId), ct);
        if (project is null)
            return Result.NotFound("Projeto nao encontrado.");

        var result = project.TransferLeadership(
            ResearcherId.From(command.NewLeadResearcherId),
            ResearcherId.From(command.RequestedById));

        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
