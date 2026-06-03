using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Research.Application.Publications.Commands.RemoveResearcher;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class RemovePublicationResearcherHandler(
    IPublicationRepository publicationRepository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<RemovePublicationResearcherCommand, Result>
{
    public async ValueTask<Result> Handle(RemovePublicationResearcherCommand command, CancellationToken ct)
    {
        var publication = await publicationRepository.GetByIdAsync(PublicationId.From(command.PublicationId), ct);
        if (publication is null)
            return Result.NotFound("Publicacao nao encontrada.");

        var result = publication.RemoveResearcher(ResearcherId.From(command.ResearcherId));
        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
