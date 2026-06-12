using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Research.Application.Publications.Commands.Create;

using Shared;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using EventHandlers;
using Mediator;

public class CreatePublicationHandler(
    IPublicationRepository repository,
    IResearchUnitOfWork unitOfWork,
    IServiceScopeFactory scopeFactory)
    : ICommandHandler<CreatePublicationCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(CreatePublicationCommand command, CancellationToken ct)
    {
        var result = Publication.Create(
            command.Title,
            command.Description,
            command.Doi,
            command.PublicationDate,
            command.PublishedOn,
            command.PublishUrl);

        if (result.IsFailure)
            return Result<Guid>.FromError(result);

        var publication = result.Data!;

        await repository.AddAsync(publication, ct);
        await unitOfWork.CompleteAsync(ct);
        
        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();

            var publisher =
                scope.ServiceProvider.GetRequiredService<IPublisher>();
            
            await publisher.Publish(
                new PublicationTranslationEvent(publication.Id),
                CancellationToken.None);
        });
        
        return Result<Guid>.Success(result.Data!.Id);
    }
}
