namespace LabViroMol.Modules.Shared.Kernel.Interfaces;

public interface IUnitOfWork
{
    public Task CompleteAsync(CancellationToken cancellationToken = default);
}