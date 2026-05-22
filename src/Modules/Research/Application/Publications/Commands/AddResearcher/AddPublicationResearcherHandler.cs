namespace LabViroMol.Modules.Research.Application.Publications.Commands.AddResearcher;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public class AddPublicationResearcherHandler(
    IPublicationRepository publicationRepository,
    IResearcherRepository researcherRepository,
    ICurrentUser currentUser,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<AddPublicationResearcherCommand, Result>
{
    public async ValueTask<Result> Handle(AddPublicationResearcherCommand command, CancellationToken ct)
    {
        var publication = await publicationRepository.GetByIdAsync(PublicationId.From(command.PublicationId), ct);
        if (publication is null)
            return Result.NotFound("Publicacao nao encontrada.");

        var researcher = await researcherRepository.GetByIdAsync(ResearcherId.From(command.ResearcherId), ct);
        if (researcher is null)
            return Result.NotFound("Pesquisador nao encontrado.");

        var result = publication.AddResearcher(ResearcherId.From(command.ResearcherId), currentUser.Id);
        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
