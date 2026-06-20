namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;

public interface IOutboxProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken);
}
