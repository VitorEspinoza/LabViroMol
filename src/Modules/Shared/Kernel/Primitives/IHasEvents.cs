using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public interface IHasEvents
{
    IReadOnlyCollection<IEvent> Events { get; }
    void ClearDomainEvents();
    
    void ClearEvents();
}