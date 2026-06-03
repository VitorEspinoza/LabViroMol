using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Shared.Kernel.Identity;

namespace LabViroMol.Modules.Notify.Domain.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct);
    Task<List<Notification>> GetNotificationsByUserNotDismissed(UserId userId, List<string> permissions, CancellationToken ct);
    Task<List<Notification>> GetAllByNotificationIds(List<NotificationId> ids, CancellationToken ct);
    Task<Notification?> GetByNotificationId(NotificationId notificationId, CancellationToken ct);
}