using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Notify.Infrastructure.Notifications;

public class NotificationQueries(NotifyDbContext context)
{
    public async Task<IReadOnlyCollection<Notification>> GetAllByPermissions(List<string> permissions)
    {
        return await context.Notifications.AsNoTracking()
            .Where(n => permissions.Contains(n.TargetPermission))
            .ToListAsync();
    }
}