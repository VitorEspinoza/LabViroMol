using LabViroMol.Modules.Shared.Abstractions.Messaging;

namespace LabViroMol.Modules.Shared.Abstractions.Primitives;

public interface IHasEvents
{
    IReadOnlyCollection<IEvent> Events { get; }
    void ClearDomainEvents();
    
    void ClearEvents();
}