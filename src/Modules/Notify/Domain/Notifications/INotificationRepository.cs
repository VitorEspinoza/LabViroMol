using LabViroMol.Modules.Shared.Kernel.Identity;

namespace LabViroMol.Modules.Notify.Domain.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct);
    Task<List<Notification>> GetNotificationsByUserNotDismissed(UserId userId, CancellationToken ct);
    Task<List<Notification>> GetAllByNotificationIds(List<NotificationId> ids, CancellationToken ct);
    Task<Notification?> GetByNotificationId(NotificationId notificationId, CancellationToken ct);
}