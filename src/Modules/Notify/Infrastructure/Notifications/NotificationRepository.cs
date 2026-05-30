using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Notify.Infrastructure.Notifications;

public class NotificationRepository(NotifyDbContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct) 
        => await context.Notifications.AddAsync(notification, ct);
    
    public async Task<List<Notification>> GetNotificationsByUserNotDismissed(UserId userId, CancellationToken ct)
        => await context.Notifications
            .Include(n => n.NotificationDismissals)
            .Where(n => n.ExpiresOn > DateTimeOffset.Now)
            .Where(n => n.TargetPermissionId == Guid.Parse("f3a7c1d2-8b4e-4c91-a6f7-2d9e5b7f4a13"))
            .Where(n => n.NotificationDismissals
                .All(d => d.UserId != userId))
            .ToListAsync(ct);
    
    public async Task<List<Notification>> GetAllByNotificationIds(List<NotificationId> ids, CancellationToken ct) 
        => await context.Notifications
            .Include(n => n.NotificationDismissals)
            .Where(n => ids.Contains(n.Id))
            .ToListAsync(ct);
    
    public async Task<Notification?> GetByNotificationId(NotificationId notificationId, CancellationToken ct)
        => await context.Notifications.FindAsync(notificationId, ct);
}