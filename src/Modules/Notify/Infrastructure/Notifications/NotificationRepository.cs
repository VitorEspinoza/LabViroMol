using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Notify.Infrastructure.Notifications;

public class NotificationRepository(NotifyDbContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct) 
        => await context.Notifications.AddAsync(notification, ct);
    
    public async Task<List<Notification>> GetNotificationsByUserNotDismissed(UserId userId, List<string> permissions, CancellationToken ct)
        => await context.Notifications
            .Include(n => n.NotificationDismissals)
            .Where(n => n.ExpiresOn > DateTimeOffset.Now)
            .Where(n => permissions.Contains(n.TargetPermission))
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