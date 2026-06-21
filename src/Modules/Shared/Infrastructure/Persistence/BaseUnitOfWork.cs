using System.Text.Json;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using LabViroMol.Modules.Shared.Kernel.Exceptions;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Messaging;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence;

public abstract class BaseUnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    protected readonly TContext _context;
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly IPersistentEventTypeRegistry _eventTypeRegistry;

    private readonly List<IPersistentEvent> _persistentEvents = new();

    public BaseUnitOfWork(
        TContext context,
        IMediator mediator,
        ICurrentUser currentUser,
        IPersistentEventTypeRegistry eventTypeRegistry)
    {
        _context = context;
        _mediator = mediator;
        _currentUser = currentUser;
        _eventTypeRegistry = eventTypeRegistry;
    }

    public void AddPersistentEvent(IPersistentEvent persistentEvent)
    {
        _persistentEvents.Add(persistentEvent);
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

        var persistentEvents = _context.ChangeTracker.Entries<IHasEvents>()
            .SelectMany(e => e.Entity.Events)
            .OfType<IPersistentEvent>()
            .Concat(_persistentEvents)
            .ToList();

        _context.ChangeTracker.Entries<IHasEvents>().ToList().ForEach(e => e.Entity.ClearEvents());

        foreach (var persistentEvent in persistentEvents)
        {
            var eventType = persistentEvent.GetType();
            var outboxMessage = new OutboxMessage(
                type: _eventTypeRegistry.GetName(eventType),
                content: JsonSerializer.Serialize(persistentEvent, eventType, OutboxJson.Options),
                occurredOn: persistentEvent.OccurredOn);

            _context.Set<OutboxMessage>().Add(outboxMessage);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException)
        {
            throw new UniqueConstraintViolationException(postgresException.MessageText, ex);
        }
    }
}
