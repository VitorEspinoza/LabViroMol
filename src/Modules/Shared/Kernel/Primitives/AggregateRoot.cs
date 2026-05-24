using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public abstract class AggregateRoot<TId> : AuditableEntity<TId>, IHasEvents
{
    public AggregateRoot(TId id, UserId createdBy) : base(id, createdBy) { }
    
    public AggregateRoot(TId id) : base(id) { }
    
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
