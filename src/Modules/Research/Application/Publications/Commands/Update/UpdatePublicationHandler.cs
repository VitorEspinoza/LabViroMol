using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Research.Application.Publications.Commands.Update;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class UpdatePublicationHandler(
    IPublicationRepository repository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<UpdatePublicationCommand, Result>
{
    public async ValueTask<Result> Handle(UpdatePublicationCommand command, CancellationToken ct)
    {
        var publication = await repository.GetByIdAsync(PublicationId.From(command.PublicationId), ct);
        if (publication is null)
            return Result.NotFound("Publicacao nao encontrada.");

        var result = publication.Update(
            command.Title,
            command.Description,
            command.PublishedOn,
            command.PublishUrl);

        if (result.IsFailure)
            return result;

        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
