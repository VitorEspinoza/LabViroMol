using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Messaging;

namespace LabViroMol.Modules.Shared.Abstractions.Primitives;

public abstract class AggregateRoot<TId> : AuditableEntity<TId>, IHasEvents
{
    public AggregateRoot(TId id, UserId createdBy) : base(id, createdBy) { }
    protected AggregateRoot() : base() { }

    private readonly List<IEvent> _events = new();

    public IReadOnlyCollection<IEvent> Events => _events.AsReadOnly();

    protected void AddEvent(IEvent @event)
    {
        _events.Add(@event);
    }

    public void ClearEvents()
    {
        _events.Clear();
    }

    public void ClearDomainEvents()
    {
        _events.RemoveAll(e => e is IDomainEvent);
    }
}
