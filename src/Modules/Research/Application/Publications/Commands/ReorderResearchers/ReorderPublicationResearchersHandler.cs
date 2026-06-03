using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Research.Application.Publications.Commands.ReorderResearchers;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class ReorderPublicationResearchersHandler(
    IPublicationRepository publicationRepository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<ReorderPublicationResearchersCommand, Result>
{
    public async ValueTask<Result> Handle(ReorderPublicationResearchersCommand command, CancellationToken ct)
    {
        var publication = await publicationRepository.GetByIdAsync(PublicationId.From(command.PublicationId), ct);
        if (publication is null)
            return Result.NotFound("Publicacao nao encontrada.");

        var orderedIds = command.ResearcherIds
            .Select(id => ResearcherId.From(id))
            .ToList();

        var result = publication.ReorderResearchers(orderedIds);
        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
