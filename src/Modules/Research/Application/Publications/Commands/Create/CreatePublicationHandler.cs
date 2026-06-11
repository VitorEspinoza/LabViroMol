using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Research.Application.Publications.Commands.Create;

using Shared;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using EventHandlers;
using Mediator;


public class CreatePublicationHandler : ICommandHandler<CreatePublicationCommand, Result>
{
    private readonly IPublicationRepository _repository;
    private readonly IResearchUnitOfWork _unitOfWork;
    private readonly IServiceScopeFactory _scopeFactory;

    
    public CreatePublicationHandler(
        IPublicationRepository repository,
        IResearchUnitOfWork unitOfWork,
        IServiceScopeFactory  scopeFactory)
    {
        _repository = repository;
        _unitOfWork = unitOfWork; 
        _scopeFactory = scopeFactory;
    }
    
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

        var publication = result.Data!;

        await _repository.AddAsync(publication, ct);
        await _unitOfWork.CompleteAsync(ct);
        
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();

            var publisher =
                scope.ServiceProvider.GetRequiredService<IPublisher>();
            
            await publisher.Publish(
                new PublicationTranslationEvent(publication.Id),
                CancellationToken.None);
        });
        
        return Result.Success();
    }
}
