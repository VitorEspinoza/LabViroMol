using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Notify.Infrastructure.Notifications;

public class NotificationQueries(NotifyDbContext context)
{
    public async Task<IReadOnlyCollection<Notification>> GetAllByPermissionsId(List<Guid> ids, CancellationToken ct)
    {
        return await context.Notifications.AsNoTracking()
            .Where(n => ids.Contains(n.Id))
            .ToListAsync(ct);
    }
}