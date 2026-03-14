using LabViroMol.Modules.Shared.Abstractions.Messaging;

namespace LabViroMol.Modules.Shared.Abstractions.Persistence;

public interface IUnitOfWork
{
    public Task CompleteAsync(CancellationToken cancellationToken = default);
    void AddIntegrationEvent(IIntegrationEvent integrationEvent);
}