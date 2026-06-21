using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.IntegrationTests.Shared;

public static class OutboxDrainer
{
    public static async Task DrainAsync(IServiceProvider services, int maxRounds = 10)
    {
        for (var round = 0; round < maxRounds; round++)
        {
            using var scope = services.CreateScope();

            var pendingBefore = await CountPendingAsync(scope.ServiceProvider);
            if (pendingBefore == 0)
                return;

            foreach (var processor in scope.ServiceProvider.GetServices<IOutboxProcessor>())
                await processor.ProcessAsync(CancellationToken.None);

            var pendingAfter = await CountPendingAsync(scope.ServiceProvider);
            if (pendingAfter == 0)
                return;

            if (pendingAfter == pendingBefore)
                throw new InvalidOperationException(
                    "OutboxDrainer não conseguiu processar mensagens pendentes - possível falha de handler.");
        }

        throw new TimeoutException($"OutboxDrainer excedeu {maxRounds} rodadas sem esvaziar o outbox.");
    }

    private static async Task<int> CountPendingAsync(IServiceProvider scopedServices)
    {
        var total = 0;
        total += await PendingCountAsync(scopedServices.GetRequiredService<LabViroMolIdentityDbContext>());
        total += await PendingCountAsync(scopedServices.GetRequiredService<InventoryDbContext>());
        total += await PendingCountAsync(scopedServices.GetRequiredService<ResearchDbContext>());
        total += await PendingCountAsync(scopedServices.GetRequiredService<SchedulingDbContext>());
        total += await PendingCountAsync(scopedServices.GetRequiredService<AssetsDbContext>());
        total += await PendingCountAsync(scopedServices.GetRequiredService<NotifyDbContext>());
        return total;
    }

    private static Task<int> PendingCountAsync(DbContext context) =>
        context.Set<OutboxMessage>().CountAsync(m => m.ProcessedOn == null);
}
