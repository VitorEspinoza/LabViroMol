using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;

namespace LabViroMol.Modules.Notify.Infrastructure.Notifications;

public class NotificationRepository(NotifyDbContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct) 
        => await context.Notifications.AddAsync(notification, ct);
}