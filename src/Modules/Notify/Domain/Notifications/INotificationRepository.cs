using LabViroMol.Modules.Shared.Kernel.Identity;

namespace LabViroMol.Modules.Notify.Domain.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct);
    Task<List<Notification>> GetNotificationsByUserNotDismissed(UserId userId, List<string> permissions, CancellationToken ct);
    Task<Notification?> GetByNotificationId(NotificationId notificationId, CancellationToken ct);
}