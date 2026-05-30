namespace LabViroMol.Modules.Research.Application.Publications.Commands.Delete;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class DeletePublicationHandler(
    IPublicationRepository repository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<DeletePublicationCommand, Result>
{
    public async ValueTask<Result> Handle(DeletePublicationCommand command, CancellationToken ct)
    {
        var publication = await repository.GetByIdAsync(PublicationId.From(command.PublicationId), ct);
        if (publication is null)
            return Result.NotFound("Publicacao nao encontrada.");

        repository.Delete(publication);
        await unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
