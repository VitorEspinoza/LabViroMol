namespace LabViroMol.Modules.Shared.Abstractions.Interfaces;

public interface IUnitOfWork
{
    public Task CompleteAsync(CancellationToken cancellationToken = default);
}