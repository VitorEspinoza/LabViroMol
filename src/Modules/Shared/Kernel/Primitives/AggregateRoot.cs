using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public abstract class AggregateRoot<TId> : BaseEntity<TId>, IHasEvents, IConcurrencySafe
{
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
