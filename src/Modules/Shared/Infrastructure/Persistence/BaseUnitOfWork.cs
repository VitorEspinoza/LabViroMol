using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly ICurrentUser _currentUser;

    private readonly List<IIntegrationEvent> _integrationEvents = new();
    public BaseUnitOfWork(TContext context, IMediator mediator, ICurrentUser currentUser)
    {
        _context = context;
        _mediator = mediator;
        _currentUser = currentUser;
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

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUser.Id;

        foreach (var entry in _context.ChangeTracker.Entries())
        {
            if (entry is { State: EntityState.Added, Entity: ICreationAuditable })
            {
                entry.Property("CreatedAt").CurrentValue = now;
                entry.Property("CreatedBy").CurrentValue = userId;
            }

            if (entry is { State: EntityState.Modified, Entity: IModificationAuditable })
            {
                entry.Property("UpdatedAt").CurrentValue = now;
                entry.Property("UpdatedBy").CurrentValue = userId;
            }
            
            if (entry is { State: EntityState.Deleted, Entity: IDeletionAuditable })
            {
                entry.State = EntityState.Modified; 
                
                entry.Property("IsDeleted").CurrentValue = true;
                entry.Property("RemovedAt").CurrentValue = now;
                entry.Property("RemovedBy").CurrentValue = userId;
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
        
        foreach (var integrationEvent in allIntegrationEvents)
        {
            await _mediator.Publish(integrationEvent, cancellationToken);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}