namespace LabViroMol.Modules.Research.Application.Publications.Commands.AssignDoi;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class AssignPublicationDoiHandler(
    IPublicationRepository publicationRepository,
    ICurrentUser currentUser,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<AssignPublicationDoiCommand, Result>
{
    public async ValueTask<Result> Handle(AssignPublicationDoiCommand command, CancellationToken ct)
    {
        var publication = await publicationRepository.GetByIdAsync(PublicationId.From(command.PublicationId), ct);
        if (publication is null)
            return Result.NotFound("Publicacao nao encontrada.");

        var result = publication.AssignDoi(command.Doi, currentUser.Id);
        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
