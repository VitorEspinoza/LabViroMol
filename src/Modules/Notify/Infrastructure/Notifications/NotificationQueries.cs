using LabViroMol.Modules.Notify.Application.Notifications.Queries;
using LabViroMol.Modules.Notify.Application.Notifications.ViewModels;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Notify.Infrastructure.Notifications;

internal sealed class NotificationQueries(NotifyDbContext context) : INotificationQueries
{
    public async Task<List<NotificationViewModel>> GetUnreadByUserAsync(UserId userId, List<string> permissions, CancellationToken ct)
    {
        return await context.Notifications.AsNoTracking()
            .Where(n => n.ExpiresOn > DateTimeOffset.UtcNow)
            .Where(n => permissions.Contains(n.TargetPermission))
            .Where(n => n.NotificationDismissals.All(d => d.UserId != userId))
            .OrderByDescending(n => EF.Property<DateTimeOffset>(n, "CreatedAt"))
            .Select(n => new NotificationViewModel(
                n.Id.Value,
                n.Title,
                n.Message,
                n.Type,
                n.ReferenceId,
                n.ReferenceModule,
                EF.Property<DateTimeOffset>(n, "CreatedAt")))
            .ToListAsync(ct);
    }
}
