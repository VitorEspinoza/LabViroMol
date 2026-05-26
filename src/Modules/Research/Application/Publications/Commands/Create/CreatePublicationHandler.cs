namespace LabViroMol.Modules.Research.Application.Publications.Commands.Create;

using LabViroMol.Modules.Research.Application.Shared;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public class CreatePublicationHandler(
    IPublicationRepository repository,
    IResearchUnitOfWork unitOfWork)
    : ICommandHandler<CreatePublicationCommand, Result>
{
    public async ValueTask<Result> Handle(CreatePublicationCommand command, CancellationToken ct)
    {
        var result = Publication.Create(
            command.Title,
            command.Description,
            command.Doi,
            command.PublicationDate,
            command.PublishedOn,
            command.PublishUrl);

        if (result.IsFailure)
            return result;

        await repository.AddAsync(result.Data!, ct);
        await unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
