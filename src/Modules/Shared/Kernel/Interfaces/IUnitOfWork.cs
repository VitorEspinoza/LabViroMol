using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Shared.Kernel.Interfaces;

public interface IUnitOfWork
{
    void AddPersistentEvent(IPersistentEvent persistentEvent);
    public Task CompleteAsync(CancellationToken cancellationToken = default);
}
