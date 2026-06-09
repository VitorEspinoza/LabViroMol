namespace LabViroMol.Modules.Shared.Kernel.Interfaces;

public interface IBackgroundJobQueue
{
    ValueTask QueueAsync(
        Func<IServiceProvider, CancellationToken, Task> workItem);

    ValueTask<Func<IServiceProvider, CancellationToken, Task>>
        DequeueAsync(CancellationToken cancellationToken);
}