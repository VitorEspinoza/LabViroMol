using System.Text.Json;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Messaging;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence;

public abstract class BaseUnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    
    protected readonly TContext _context;
    private readonly IMediator _mediator;

    private readonly List<IIntegrationEvent> _integrationEvents = new();
    public BaseUnitOfWork(TContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public void AddIntegrationEvent(IIntegrationEvent integrationEvent)
    {
        _integrationEvents.Add(integrationEvent);
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        while (_context.ChangeTracker.Entries<IHasEvents>().Any(e => e.Entity.Events.OfType<IDomainEvent>().Any()))
        {
            var entitiesWithDomainEvents = _context.ChangeTracker.Entries<IHasEvents>()
                .Where(e => e.Entity.Events.OfType<IDomainEvent>().Any())
                .ToList();

            var domainEvents = entitiesWithDomainEvents
                .SelectMany(e => e.Entity.Events)
                .OfType<IDomainEvent>()
                .ToList();

            foreach (var entity in entitiesWithDomainEvents.Select(e => e.Entity))
            {
                entity.ClearDomainEvents();
            }

            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent, cancellationToken);
            }
        }

        var allIntegrationEvents = _context.ChangeTracker.Entries<IHasEvents>()
            .SelectMany(e => e.Entity.Events)
            .OfType<IIntegrationEvent>()
            .Concat(_integrationEvents)
            .ToList();

        _context.ChangeTracker.Entries<IHasEvents>().ToList().ForEach(e => e.Entity.ClearEvents());

        // foreach (var integrationEvent in allIntegrationEvents)
        // {
        //     var outboxMessage = new OutboxMessage(
        //         type: _registry.GetName(integrationEvent.GetType()),
        //         content: JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType())
        //     );
        //     _context.Set<OutboxMessage>().Add(outboxMessage);
        // }

        await _context.SaveChangesAsync(cancellationToken);
    }
}