using LabViroMol.Modules.Shared.Kernel.Messaging;

namespace LabViroMol.Modules.Shared.Kernel.Interfaces;

public interface IUnitOfWork
{
    void AddIntegrationEvent(IIntegrationEvent integrationEvent);
    public Task CompleteAsync(CancellationToken cancellationToken = default);
}