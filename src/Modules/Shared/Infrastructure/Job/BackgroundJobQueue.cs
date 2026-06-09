using LabViroMol.Modules.Shared.Kernel.Interfaces;
using System.Threading.Channels;

namespace LabViroMol.Modules.Shared.Infrastructure.Job;

public sealed class BackgroundJobQueue : IBackgroundJobQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue;

    public BackgroundJobQueue()
    {
        _queue = Channel.CreateUnbounded<
            Func<IServiceProvider, CancellationToken, Task>>();
    }

    public async ValueTask QueueAsync(
        Func<IServiceProvider, CancellationToken, Task> workItem)
    {
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<IServiceProvider, CancellationToken, Task>>
        DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}